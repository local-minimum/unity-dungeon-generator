using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using ProcDungeon.World;
using System.Collections.Generic;


namespace ProcDungeon
{
    public class DungeonLevelGenerator : Singleton<DungeonLevelGenerator>
    {
        [SerializeField]
        DungeonLevelSetting settings;

        [SerializeField]
        int seed = 1234;
        public int Seed => seed;

        public DungeonGrid DungeonGrid { get; private set; }

        void GenerateLevel(int seed)
        {
            Debug.Log($"Starting level generation (Seed {seed})");

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
            var spawnPosition = PlayerStartSelector.ChooseStartPosition(roomGenerator.Rooms, DungeonGrid.Dungeon, out spawnRoom);
            var spawnLookDirection = SpawnLookDirection(spawnPosition, spawnRoom);

            // DOORS
            var puzzleGenerator = new PuzzleGenerator(roomGenerator, spawnRoom, spawnPosition);
            var nDoors = puzzleGenerator.AddDoors(Random.Range(2, 4));
            Debug.Log($"Added {nDoors} doors to level");
            DungeonGrid.Doors = puzzleGenerator.Doors;

            // PLACE WORLD
            RegisterHallways(hallwayGenerator);
            RegisterRooms(roomGenerator, hallwayGenerator);

            RegisterDoors(puzzleGenerator);
            RegisterKeys(puzzleGenerator);

            // SpawnPlayer(spawnPosition, spawnLookDirection, spawnRoom);
            DungeonGrid.PlayerPosition = spawnPosition;
            DungeonGrid.PlayerLookDirection = spawnLookDirection;

            // SETUP MAP
            DungeonGrid.Hub = roomGenerator.CreateHub();
            RegisterRoom(DungeonGrid.Hub, hallwayGenerator);
            DungeonHub.instance.Room = DungeonGrid.Hub;

            // Note that this must be after all things have been registered            
            Debug.Log($"Done level generation (Seed {seed})");

            DungeonLevelInstancer.instance.InstaniateLevel(DungeonGrid);
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
        
        private void RegisterKeys(PuzzleGenerator puzzleGenerator)
        {
            foreach (var keyInfo in puzzleGenerator.Keys)
            {
                DungeonGrid.GridPositions[keyInfo.Coordinates].AddKey(keyInfo);
            }
        }

        private void RegisterDoors(PuzzleGenerator puzzleGenerator)
        {
            foreach (var doorInfo in puzzleGenerator.Doors)
            {
                DungeonGrid.GridPositions[doorInfo.Coordinates].SetDoor(doorInfo);
            }
        }

        private void RegisterHallways(HallwayGenerator hallwayGenerator)
        {
            foreach (var hallway in hallwayGenerator.Hallways)
            {
                for (int i = 0, n=hallway.Hallway.Count; i<n; i++)                
                {
                    
                    var tileCoordinates = hallway.Hallway[i];

                    DungeonGrid.GridPositions.Add(
                        tileCoordinates, 
                        new GridPosition(
                            hallway.WallDirection(i), 
                            category: GridMapCategory.Hallway,
                            categoryId: hallway.Id
                        )
                    );
                }                
            }
        }

        private void RegisterRooms(RoomGenerator roomGenerator, HallwayGenerator hallwayGenerator)
        {
            foreach (var room in roomGenerator.Rooms)
            {
                RegisterRoom(room, hallwayGenerator);
            }
        }

        void RegisterRoom(DungeonRoom room, HallwayGenerator hallwayGenerator)
        {
            foreach (var tileCoordinates in room.Perimeter)
            {

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
                }

                var isHub = tileCoordinates.x < 0 && tileCoordinates.y < 0;

                if (isHub && DungeonGrid.GridPositions.ContainsKey(tileCoordinates)) { continue; }

                DungeonGrid.GridPositions.Add(
                    tileCoordinates, 
                    new GridPosition(
                        directions, 
                        category: isHub ? GridMapCategory.Hub : GridMapCategory.Room,
                        categoryId: room.RoomId
                    )
                );
            }

            foreach (var tileCoordinates in room.Interior)
            {

                var isHub = tileCoordinates.x < 0 && tileCoordinates.y < 0;

                if (isHub && DungeonGrid.GridPositions.ContainsKey(tileCoordinates)) { continue; }

                DungeonGrid.GridPositions.Add(
                    tileCoordinates, 
                    new GridPosition(
                        category: isHub ? GridMapCategory.Hub : GridMapCategory.Room,
                        categoryId: room.RoomId
                    )
                );
            }
        }
    }
}