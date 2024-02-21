using ProcDungeon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonDoor
{
    public readonly DungeonRoom Room;
    public readonly DungeonHallway Hallway;
    public int[] Sectors;
    public Vector2Int Coordinates => Hallway.MyHallStart(Room);

    public DungeonDoor(DungeonRoom room, DungeonHallway hallway, int[] sectors)
    {
        Room = room;
        Hallway = hallway;
        
        Sectors = sectors;
    }
}
