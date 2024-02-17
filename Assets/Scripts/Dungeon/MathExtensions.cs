using UnityEngine;

namespace ProcDungeon
{
    public static class MathExtensions
    {
        #region int 
        private static int Sign(this int value)
        {
            if (value < 0) return -1;
            return value > 0 ? 1 : 0;
        }

        #endregion
        
        #region Vector2Int
        public static readonly Vector2Int[] CardinalDirections = new Vector2Int[] {
            Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down,
        };

        public static Vector2Int RandomDirection() => CardinalDirections[Random.Range(0, 4)];

        public static int SmallestDimension(this Vector2Int vector) => Mathf.Min(Mathf.Abs(vector.x), Mathf.Abs(vector.y));

        public static Vector2Int OrthoIntersection(this Vector2Int point, Vector2Int target, Vector2Int direction)
        {
            var candidate = new Vector2Int(point.x, target.y);
            var diff = candidate - point;
            if (diff.x * direction.x + diff.y * direction.y == 0) return candidate;

            return new Vector2Int(target.x, point.y);
        }

        public static Vector2Int MainDirection(this Vector2Int source, Vector2Int destination) =>
            (destination - source).MainDirection();        

        public static Vector2Int MainDirection(this Vector2Int direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                direction.x = direction.x.Sign();
                direction.y = 0;
            }
            else
            {
                direction.x = 0;
                direction.y = direction.y.Sign();
            }
            return direction;
        }

        public static bool IsUnitVector(this Vector2Int vector) => 
            Mathf.Abs(vector.x) + Mathf.Abs(vector.y) == 1;

        public static bool IsOrthogonalCardinal(this Vector2Int cardinal1, Vector2Int cardinal2) =>
            cardinal1.x == 0 && cardinal2.y == 0 || cardinal1.y == 0 && cardinal2.x == 0;

        public static bool IsInverseDirection(this Vector2Int direction1, Vector2Int direction2) =>
            direction1.x == -direction2.x && direction1.y == -direction2.y;

        public static bool IsCCWRotationOf(this Vector2Int cardinal1, Vector2Int cardinal2) =>
           cardinal1.RotateCCW() == cardinal2;

        public static bool IsCWRotationOf(this Vector2Int cardinal1, Vector2Int cardinal2) =>
           cardinal1.RotateCW() == cardinal2;

        public static Vector2Int RotateCW(this Vector2Int direction) =>
            new Vector2Int(-direction.y, direction.x);

        public static Vector2Int RotateCCW(this Vector2Int direction) =>
            new Vector2Int(direction.y, -direction.x);

        public static int ManhattanDistance(this Vector2Int point, Vector2Int other) =>
             Mathf.Abs(point.x - other.x) + Mathf.Abs(point.y - other.y);

        public static Vector2Int[] AsUnitComponents(this Vector2Int direction) => new Vector2Int[] { 
            new Vector2Int(direction.x.Sign(), 0),
            new Vector2Int(0, direction.y.Sign())
        };
        #endregion

        #region RectInt 
        public static int Area(this RectInt rect) => rect.width * rect.height;

        public static void ApplyForRect(this RectInt rect, System.Action<int, int> action) { 
            for (int y = rect.min.y, yMax=rect.max.y; y<yMax; y++)
            {
                for (int x = rect.min.x, xMax = rect.max.x; x < xMax; x++)
                {
                    action(x, y);
                }
            }
        }

        public static bool UnitesToRect(this RectInt rect, RectInt other) => 
            rect.min.x == other.min.x && rect.max.x == other.max.x && (rect.min.y == other.max.y || rect.max.y == other.min.y)
            || rect.min.y == other.min.y && rect.max.y == other.max.y && (rect.min.x == other.max.x || rect.max.x == other.min.x);
        
        
        #endregion
    }
}