using UnityEngine;
using ProcDungeon;
using UnityEngine.UIElements;
using System.Linq;

public class DungeonLevelGenerator : MonoBehaviour
{
    [SerializeField]
    PlayerController playerControllerPrefab;

    [SerializeField]
    DungeonLevelSetting settings;

    [SerializeField]
    int seed = 1234;

    [SerializeField]
    GameObject debugFloorPrefab;

    [SerializeField]
    GameObject debugWallPrefab;

    [SerializeField]
    Transform generatedLevel;

    public PlayerController PlayerController { get; private set; }
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

        DebugPlaceHallways(hallwayGenerator);

        DebugPlaceRooms(roomGenerator, hallwayGenerator);

        SpawnPlayer(roomGenerator);
    }

    private void SpawnPlayer(RoomGenerator roomGenerator)
    {
        DungeonRoom spawnRoom;

        var spawnPosition = PlayerController.ChooseStartPosition(roomGenerator.Rooms, DungeonGrid.Dungeon, out spawnRoom);
        var lookDirections = spawnRoom
            .DirectionToExits(spawnPosition)
            .OrderBy(direction => Mathf.Min(Mathf.Abs(direction.x), Mathf.Abs(direction.y)))
            .ToList();

        if (PlayerController == null)
        {
            PlayerController = Instantiate(playerControllerPrefab, transform);
        }

        PlayerController.DungeonGrid = DungeonGrid;

        Debug.Log($"Player spawns at {spawnPosition} in room {spawnRoom}");
        
        if (lookDirections.Count > 0)
        {
            PlayerController.Teleport(spawnPosition, lookDirections.First().MainDirection());
        }
        else
        {
            PlayerController.Teleport(spawnPosition, MathExtensions.RandomDirection());
        }
    }

    private void Start()
    {
        GenerateLevel(seed);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateLevel(Mathf.RoundToInt(Random.value * 10000));
        }
    }
}
