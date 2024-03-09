using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using ProcDungeon.World;
using System.Collections.Generic;
using ProcDungeon.UI;
using ProcDungeon.Experimental;
using Unity.VisualScripting;

namespace ProcDungeon
{
    public class DungeonLevelGenerator : Singleton<DungeonLevelGenerator>
    {
        [SerializeField]
        DungeonLevelSetting settings;

        [SerializeField]
        int seed = 1234;
        public int Seed => seed;

        [SerializeField]
        GameObject debugFloorPrefab;

        [SerializeField]
        GameObject debugWallPrefab;

        [SerializeField]
        AbstractDoorController debugDoorPrefab;

        [SerializeField]
        SpecificKey debugKeyPrefab;

        [SerializeField]
        Transform generatedLevel;

        public PlayerController PlayerController;
        public DungeonGrid DungeonGrid { get; private set; }

        void GenerateLevel(int seed)
        {
            Debug.Log($"Starting level generation (Seed {seed})");

            foreach (Transform t in generatedLevel.transform)
            {
                Destroy(t.gameObject);
            }

            Random.InitState(seed);

            var grid = new DungeonGridLayer(settings);

            DungeonGrid = new DungeonGrid(grid, settings);

            var segmenter = new GridSegmenter(ref settings);
            segmenter.Segment();

            // ROOMS
            var roomGenerator = new RoomGenerator(grid, ref settings);
            roomGenerator.PlaceRooms(segmenter);
            DungeonGrid.Rooms = roomGenerator.Rooms;

            // HALLS
            var hallwayGenerator = new HallwayGenerator(grid, roomGenerator.Rooms, ref settings);
            hallwayGenerator.MakeHallways();

            roomGenerator.CalculateHubSeparations();

            for (int i = 0, n = Random.Range(2, 4); i < n; i++)
            {
                hallwayGenerator.AddExtraHallway();
                roomGenerator.CalculateHubSeparations();
            }
            
            // TODO: Add Range to settings
            for (int i = 0, n = Random.Range(4, 8); i < n; i++)
            {
                hallwayGenerator.AddDeadEndHallway();
            }

            // SPAWN PLAYER
            DungeonRoom spawnRoom;
            var spawnPosition = PlayerController.ChooseStartPosition(roomGenerator.Rooms, DungeonGrid.Dungeon, out spawnRoom);
            var spawnLookDirection = SpawnLookDirection(spawnPosition, spawnRoom);

            // DOORS
            var puzzleGenerator = new PuzzleGenerator(roomGenerator, spawnRoom, spawnPosition);
            var nDoors = puzzleGenerator.AddDoors(Random.Range(2, 4));
            Debug.Log($"Added {nDoors} doors to level");
            DungeonGrid.Doors = puzzleGenerator.Doors;

            // PLACE WORLD
            DebugPlaceHallways(hallwayGenerator);
            DebugPlaceRooms(roomGenerator, hallwayGenerator);

            DebugPlaceDoors(puzzleGenerator);
            DebugPlaceKeys(puzzleGenerator);

            SpawnPlayer(spawnPosition, spawnLookDirection, spawnRoom);

            // SETUP MAP
            DungeonGrid.Hub = roomGenerator.CreateHub();
            DebugPlaceRoom(DungeonGrid.Hub, hallwayGenerator);
            DungeonHub.instance.Room = DungeonGrid.Hub;


            // Note that this must be after debug place things which create info because because
            PrepareMap();
            LevelMapCamera.instance.AdjustView();
            DungeonGrid.VisitPosition(spawnPosition, spawnLookDirection);

            Debug.Log($"Done level generation (Seed {seed})");

        }

        [SerializeField]
        Transform GenerateMapRoot;


        void PrepareMap()
        {
            var lookup = MapTilesCollection.instance;

            foreach (var (coordinates, position) in DungeonGrid.GridPositions)
            {
                var groundId = position.GroundID;
                var mapTile = lookup.GetTileInstance(groundId);
                if (mapTile == null)
                {
                    Debug.LogWarning($"Failed to create map tile with ID {groundId} at {coordinates}");
                    continue;
                }

                mapTile.transform.SetParent(GenerateMapRoot);
                mapTile.transform.position = DungeonGrid.LocalWorldPosition(coordinates, -0.2f);
                mapTile.name = $"GroundMap {groundId} {coordinates}";
                position.SetMapTile(mapTile);

                if (position.Feature != GridMapFeature.None)
                {
                    var featureTile = lookup.GetTileInstance(position.FullFeatureID);
                    if (featureTile != null)
                    {
                        featureTile.transform.SetParent(GenerateMapRoot);
                        featureTile.transform.position = DungeonGrid.LocalWorldPosition(coordinates, -0.1f);
                        featureTile.name = $"MapFeature {position.FullFeatureID} {coordinates}";
                        position.SetFeatureTile(featureTile);
                    } else
                    {
                        Debug.LogWarning($"Missing map tile {position.FullFeatureID} at {coordinates}");
                    }
                }
                
            }
        }

        private Vector2Int SpawnLookDirection(Vector2Int spawnPosition, DungeonRoom spawnRoom)
        {
            var lookDirections = spawnRoom
                .DirectionToExits(spawnPosition)
                .OrderBy(direction => Mathf.Min(Mathf.Abs(direction.x), Mathf.Abs(direction.y)))
                .ToList();

            if (lookDirections.Count > 0)
            {
                return lookDirections.First().MainDirection();
            }
            else
            {
                return MathExtensions.RandomDirection();
            }

        }

        private void SpawnPlayer(Vector2Int spawnPosition, Vector2Int lookDirection, DungeonRoom spawnRoom)
        {
            Debug.Log($"Player spawns at {spawnPosition} in room {spawnRoom}");
            PlayerController.DungeonGrid = DungeonGrid;
            PlayerController.AddComponent<ElevationNoiseSubscriber>();
            PlayerController.Teleport(spawnPosition, lookDirection);

        }


        private void Start()
        {

            GenerateLevel(seed);
        }

        public void OnGenerateNextLevel(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                GenerateLevel(Mathf.RoundToInt(Random.value * 10000));
            }
        }

        private void DebugPlaceKeys(PuzzleGenerator puzzleGenerator)
        {
            foreach (var keyInfo in puzzleGenerator.Keys)
            {
                var key = Instantiate(debugKeyPrefab, generatedLevel);
                key.Key = keyInfo;
                key.transform.position = DungeonGrid.LocalWorldPosition(keyInfo.Coordinates);
                key.name = $"Key {keyInfo.Id}";
            }
        }

        private void DebugPlaceDoors(PuzzleGenerator puzzleGenerator)
        {
            foreach (var doorInfo in puzzleGenerator.Doors)
            {
                var door = Instantiate(debugDoorPrefab, generatedLevel);
                door.dungeonDoor = doorInfo;
                door.transform.position = DungeonGrid.LocalWorldPosition(doorInfo.Coordinates);
                door.transform.rotation = doorInfo.DirectionFromRoom.AsQuaternion();
                door.name = $"Door connecting sector {doorInfo.Sectors[0]} <=> {doorInfo.Sectors[1]}";

                DungeonGrid.GridPositions[doorInfo.Coordinates].SetFeature(GridMapFeature.Door);
            }
        }

        private void DebugPlaceHallways(HallwayGenerator hallwayGenerator)
        {
            foreach (var hallway in hallwayGenerator.Hallways)
            {
                for (int i = 0, n=hallway.Hallway.Count; i<n; i++)                
                {
                    var tileCoordinates = hallway.Hallway[i];
                    var floor = Instantiate(debugFloorPrefab, generatedLevel);
                    floor.transform.position = DungeonGrid.LocalWorldPosition(tileCoordinates);
                    floor.name = $"Hallway {hallway.Id} Floor {tileCoordinates}";
                    floor.AddComponent<ElevationNoiseSubscriber>();

                    DungeonGrid.GridPositions.Add(tileCoordinates, new GridPosition(hallway.WallDirection(i)));
                }                

                foreach (var wallPosition in hallway.Walls(settings.tileSize, settings.tileSize * 0.5f))
                {
                    var wall = Instantiate(debugWallPrefab, generatedLevel);
                    wall.transform.position = wallPosition.Position;
                    wall.transform.rotation = wallPosition.Rotation;
                    wall.name = $"Hallway {hallway.Id} Wall {wallPosition.Coordinates} Facing {wallPosition.Direction}";
                    wall.AddComponent<ElevationNoiseSubscriber>();

                }
            }
        }

        private void DebugPlaceRooms(RoomGenerator roomGenerator, HallwayGenerator hallwayGenerator)
        {
            foreach (var room in roomGenerator.Rooms)
            {
                DebugPlaceRoom(room, hallwayGenerator);
            }
        }

        void DebugPlaceRoom(DungeonRoom room, HallwayGenerator hallwayGenerator)
        {
            foreach (var tileCoordinates in room.Perimeter)
            {
                var floor = Instantiate(debugFloorPrefab, generatedLevel);
                floor.transform.position = DungeonGrid.LocalWorldPosition(tileCoordinates);
                floor.name = $"Room {room.RoomId} Perimeter {tileCoordinates}";

                var directions = new List<Vector2Int>();

                foreach (var direction in MathExtensions.CardinalDirections)
                {
                    var perimeterNeighbour = tileCoordinates + direction;
                    if (room.Contains(perimeterNeighbour)) continue;

                    bool isInHall = false;
                    foreach (var hall in hallwayGenerator.Hallways)
                    {
                        if (hall.IsHallExit(tileCoordinates) && hall.Contains(perimeterNeighbour))
                        {
                            isInHall = true;
                            break;
                        }
                    }

                    if (isInHall) continue;

                    directions.Add(direction);

                    var wallPosition = WallPosition.From(tileCoordinates, direction, settings.tileSize, settings.tileSize * 0.5f);
                    var wall = Instantiate(debugWallPrefab, generatedLevel);
                    wall.transform.position = wallPosition.Position;
                    wall.transform.rotation = wallPosition.Rotation;
                    wall.name = $"Room {room.RoomId} Wall {wallPosition.Coordinates} Facing {wallPosition.Direction}";
                    wall.AddComponent<ElevationNoiseSubscriber>();

                }

                if (tileCoordinates.x >= 0 && tileCoordinates.y >= 0) {
                    DungeonGrid.GridPositions.Add(tileCoordinates, new GridPosition(directions));
                }
            }

            foreach (var tileCoordinates in room.Interior)
            {
                var floor = Instantiate(debugFloorPrefab, generatedLevel);
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
                floor.name = $"Room {room.RoomId} Interior {tileCoordinates}";
                floor.AddComponent<ElevationNoiseSubscriber>();

                if (tileCoordinates.x >= 0 && tileCoordinates.y >= 0)
                {
                    DungeonGrid.GridPositions.Add(tileCoordinates, new GridPosition());
                }
            }
        }
    }
}