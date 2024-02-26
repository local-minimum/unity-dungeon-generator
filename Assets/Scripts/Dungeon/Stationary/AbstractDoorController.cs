using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.World
{
    public abstract class AbstractDoorController : MonoBehaviour
    {
        abstract public void Open();
        public abstract void Close();

        /** Returns open = true */
        public abstract bool Toggle();

        [HideInInspector]
        public DungeonDoor dungeonDoor;

    }
}
