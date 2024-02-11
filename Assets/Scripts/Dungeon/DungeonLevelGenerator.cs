using UnityEngine;
using ProcDungeon;
using Unity.VisualScripting;

public class DungeonLevelGenerator : MonoBehaviour
{
    [SerializeField]
    DungeonLevelSetting settings;

    [SerializeField]
    int seed = 1234;

    [SerializeField]
    GameObject debugFloorPrefab;


    void GenerateLevel()
    {
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
        GenerateLevel();
    }

    private void DebugPlaceHallways(HallwayGenerator hallwayGenerator)
    {
        foreach (var hallway in hallwayGenerator.Hallways)
        {
            foreach (var tileCoordinates in hallway.Hallway)
            {
                var floor = Instantiate(debugFloorPrefab, transform);
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
                var floor = Instantiate(debugFloorPrefab, transform);
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
                floor.name = $"Room {room.RoomId} Perimeter {tileCoordinates}";
            }

            foreach (var tileCoordinates in room.Interior)
            {
                var floor = Instantiate(debugFloorPrefab, transform);
                floor.transform.position = new Vector3(tileCoordinates.x * settings.tileSize, 0, tileCoordinates.y * settings.tileSize);
                floor.name = $"Room {room.RoomId} Interior {tileCoordinates}";
            }
        }
    }
}
