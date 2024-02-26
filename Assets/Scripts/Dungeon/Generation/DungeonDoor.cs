using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon
{
    public class DungeonDoor
    {
        public readonly DungeonRoom Room;
        public readonly DungeonHallway Hallway;
        public int[] Sectors;
        public bool Unlocked { get; set; }
        public bool Closed { get; set; }

        public Vector2Int Coordinates => Hallway.MyHallStart(Room);
        public Vector2Int DirectionFromRoom => Hallway.MyHallStart(Room) - Hallway.MyRoomExit(Room);

        public DungeonDoor(DungeonRoom room, DungeonHallway hallway, int[] sectors)
        {
            Room = room;
            Hallway = hallway;

            Sectors = sectors;
            Closed = true;
            Unlocked = true;
        }

        public bool FacesSector(int sector) => Sectors.Contains(sector);
        public bool FacesSector(Func<int, bool> predicate) => Sectors.Any(predicate);

        public void UpdateSector(int oldSector, int newSector)
        {
            Sectors[Sectors[0] == oldSector ? 0 : 1] = newSector;
        }

        public int OtherSector(int sector) => Sectors[Sectors[0] == sector ? 1 : 0];

        public override string ToString() => 
            $"<Door at {Coordinates} ({Sectors[0]}<->{Sectors[1]})>";
        
    }
}