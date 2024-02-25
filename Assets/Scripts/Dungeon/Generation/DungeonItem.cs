using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon
{
    public abstract class DungeonItem
    {
        public Vector2Int SpawnPosition { get; protected set; }
        public Vector2Int Location;
       
    }
}
