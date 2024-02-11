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
    }
    
}
