using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon
{
    public class DungeonHallway
    {
        public readonly int Id;
        public readonly DungeonRoom SourceRoom;
        public readonly Vector2Int Source;
        public readonly Vector2Int SourceExit;

        public readonly DungeonRoom DestinationRoom;
        public readonly Vector2Int Destination;
        public readonly Vector2Int DestinationExit;

        public bool Valid { get; set; }
        public List<Vector2Int> Hallway {  get; private set; }

        public bool Contains(Vector2Int pt) => Hallway.Contains(pt);
        public bool IsHallExit(Vector2Int pt) => SourceExit == pt || DestinationExit == pt;

        public DungeonHallway(DungeonRoom sourceRoom, Vector2Int source, Vector2Int sourceExit, DungeonRoom destinationRoom, Vector2Int destination, Vector2Int destinationExit, int id)
        {
            SourceRoom = sourceRoom;
            Source = source;
            SourceExit = sourceExit;
            DestinationRoom = destinationRoom;
            Destination = destination;
            DestinationExit = destinationExit;
            Id = id;
            Hallway = new List<Vector2Int>();
        }

        public bool Connects(DungeonRoom room, DungeonRoom other) => 
            SourceRoom == room && DestinationRoom == other
            || SourceRoom == other && DestinationRoom == room;


        public static WallPosition WallPosition(Vector2Int pt, Vector2Int direction, float scale, float elevation)
        {
            var offset = new Vector3(pt.x + 0.5f * direction.x, 0, pt.y + 0.5f * direction.y);

            offset *= scale;


            return new WallPosition(pt, direction, offset + Vector3.up * elevation, Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)));
        }

        public IEnumerable<WallPosition> Walls(float scale, float elevation)
        {
            var currentDirection = Source - SourceExit;

            for (int i = 0, n=Hallway.Count; i < n; ++i)
            {
                var pt = Hallway[i];
                var directions = new List<Vector2Int> {
                    new Vector2Int(-currentDirection.y, currentDirection.x),
                    new Vector2Int(currentDirection.y, -currentDirection.x)
                };

                if (pt + currentDirection != DestinationExit)
                {
                    directions.Add(currentDirection);
                }

                foreach(var direction in directions)
                {
                    bool noWall = false;
                    var neigbour = pt + direction;
                    for (int j=0; j<n; ++j)
                    {
                        if (Hallway[j] == neigbour)
                        {
                            noWall = true;
                            break;
                        }
                    }

                    if (noWall) continue;

                    yield return WallPosition(pt, direction, scale, elevation);
                }

                if (i + 1 < n)
                {
                    currentDirection = Hallway[i + 1] - pt;
                }
            }
        }
    }
    
}
