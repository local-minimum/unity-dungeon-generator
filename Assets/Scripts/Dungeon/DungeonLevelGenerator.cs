using UnityEngine;
using ProcDungeon;

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
    Transform generatedLevel;

    void GenerateLevel(int seed)
    {
        foreach (Transform t in generatedLevel.transform)
        {
            Destroy(t.gameObject);
        }

        Random.InitState(seed);

        var segmenter = new GridSegmenter();
        segmenter.Segment(ref settings);

        var roomGenerator = new RoomGenerator();
        roomGenerator.PlaceRooms(segmenter, ref settings);

        var hallwayGenerator = new HallwayGenerator();
        hallwayGenerator.MakeHallways(roomGenerator.Rooms, ref settings);

        DebugPlaceHallways(hallwayGenerator);

        DebugPlaceRooms(roomGenerator, hallwayGenerator);
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
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
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
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
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

                    var wallPosition = DungeonHallway.WallPosition(tileCoordinates, direction, settings.tileSize, settings.tileSize * 0.5f);
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
