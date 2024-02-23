using ProcDungeon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonDoor
{
    public readonly DungeonRoom Room;
    public readonly DungeonHallway Hallway;
    public int[] Sectors;
    public bool Unlocked { get; set; }
    public bool Closed {  get; set; }

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
}
