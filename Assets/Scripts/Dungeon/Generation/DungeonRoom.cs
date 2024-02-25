using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon
{
    public class DungeonRoom
    {
        const int VERY_FAR_APART = 1000000;
        const int MAX_PERIMETER_LENGTH = 10000;

        public readonly int RoomId;
        /** Bounding box max exclusive */
        public readonly RectInt BoundingBox;
        /** Actual corners max sides inclusive */
        public readonly Vector2Int[] BoundingCorners;
        public readonly Vector2Int Center;
        public readonly List<DungeonHallway> Exits = new List<DungeonHallway>();
        public int HubSeparation {  get; set; }

        private List<RectInt> _Segments;

        private List<Vector2Int> _Perimeter = new List<Vector2Int>();
        public List<Vector2Int> Perimeter => _Perimeter;

        private List<Vector2Int> _Interior = new List<Vector2Int>();
        public List<Vector2Int> Interior => _Interior;

        public IEnumerable<Vector2Int> Tiles
        {
            get
            {
                foreach (var tile in _Interior) { 
                    yield return tile;
                }
                foreach (var tile in _Perimeter)
                {
                    yield return tile;
                }
            }
        }

        public int Size => _Perimeter.Count + _Interior.Count;

        public bool IsTerminus => Exits.Count(hall => hall.OtherRoom(this) != null) < 2;

        public bool Contains(Vector2Int point) => _Perimeter.Contains(point) || _Interior.Contains(point);

        public override string ToString() => $"<Room {RoomId} {BoundingBox} ({_Segments.Count} segments; {Center} center; {_Perimeter.Count} perimeter; {_Interior.Count} interior)>";        

        public DungeonRoom(int roomId, List<RectInt> segments)
        {
            RoomId = roomId;
            _Segments = segments;

            BoundingBox = CalculateBoundingBox();
            BoundingCorners = new Vector2Int[] {
                BoundingBox.min,
                new Vector2Int(BoundingBox.max.x - 1, BoundingBox.min.y),
                BoundingBox.max - Vector2Int.one,
                new Vector2Int(BoundingBox.min.x, BoundingBox.max.y - 1),
            };

            InitPerimeter();
            InitInterior();

            Center = CalculateCenter();
        }

        RectInt CalculateBoundingBox()
        {
            if (_Segments.Count == 0) return new RectInt();

            var segment = _Segments[0];

            Vector2Int min = segment.min;
            Vector2Int max = segment.max;

            for (int i = 1, n = _Segments.Count; i < n; i++)
            {
                segment = _Segments[i];
                min.x = Mathf.Min(min.x, segment.min.x);
                min.y = Mathf.Min(min.y, segment.min.y);
                max.x = Mathf.Max(max.x, segment.max.x);
                max.y = Mathf.Max(max.y, segment.max.y);
            }

            return new RectInt(min, max - min);
        }

        bool ContainsBySegments(Vector2Int point)
        {
            for (int i = 0, n = _Segments.Count; i<n; i++)
            {
                if (_Segments[i].Contains(point)) return true;
            }
            return false;
        }

        void InitPerimeter()
        {
            if (_Segments.Count == 0) return;

            var prevPerimeterPt = new Vector2Int(-1, -1);
            var perimeterStart = new Vector2Int(-1, -1);
            var perimeterDirection = new Vector2Int(0, 1);

            // Find starting corner
            bool found = false;
            var perimeterPt = BoundingBox.min;
            
            while (perimeterPt.y < BoundingBox.max.y)
            {
                if (ContainsBySegments(perimeterPt))
                {
                    perimeterStart = perimeterPt;
                    found = true;
                    break;
                }

                if (!found)
                {
                    perimeterPt += perimeterDirection;
                }
            }

            if (!found)
            {
                Debug.LogError($"No area of Room {RoomId} touched its bounding box {BoundingBox}");
                return;
            }

            var orthoDirection = perimeterDirection.RotateCW();
            int steps = 0;
            while (steps < MAX_PERIMETER_LENGTH)
            {
                if (perimeterPt != prevPerimeterPt)
                {
                    _Perimeter.Add(perimeterPt);
                    prevPerimeterPt = perimeterPt;
                }

                var orthoCandidate = perimeterPt + orthoDirection;
                if (ContainsBySegments(orthoCandidate))
                {
                    perimeterDirection = orthoDirection;
                    orthoDirection = perimeterDirection.RotateCW();

                    perimeterPt = orthoCandidate;
                     
                } else
                {
                    var paraCandidate = perimeterPt + perimeterDirection;
                    if (ContainsBySegments(paraCandidate))
                    {
                        perimeterPt = paraCandidate;
                    } else
                    {
                        orthoDirection = perimeterDirection;
                        perimeterDirection = perimeterDirection.RotateCCW();
                    }
                }

                if (perimeterPt == perimeterStart)
                {
                    break;
                }

                steps++;
            }
        }
    
        void ApplyForSegments(System.Action<int, int> action)
        {
            for (int i=0, n=_Segments.Count; i<n; i++)
            {
                _Segments[i].ApplyForRect(action);
            }
        }

        void InitInterior()
        {
            ApplyForSegments((x, y) =>
            {
                var point = new Vector2Int(x, y);
                if (!_Perimeter.Contains(point)) _Interior.Add(point);
            });
        }

        Vector2Int CalculateCenter()
        {
            int sumX = 0;
            int sumY = 0;
            int n = 0;

            ApplyForSegments((x, y) => {
                sumX += x;
                sumY += y;
                n++;
            });

            return new Vector2Int(sumX / n, sumY / n);
        }

        public int CenterDistance(DungeonRoom other) => Center.ManhattanDistance(other.Center);
            
        public Vector2Int ClosestBoundingCorner(DungeonRoom other, out Vector2Int otherCorner)
        {
            int bestIdx = 0;
            int closestDistance = VERY_FAR_APART;
            otherCorner = other.BoundingCorners[0];

            for (int i = 1; i < 4; i++)
            {
                var myCandidate = BoundingCorners[i];

                for (int j = 1; j < 4; j++)
                {
                    var otherCandidate = other.BoundingCorners[j];

                    var candidateDistance = myCandidate.ManhattanDistance(otherCandidate);

                    if (candidateDistance < closestDistance)
                    {
                        closestDistance = candidateDistance;
                        bestIdx = i;
                        otherCorner = otherCandidate;
                    }
                }
            }

            return BoundingCorners[bestIdx];
        }

        public Vector2Int ExitDirection(Vector2Int perimeterPoint)
        {
            foreach (var direction in MathExtensions.CardinalDirections)
            {
                if (!Contains(perimeterPoint + direction))
                {
                    return direction;
                }
            }

            Debug.LogError($"{this} lacked exit direction for perimeter point {perimeterPoint}");
            return Vector2Int.zero;
        }

        public IEnumerable<Vector2Int> DirectionToExits(Vector2Int position)
        {
            foreach (var hall in Exits)
            {
                yield return hall.MyRoomExit(this) - position;
            }
        }

        public Vector2Int RandomTile
        {
            get
            {
                var tiles = Tiles.ToList();
                return tiles[Random.Range(0, tiles.Count)];
            }
        }
    }
}