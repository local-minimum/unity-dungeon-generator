using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon
{
    public abstract class AbstractDoorController : MonoBehaviour
    {
        abstract public void Open();
        public abstract void Close();
        public abstract void Toggle();

        [HideInInspector]
        public DungeonDoor dungeonDoor;

    }
}
