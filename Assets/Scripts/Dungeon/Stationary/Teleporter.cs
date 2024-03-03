using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.World
{
    public class Teleporter : MonoBehaviour
    {
        public bool HubSide { get; set; }
        public Vector2Int Coordinates { get; set; }

        public Teleporter PairedTeleporter { get; set; }
    }
}
