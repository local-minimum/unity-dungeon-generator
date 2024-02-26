using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.World {
    public class SpecificKey : AbstractItem
    {
        public DungeonDoorKey Key
        {
            get { return Item as DungeonDoorKey; }
            set { Item = value; }
        }
    }
}