using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace ProcDungeon.World
{
    public class DungeonHub : Singleton<DungeonHub>
    {
        public Teleporter TeleporterPrefab;

        private DungeonRoom room;
        List<Vector2Int> teleporterLocations = new List<Vector2Int>();
        List<Teleporter> teleporters = new List<Teleporter>();
        public Vector2Int Fire { get; private set; }

        public bool AddTeleporter(out Teleporter teleporter)
        {
            if (Teleporters < teleporterLocations.Count)
            {
                var slot = teleporters.IndexOf(null);
                bool add = slot < 0;
                if (slot < 0)
                {                    
                    slot = teleporters.Count;                
                }

                teleporter = Instantiate(TeleporterPrefab, transform);

                if (add)
                {
                    teleporters.Add(teleporter);
                } else
                {
                    teleporters[slot] = teleporter;
                }

                var dungeonGrid = DungeonLevelGenerator.instance.DungeonGrid;
                teleporter.transform.position = dungeonGrid.LocalWorldPosition(teleporterLocations[slot]);
                teleporter.transform.rotation = dungeonGrid.LocalWorldRotation((teleporterLocations[slot] - Fire).MainDirection());

                teleporter.HubSide = true;
                teleporter.Coordinates = teleporterLocations[slot];

                return true;
            }

            teleporter = null;
            return false;
        }

        public bool DestroyTeleporter(Teleporter teleporter)
        {
            if (Teleporters < 1) return false;

            if (teleporters.Contains(teleporter))
            {
                teleporters[teleporters.IndexOf(teleporter)] = null;   
                Destroy(teleporter.gameObject);
                return true;
            }

            return false;
        }

        public int Teleporters => teleporters.Count(t => t != null);

        public DungeonRoom Room {
            set { 
                room = value; 
                var center = room.Center;
                var bbox = room.BoundingBox;
                teleporterLocations.Clear();
                teleporterLocations.Add(new Vector2Int(bbox.min.x, center.y));
                teleporterLocations.Add(new Vector2Int(bbox.max.x - 1, center.y));
                teleporterLocations.Add(new Vector2Int(center.x, bbox.min.y));
                teleporterLocations.Add(new Vector2Int(center.x, bbox.max.y - 1));

                Fire = center;

                foreach (var location in teleporterLocations)
                {
                    Debug.Log(location);
                }
            }
        }

        public Vector2Int GetTeleporterLocation(int idx) => teleporterLocations[idx];
    }
}
