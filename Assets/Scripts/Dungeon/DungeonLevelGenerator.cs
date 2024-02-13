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

        DebugPlaceRooms(roomGenerator);
        DebugPlaceHallways(hallwayGenerator);
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
                floor.name = $"Hallway {hallway.Id} {tileCoordinates}";

            }
        }
    }

    private void DebugPlaceRooms(RoomGenerator roomGenerator)
    {
        foreach (var room in roomGenerator.Rooms)
        {
            foreach (var tileCoordinates in room.Perimeter)
            {
                var floor = Instantiate(debugFloorPrefab, generatedLevel);
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
                floor.name = $"Room {room.RoomId} Perimeter {tileCoordinates}";
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
