using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon
{
    public class DungeonGridLayer
    {
        public const int EMPTY_SPACE = 0;
        public const int ROOM_PERIMETER = -1;
        public const int ROOM_EXIT = -2;
        public const int ROOM_FORBIDDEN_EXIT = -4;
        public const int ROOM_CORNER = -5;
        public const int ROOM_INTERIOR = -10;

        public const int GRID_VALUE_TO_CHAR_BASE = 73;

        private int[,] Grid;
        public readonly int Rows;
        public readonly int Columns;
        public readonly int GridSize;
        public readonly int LargestManhattanDistance;

        public DungeonGridLayer(DungeonLevelSetting settings)
        {
            Rows = settings.gridSizeRows;
            Columns = settings.gridSizeColumns;

            GridSize = Rows * Columns;
            LargestManhattanDistance = Rows + Columns;

            Grid = new int[Rows, Columns];
        }

        public int this[Vector2Int point]
        {
            get => Grid[point.y, point.x];
            set
            {
                Grid[point.y, point.x] = value;
            }
        }

        public bool InBounds(Vector2Int point) =>
            point.x >= 0 && point.y >= 0 && point.x < Columns && point.y < Rows;

        public bool IsEmpty(Vector2Int point) => this[point] == EMPTY_SPACE;

        public bool IsPerimeter(Vector2Int point) => this[point] == ROOM_PERIMETER;

        public bool IsAnyRoom(Vector2Int point)
        {
            var value = this[point];
            return value == ROOM_CORNER
                || value == ROOM_EXIT
                || value == ROOM_FORBIDDEN_EXIT
                || value == ROOM_INTERIOR
                || value == ROOM_PERIMETER;
        }

        public bool IsHallway(Vector2Int point) => this[point] > EMPTY_SPACE;

        public bool GetEmptyNeighbour(Vector2Int point, out Vector2Int neighbour)
        {
            for (int i = 0; i < 4; i++)
            {
                var direction = MathExtensions.CardinalDirections[i];
                var neighbourCandidate = point + direction;
                if (InBounds(neighbourCandidate) && IsEmpty(neighbourCandidate))
                {
                    neighbour = neighbourCandidate;
                    return true;
                }
            }

            neighbour = point;
            return false;
        }

        public string ToString(List<DungeonRoom> rooms)
        {
            var output = "";

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    var pt = new Vector2Int(col, row);
                    var value = this[pt];
                    
                    if (value == ROOM_INTERIOR)
                    {
                        bool foundRoom = false;
                        foreach (var room in rooms)
                        {
                            if (room.Contains(pt))
                            {
                                var roomName = $"{room.RoomId}#";
                                output += roomName[Math.Min(roomName.Length - 1, (row + col) % roomName.Length)];
                                foundRoom = true;
                                break;
                            }
                        }

                        if (!foundRoom)
                        {
                            output += Convert.ToChar(value + GRID_VALUE_TO_CHAR_BASE);
                        }
                    }
                    else if (value == EMPTY_SPACE)
                    {
                        output += ".";
                    }
                    else
                    {
                        output += Convert.ToChar(value + GRID_VALUE_TO_CHAR_BASE);
                    }
                }
                output += "\n";
            }

            return output;
        }
    }
}