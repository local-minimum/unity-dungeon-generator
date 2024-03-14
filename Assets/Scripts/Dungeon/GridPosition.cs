using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon.World
{
    public enum GridMapFeature { None, Door, Treasure, Teleporter, IllusaryWall, Other };
    public enum GridMapCategory { None, Room, Hallway, Hub, Secret };

    public class GridPosition
    {
        public bool Seen;
        public GridMapFeature Feature;
        public GridMapCategory Category;
        public int CategoryId;
        public bool NorthWall;
        public bool WestWall;
        public bool EastWall;
        public bool SouthWall;        
        private GameObject MapTile;
        private GameObject FeatureTile;
        public DungeonDoor Door { get; private set; }

        public List<DungeonDoorKey> Keys {  get; private set; } = new List<DungeonDoorKey>();

        public void Visit()
        {
            Seen = true;
            SyncMapLayer();
        }

        public void SetDoor(DungeonDoor door)
        {
            Door = door;
            Feature = GridMapFeature.Door;
        }

        public void AddKey(DungeonDoorKey key)
        {
            Keys.Add(key);
        }

        public void SetFeature(GridMapFeature feature)
        {
            Feature = feature;
            if (Feature != GridMapFeature.Door)
            {
                Door = null;
            }
        }

        public void SetFeatureTile(GameObject featureTile)
        {
            FeatureTile = featureTile;
            SyncMapLayer();
        }

        public void SetMapTile(GameObject mapTile)
        {
            MapTile = mapTile;
            SyncMapLayer();
        }

        void SyncMapLayer()
        {
            if (MapTile != null)
            {
                MapTile.layer = LayerMask.NameToLayer(Seen ? "VisibleMap" : "InvisibleMap");
            }
            if (FeatureTile != null)
            {
                FeatureTile.layer = LayerMask.NameToLayer(Seen ? "VisibleMap" : "InvisibleMap");
            }
        }

        public GridPosition(
            GridMapFeature feature = GridMapFeature.None,
            GridMapCategory category = GridMapCategory.None,
            int categoryId = -1
        )
        {
            Seen = false;
            MapTile = null;
            Feature = feature;
            Category = category;
            CategoryId = categoryId;
        }

        public GridPosition(
            bool northWall, 
            bool westWall, 
            bool eastWall, 
            bool southWall, 
            GridMapFeature feature = GridMapFeature.None, 
            GridMapCategory category = default, 
            int categoryId = -1
        )
        {
            NorthWall = northWall;
            WestWall = westWall;
            EastWall = eastWall;
            SouthWall = southWall;
            Seen = false;
            MapTile = null;
            Feature = feature;
            Category = category;
            CategoryId = categoryId;
        }

        public GridPosition(
            IEnumerable<Vector2Int> directions, 
            GridMapFeature feature = GridMapFeature.None, 
            GridMapCategory category = GridMapCategory.None, 
            int categoryId = -1
        )
        {
            NorthWall = directions.Contains(Vector2Int.up);
            WestWall = directions.Contains(Vector2Int.left);
            EastWall = directions.Contains(Vector2Int.right);
            SouthWall = directions.Contains(Vector2Int.down);
            Seen = false;
            MapTile = null;
            Feature = feature;
            Category = category;
            CategoryId = categoryId;
        }

        public IEnumerable<Vector2Int> WallDirections()
        {
            if (NorthWall)
            {
                yield return Vector2Int.up;
            }
            if (SouthWall)
            {
                yield return Vector2Int.down;
            }
            if (WestWall)
            {
                yield return Vector2Int.left;
            }
            if (EastWall)
            {
                yield return Vector2Int.right;
            }
        }

        public string WallID {
            get
            {
                string id = "";
                if (NorthWall)
                {
                    id += "N";
                }
                if (SouthWall)
                {
                    id += "S";
                }
                if (WestWall)
                {
                    id += "W";
                }
                if (EastWall)
                {
                    id += "E";
                }
                return id;
            }
        }

        private string FeatureID
        {
            get
            {
                switch (Feature)
                {
                    case GridMapFeature.Door:
                        return "D";
                    case GridMapFeature.Treasure:
                        return "Tr";
                    case GridMapFeature.Teleporter:
                        return "Tp";
                    case GridMapFeature.IllusaryWall:
                        return "I";
                    case GridMapFeature.Other:
                        return "O";
                    default:
                        return "-";
                }
            }
        }

        public string CategoryID
        {
            get
            {
                switch (Category)
                {
                    case GridMapCategory.Room:
                        return $"R{CategoryId}";
                    case GridMapCategory.Hallway:
                        return $"H{CategoryId}";
                    case GridMapCategory.Hub:
                        return $"Hub{CategoryId}";
                    case GridMapCategory.Secret:
                        return $"S{CategoryId}";
                    default:
                        return "-";
                }
            }
        }

        public string MapTileFeatureId => $"{FeatureID} {WallID}";
        public string FullId(Vector2Int coordinates) => $"{CategoryID}|{FeatureID}|{WallID}|{coordinates}";
    }
}
