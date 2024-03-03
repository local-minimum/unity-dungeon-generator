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
    public List<DungeonDoor> Doors { get; set; } = new List<DungeonDoor>();
    public List<Teleporter> Teleporters { get; set; } = new List<Teleporter>();
    public DungeonRoom Hub { get; set; }

    public DungeonGrid(DungeonGridLayer dungeon, DungeonLevelSetting levelSetting)
    {
        Dungeon = dungeon;
        LevelSetting = levelSetting;
    }

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
