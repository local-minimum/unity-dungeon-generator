using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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
        public DungeonRoom OtherRoom(DungeonRoom room) => SourceRoom == room ? DestinationRoom : SourceRoom;
        public Vector2Int MyRoomExit(DungeonRoom room) => SourceRoom == room ? SourceExit : DestinationExit;
        public Vector2Int MyHallStart(DungeonRoom room) => SourceRoom == room ? Source : Destination;

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

        private List<List<Vector2Int>> WallDirections = new List<List<Vector2Int>>();

        private void InitWallDirections()
        {
            WallDirections.Clear();

            var currentDirection = Source - SourceExit;

            for (int i = 0, n = Hallway.Count; i < n; ++i)
            {
                var pt = Hallway[i];
                var candidateDirections = new List<Vector2Int> {
                    new Vector2Int(-currentDirection.y, currentDirection.x),
                    new Vector2Int(currentDirection.y, -currentDirection.x)
                };

                if (pt + currentDirection != DestinationExit)
                {
                    candidateDirections.Add(currentDirection);
                }

                var wallDirections = new List<Vector2Int>();

                foreach (var direction in candidateDirections)
                {
                    bool noWall = false;
                    var neigbour = pt + direction;
                    for (int j = 0; j < n; ++j)
                    {
                        if (Hallway[j] == neigbour)
                        {
                            noWall = true;
                            break;
                        }
                    }

                    if (noWall) continue;

                    wallDirections.Add(direction);
                }

                WallDirections.Add(wallDirections);

                if (i + 1 < n)
                {
                    currentDirection = Hallway[i + 1] - pt;
                }

            }
        }



        public IEnumerable<Vector2Int> WallDirection(int hallIndex)
        {
            if (Hallway.Count != WallDirections.Count) { InitWallDirections(); }
            return WallDirections[hallIndex];
        }

        public IEnumerable<WallPosition> Walls(float scale, float elevation)
        {
            if (Hallway.Count != WallDirections.Count) { InitWallDirections(); }

            for (int i = 0, n=Hallway.Count; i<n ; i++)
            {
                var pt = Hallway[i];
                foreach (var direction in WallDirections[i])
                {
                    yield return WallPosition.From(pt, direction, scale, elevation);
                }
            }
        }
    }
    
}
