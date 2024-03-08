using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProcDungeon.World
{
    public enum GridMapFeature { None, Door, Treasure, Teleporter, IllusaryWall, Other };

    public class GridPosition
    {
        public bool Seen;
        public GridMapFeature Feature;
        public bool NorthWall;
        public bool WestWall;
        public bool EastWall;
        public bool SouthWall;
        private GameObject MapTile;
        private GameObject FeatureTile;

        public void Visit()
        {
            Seen = true;
            SyncMapLayer();
        }

        public void SetFeature(GridMapFeature feature)
        {
            Feature = feature;
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
        public GridPosition()
        {
            Feature = GridMapFeature.None;
        }

        public GridPosition(bool northWall, bool westWall, bool eastWall, bool southWall, GridMapFeature feature = GridMapFeature.None)
        {
            NorthWall = northWall;
            WestWall = westWall;
            EastWall = eastWall;
            SouthWall = southWall;
            Seen = false;
            Feature = feature;
            MapTile = null;
        }

        public GridPosition(IEnumerable<Vector2Int> directions, GridMapFeature feature = GridMapFeature.None)
        {
            NorthWall = directions.Contains(Vector2Int.up);
            WestWall = directions.Contains(Vector2Int.left);
            EastWall = directions.Contains(Vector2Int.right);
            SouthWall = directions.Contains(Vector2Int.down);
            Seen = false;
            Feature = feature;
            MapTile = null;
        }

        public string GroundID {
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

        public string FullFeatureID => $"{FeatureID} {GroundID}";        
    }
}
