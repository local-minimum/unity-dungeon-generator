using UnityEngine;

namespace ProcDungeon {
    public struct WallPosition
    {
        public Vector2Int Coordinates;
        public Vector2Int Direction;
        public Vector3 Position;
        public Quaternion Rotation;

        public WallPosition(Vector2Int coordinates, Vector2Int direction, Vector3 position, Quaternion quaternion)
        {
            Coordinates = coordinates;
            Direction = direction;
            Position = position;
            Rotation = quaternion;
        }
    }
}
