using ProcDungeon;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProcDungeon.World;

public class DungeonGrid 
{
    public readonly DungeonGridLayer Dungeon;
    public readonly DungeonLevelSetting LevelSetting;

    public List<DungeonRoom> Rooms { get; set; } = new List<DungeonRoom>();
    public List<DungeonDoor> Doors { get; set; } = new List<DungeonDoor>();
    public List<Teleporter> Teleporters { get; set; } = new List<Teleporter>();
    public DungeonRoom Hub { get; set; }

    public Vector2Int PlayerPosition { get; set; }
    public Vector2Int PlayerLookDirection { get; set; }

    public Dictionary<Vector2Int, GridPosition> GridPositions { get; set; } = new Dictionary<Vector2Int, GridPosition>();

    bool SafeVisit(Vector2Int coordinates)
    {
        if (GridPositions.ContainsKey(coordinates))
        {
            GridPositions[coordinates].Visit();
            return true;
        }

        return false;
    }

    public void VisitPosition(Vector2Int coordinates, Vector2Int forward, int area = 1, int lineOfSight = 4, bool entireRoom = true)
    {
        // Area
        for (int offX = -area; offX <= area; offX++)
        {
            for (int offY = -area; offY <= area; offY++)
            {
                SafeVisit(coordinates + new Vector2Int(offX, offY));
            }
        }

        // LOS
        for (int idx = area; idx < lineOfSight; idx++)
        {
            var candidate = coordinates + idx * forward;
            if (Doors.Any(door => door.Coordinates == candidate && door.Closed))
            {
                SafeVisit(candidate);
                break;
            }

            if (!SafeVisit(candidate))
            {
                break;
            }
        }

        // Room
        if (entireRoom)
        {
            var room = Rooms.FirstOrDefault(r => r.Contains(coordinates));
            if (room != null)
            {
                foreach (var roomCoordinates in room.Tiles)
                {
                    SafeVisit(roomCoordinates);
                }
            }
        }

        // TODO: Fill out with more stuff
    }

    public DungeonGrid(DungeonGridLayer dungeon, DungeonLevelSetting levelSetting)
    {
        Dungeon = dungeon;
        LevelSetting = levelSetting;
    }

    public Rect BoundingBox =>
        new Rect(Vector2.zero, new Vector2(LevelSetting.gridSizeColumns * LevelSetting.tileSize, LevelSetting.gridSizeRows * LevelSetting.tileSize));
    public Vector3 LocalWorldPosition(Vector2Int gridCoordinates, float elevation = 0f) =>
        new Vector3(gridCoordinates.x * LevelSetting.tileSize, elevation, gridCoordinates.y * LevelSetting.tileSize);

    public Quaternion LocalWorldRotation(Vector2Int direction) => 
        Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);

    // TODO: Add logic when we have occupancy rules
    public bool Accessible(Vector2Int coordinates, EntityType entity) =>
        Hub != null && Hub.Contains(coordinates) && Hub.Center != coordinates
        || Dungeon.InBounds(coordinates) 
        && Dungeon.Accessible(coordinates)
        && !Doors.Any(door => door.Closed && door.Coordinates == coordinates);

    public bool ValidTeleporterPosition(Vector2Int coordinates, Vector2Int direction) =>
        !(Hub != null && Hub.Contains(coordinates))
        && !Teleporters.Any(teleporter => teleporter.Coordinates == coordinates)
        && Accessible(coordinates, EntityType.Player)
        && (!Dungeon.InBounds(coordinates + direction) || Dungeon.IsEmpty(coordinates + direction));
}
