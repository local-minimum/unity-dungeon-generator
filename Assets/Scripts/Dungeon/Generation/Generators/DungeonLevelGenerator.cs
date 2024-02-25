using UnityEngine;
using ProcDungeon;
using System.Linq;
using UnityEngine.InputSystem;

public class DungeonLevelGenerator : MonoBehaviour
{
    [SerializeField]
    DungeonLevelSetting settings;

    [SerializeField]
    int seed = 1234;

    [SerializeField]
    GameObject debugFloorPrefab;

    [SerializeField]
    GameObject debugWallPrefab;

    [SerializeField]
    AbstractDoorController debugDoorPrefab;

    [SerializeField]
    Transform generatedLevel;

    public PlayerController PlayerController;
    public DungeonGrid DungeonGrid { get; private set; }

    void GenerateLevel(int seed)
    {
        foreach (Transform t in generatedLevel.transform)
        {
            Destroy(t.gameObject);
        }

        Random.InitState(seed);

        var grid = new DungeonGridLayer(settings);

        DungeonGrid = new DungeonGrid(grid, settings);

        var segmenter = new GridSegmenter(ref settings);
        segmenter.Segment();
        
        var roomGenerator = new RoomGenerator(grid, ref settings);

        roomGenerator.PlaceRooms(segmenter);

        var hallwayGenerator = new HallwayGenerator(grid, roomGenerator.Rooms, ref settings);
        hallwayGenerator.MakeHallways();
        
        roomGenerator.CalculateHubSeparations();

        for (int i = 0, n=Random.Range(2, 4); i< n; i++)
        {
            hallwayGenerator.AddExtraHallway();
            roomGenerator.CalculateHubSeparations();
        }

        // TODO: Add Range to settings
        for (int i = 0, n=Random.Range(4, 8); i<n; i++)
        {
            hallwayGenerator.AddDeadEndHallway();
        }

        DungeonRoom spawnRoom;
        var spawnPosition = PlayerController.ChooseStartPosition(roomGenerator.Rooms, DungeonGrid.Dungeon, out spawnRoom);
        var spawnLookDirection = SpawnLookDirection(spawnPosition, spawnRoom);

        var puzzleGenerator = new PuzzleGenerator(roomGenerator, spawnRoom, spawnPosition);
        var nDoors = puzzleGenerator.AddDoors(Random.Range(2, 4));
        Debug.Log($"Added {nDoors} doors to level");
        DungeonGrid.Doors = puzzleGenerator.Doors;

        DebugPlaceHallways(hallwayGenerator);

        DebugPlaceRooms(roomGenerator, hallwayGenerator);

        DebugPlaceDoors(puzzleGenerator);

        SpawnPlayer(spawnPosition, spawnLookDirection, spawnRoom);
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

    private void DebugPlaceDoors(PuzzleGenerator puzzleGenerator)
    {
        foreach (var doorInfo in puzzleGenerator.Doors)
        {
            var door = Instantiate(debugDoorPrefab, generatedLevel);
            door.dungeonDoor = doorInfo;
            door.transform.position = DungeonGrid.LocalWorldPosition(doorInfo.Coordinates);
            door.transform.rotation = doorInfo.DirectionFromRoom.AsQuaternion();
            door.name = $"Door connecting sector {doorInfo.Sectors[0]} <=> {doorInfo.Sectors[1]}";
        }
    }

    private void DebugPlaceHallways(HallwayGenerator hallwayGenerator)
    {
        foreach (var hallway in hallwayGenerator.Hallways)
        {
            foreach (var tileCoordinates in hallway.Hallway)
            {
                var floor = Instantiate(debugFloorPrefab, generatedLevel);
                floor.transform.position = DungeonGrid.LocalWorldPosition(tileCoordinates);
                floor.name = $"Hallway {hallway.Id} Floor {tileCoordinates}";

            }

            foreach (var wallPosition in hallway.Walls(settings.tileSize, settings.tileSize * 0.5f))
            {
                var wall = Instantiate(debugWallPrefab, generatedLevel);
                wall.transform.position = wallPosition.Position;
                wall.transform.rotation = wallPosition.Rotation;
                wall.name = $"Hallway {hallway.Id} Wall {wallPosition.Coordinates} Facing {wallPosition.Direction}";
            }
        }
    }

    private void DebugPlaceRooms(RoomGenerator roomGenerator, HallwayGenerator hallwayGenerator)
    {
        foreach (var room in roomGenerator.Rooms)
        {
            foreach (var tileCoordinates in room.Perimeter)
            {
                var floor = Instantiate(debugFloorPrefab, generatedLevel);
                floor.transform.position = DungeonGrid.LocalWorldPosition(tileCoordinates);
                floor.name = $"Room {room.RoomId} Perimeter {tileCoordinates}";

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

                    var wallPosition = WallPosition.From(tileCoordinates, direction, settings.tileSize, settings.tileSize * 0.5f);
                    var wall = Instantiate(debugWallPrefab, generatedLevel);
                    wall.transform.position = wallPosition.Position;
                    wall.transform.rotation = wallPosition.Rotation;
                    wall.name = $"Room {room.RoomId} Wall {wallPosition.Coordinates} Facing {wallPosition.Direction}";

                }
            }

            foreach (var tileCoordinates in room.Interior)
            {
                var floor = Instantiate(debugFloorPrefab, generatedLevel);
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
                floor.name = $"Room {room.RoomId} Interior {tileCoordinates}";
            }
        }
    }

}
