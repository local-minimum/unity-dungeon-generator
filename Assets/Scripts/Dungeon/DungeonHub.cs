using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace ProcDungeon.World
{
    public class DungeonHub : Singleton<DungeonHub>
    {
        public class TeleporterCoordinates
        {
            public Vector2Int HubCoordinates;
            public Vector2Int LevelCoordinates;
            public TeleporterCoordinates(Teleporter t) {
                HubCoordinates = t.Coordinates;
                LevelCoordinates = t.PairedTeleporter.Coordinates;
            }
        }

        public Teleporter TeleporterPrefab;
        public SavePlace SavePrefab;

        private DungeonRoom room;
        List<Vector2Int> teleporterLocations = new List<Vector2Int>();
        List<Teleporter> teleporters = new List<Teleporter>();
        public Vector2Int Center { get; private set; }

        SavePlace savePlace;

        public IEnumerable<TeleporterCoordinates> TeleporterPlacements => teleporters.Select(t => t == null ? null : new TeleporterCoordinates(t));            

        public bool AddTeleporterPair(Vector2Int levelCoordinates, Vector2Int lookDirection, out Teleporter levelTeleporter)
        {
            var dungeonGrid = DungeonLevelGenerator.instance.DungeonGrid;
            if (!dungeonGrid.ValidTeleporterPosition(levelCoordinates, lookDirection))
            {
                levelTeleporter = null;
                return false;
            }

            if (!AddTeleporter(out var hubTeleporter))
            {
                levelTeleporter = null;
                return false;
            }

            levelTeleporter = Instantiate(TeleporterPrefab);
            levelTeleporter.transform.position = dungeonGrid.LocalWorldPosition(levelCoordinates);
            levelTeleporter.transform.rotation = dungeonGrid.LocalWorldRotation(lookDirection);

            levelTeleporter.HubSide = false;
            levelTeleporter.Coordinates = levelCoordinates;
            levelTeleporter.ExitDirection = lookDirection * -1; 

            hubTeleporter.PairedTeleporter = levelTeleporter;
            levelTeleporter.PairedTeleporter = hubTeleporter;

            hubTeleporter.ShowView();
            levelTeleporter.ShowView();

            dungeonGrid.Teleporters.Add(levelTeleporter);

            return true;
        }

        bool AddTeleporter(out Teleporter teleporter)
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
                teleporter.name = $"Teleporter Slot #{slot}";

                if (add)
                {
                    teleporters.Add(teleporter);
                } else
                {
                    teleporters[slot] = teleporter;
                }

                var direction = (teleporterLocations[slot] - Center).MainDirection();

                var dungeonGrid = DungeonLevelGenerator.instance.DungeonGrid;
                teleporter.transform.position = dungeonGrid.LocalWorldPosition(teleporterLocations[slot]);
                teleporter.transform.rotation = dungeonGrid.LocalWorldRotation(direction);

                teleporter.HubSide = true;
                teleporter.Coordinates = teleporterLocations[slot];
                teleporter.ExitDirection = direction * -1;

                dungeonGrid.Teleporters.Add(teleporter);

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

                var dungeonGrid = DungeonLevelGenerator.instance.DungeonGrid;

                dungeonGrid.Teleporters.Remove(teleporter);
                dungeonGrid.Teleporters.Remove(teleporter.PairedTeleporter);

                Destroy(teleporter.PairedTeleporter.gameObject);
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

                Center = center;

                if (savePlace == null)
                {
                    savePlace = Instantiate(SavePrefab, transform);
                }

                var dungeonGrid = DungeonLevelGenerator.instance.DungeonGrid;                
                savePlace.transform.position = dungeonGrid.LocalWorldPosition(center);
            }
        }
    }
}
