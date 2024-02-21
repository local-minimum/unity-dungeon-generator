using ProcDungeon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGrid 
{
    public readonly DungeonGridLayer Dungeon;
    public readonly DungeonLevelSetting LevelSetting;

    public DungeonGrid(DungeonGridLayer dungeon, DungeonLevelSetting levelSetting)
    {
        Dungeon = dungeon;
        LevelSetting = levelSetting;
    }

    public Vector3 LocalWorldPosition(Vector2Int gridCoordinates, float elevation = 0f) =>
        new Vector3(gridCoordinates.x * LevelSetting.tileSize, elevation, gridCoordinates.y * LevelSetting.tileSize);

    public Quaternion LocalWorldRotation(Vector2Int direction) => Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);

    // TODO: Add logic when we have occupancy rules
    public bool Accessible(Vector2Int coordinates, EntityType entity) => Dungeon.InBounds(coordinates) && Dungeon.Accessible(coordinates);
}
