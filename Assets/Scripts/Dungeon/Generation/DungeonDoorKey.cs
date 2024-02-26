using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon
{
    public class DungeonDoorKey : DungeonItem
    {
        public readonly DungeonDoor Door;
        public readonly int SpawnSector;

        public DungeonDoorKey(DungeonDoor door, Vector2Int spawn, int spawnSector)
        {
            Door = door;
            SpawnPosition = spawn;
            Coordinates = spawn;
            SpawnSector = spawnSector;
            Name = "key";
            Id = $"Specific Key to {door}";
        }

        override public string ToString() => $"<Key for: {Door}; Spawn: {SpawnSector} / {SpawnPosition}>";
    }
}
