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

        public static WallPosition From(Vector2Int pt, Vector2Int direction, float scale, float elevation)
        {
            var offset = new Vector3(pt.x + 0.5f * direction.x, 0, pt.y + 0.5f * direction.y);

            offset *= scale;


            return new WallPosition(
                pt, 
                direction, 
                offset + Vector3.up * elevation, 
                direction.AsQuaternion()
                //Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y))
                );
        }
    }
}
