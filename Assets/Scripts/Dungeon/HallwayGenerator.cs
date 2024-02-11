using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;


namespace ProcDungeon
{
    public class HallwayGenerator
    {
        const int EMPTY_SPACE = 0;
        const int ROOM_PERIMETER = -1;
        const int ROOM_EXIT = -2;
        const int ROOM_FORBIDDEN_EXIT = -4;
        const int ROOM_CORNER = -5;
        const int ROOM_INTERIOR = -10;

        const int GRID_VALUE_TO_CHAR_BASE = 73;

        private int[,] Grid;
        private int LevelSize;
        private DungeonLevelSetting settings;
        private List<DungeonRoom> Rooms;
        private List<DungeonHallway> _Hallways = new List<DungeonHallway>();
        public List<DungeonHallway> Hallways => _Hallways;

        public void MakeHallways(List<DungeonRoom> rooms, ref DungeonLevelSetting settings)
        {
            this.settings = settings;
            Rooms = rooms;
            _Hallways.Clear();

            InitGrid();

            RecordRoomsOnGrid();

            ConnectRooms();

            LogGrid();            
        }

        void LogGrid()
        {
            var output = "HalwayGenerator Grid:\n";

            for (int row = 0, nRows = Grid.GetLength(0); row < nRows; row++)
            {
                for (int col = 0, nCols = Grid.GetLength(1); col < nCols; col++)
                {
                    var value = Grid[row, col];
                    var pt = new Vector2Int(col, row);
                    if (value == ROOM_INTERIOR)
                    {
                        bool foundRoom = false;
                        foreach (var room in Rooms)
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
                    } else if (value == EMPTY_SPACE)
                    {
                        output += ".";
                    } else
                    {
                        output += Convert.ToChar(value + GRID_VALUE_TO_CHAR_BASE);
                    }
                }
                output += "\n";
            }

            Debug.Log(output);
        }

        void InitGrid()
        {
            Grid = new int[settings.gridSizeRows, settings.gridSizeColumns];
            LevelSize = settings.gridSizeRows * settings.gridSizeColumns;
        }

        void RecordRoomsOnGrid()
        {
            for (int idxRoom = 0, nRooms = Rooms.Count; idxRoom < nRooms; idxRoom++)
            {
                var room = Rooms[idxRoom];

                foreach (var interior in room.Interior)
                {
                    Grid[interior.y, interior.x] = ROOM_INTERIOR;
                }

                if (room.Perimeter.Count == 0) continue;

                var prev = room.Perimeter.Last();
                var perimeter = room.Perimeter[0];
                int lastRow = settings.gridSizeRows - 1;
                int lastCol = settings.gridSizeColumns - 1;

                for (int idx = 1, n = room.Perimeter.Count; idx < n; idx++)
                {
                    var next = room.Perimeter[idx];
                    var dPrev = perimeter - prev;
                    var dNext = next - perimeter;

                    // We're at a corner
                    if (dNext.IsOrthogonalCardinal(dPrev))
                    {
                        Grid[perimeter.y, perimeter.x] = ROOM_CORNER;
                        // Concave rotation
                        if (dNext.IsCCWRotationOf(dPrev))
                        {
                            Grid[prev.y, prev.x] = ROOM_FORBIDDEN_EXIT;
                            Grid[next.y, next.x] = ROOM_FORBIDDEN_EXIT;
                            // Not yet bee set by concave rotation rule
                        }
                    }
                    else if (Grid[perimeter.y, perimeter.x] == EMPTY_SPACE)
                    {
                        // On the egde of the grid
                        if (perimeter.y == 0 || perimeter.x == 0 || perimeter.y == lastRow || perimeter.x == lastCol)
                        {
                            Grid[perimeter.y, perimeter.x] = ROOM_FORBIDDEN_EXIT;
                        }
                        else
                        {
                            Grid[perimeter.y, perimeter.x] = ROOM_PERIMETER;
                        }
                    }

                    prev = perimeter;
                    perimeter = next;
                    
                }

                if (Grid[perimeter.y, perimeter.x] == EMPTY_SPACE) {
                    if (perimeter.y == 0 || perimeter.x == 0 || perimeter.y == lastRow || perimeter.x == lastCol)
                    {
                        Grid[perimeter.y, perimeter.x] = ROOM_FORBIDDEN_EXIT;
                    }
                    else if (room.Perimeter[0] - perimeter == perimeter - prev)
                    {
                        Grid[perimeter.y, perimeter.x] = ROOM_PERIMETER;
                    }
                    else
                    {
                        Grid[perimeter.y, perimeter.x] = ROOM_CORNER;
                    }

                }
            }
        }
   
        bool FindClosestRoom(DungeonRoom room, bool excludeConnected, out DungeonRoom closest)
        {
            int closestDistance = LevelSize;
            closest = null;

            foreach (var other in Rooms)
            {
                if (other == room) continue;

                bool alreadyConnected = false;
                foreach (var hallway in _Hallways)
                {
                    if (hallway.Connects(room, other))
                    {
                        alreadyConnected = true;
                        break;
                    }
                }

                if (alreadyConnected) continue;

                int distance = room.CenterDistance(other);
                if (distance < closestDistance)
                {
                    closest = other;
                    closestDistance = distance;
                }
            }
          
            return closest != null;
        }

        private void ConnectRooms()
        {
            foreach (var room in Rooms)
            {
                DungeonRoom closestRoom;
                if (!FindClosestRoom(room, true, out closestRoom))
                {
                    Debug.LogWarning($"Hallways: No room closest to {room}");
                    continue;
                }

                if (closestRoom.Size == 0)
                {
                    Debug.LogError($"Hallways: Found room without size {closestRoom}");
                    continue;
                }

                if (room == closestRoom)
                {
                    Debug.LogError($"Hallways: Found myself {room} == {closestRoom}");
                    continue;
                }

                var hallway = Connect(room, closestRoom);
                if (hallway != null && hallway.Valid)
                {
                    Debug.Log($"Connected rooms {hallway.SourceRoom.RoomId} with {hallway.DestinationRoom.RoomId}");
                    _Hallways.Add(hallway);
                    AddRoomExitAndBlockNeighbours(hallway.SourceExit, hallway.Source - hallway.SourceExit);
                    AddRoomExitAndBlockNeighbours(hallway.DestinationExit, hallway.Destination - hallway.DestinationExit);
                } else
                {
                    Debug.LogWarning($"Failed to connect {room} with {closestRoom}");
                    if (hallway != null) ClearHallway(hallway);
                }
            }
        }

        private void ClearHallway(DungeonHallway hallway)
        {
            foreach (var hallPt in hallway.Hallway)
            {
                Grid[hallPt.y, hallPt.x] = EMPTY_SPACE;
            }
        }

        private void AddRoomExitAndBlockNeighbours(Vector2Int point, Vector2Int exitDirection)
        {
            Grid[point.y, point.x] = ROOM_EXIT;
            foreach (var direction in new[] { exitDirection.RotateCCW(), exitDirection.RotateCW() })
            {
                var neigbour = point + direction;
                if (InBounds(neigbour) && Is(neigbour, ROOM_PERIMETER))
                {
                    Grid[neigbour.y, neigbour.x] = ROOM_FORBIDDEN_EXIT;
                }
            }
        }
        private DungeonHallway Connect(DungeonRoom source, DungeonRoom destination)
        {
            int hallwayId = _Hallways.Count + 1;
            Vector2Int destinationCorner;
            Vector2Int sourceCorner = source.ClosestBoundingCorner(destination, out destinationCorner);

            Debug.Log($"Connecting room {source.RoomId} to {destination.RoomId} using corners {sourceCorner} and {destinationCorner}");
            List<Vector2Int> sourceExitCandidates = ExitCandidates(source, destinationCorner, settings.exitCandidateTolerance);

            if (sourceExitCandidates.Count == 0)
            {
                Debug.LogError($"Room {source} had no possible exits");
                return null;
            }
            var sourceExit = sourceExitCandidates[Random.Range(0, sourceExitCandidates.Count)];
            var sourceExitDirection = source.ExitDirection(sourceExit);

            List<Vector2Int> destinationExitCandidates = ExitCandidates(destination, sourceExit, settings.exitCandidateTolerance)
                    .Where(exitCandidate =>
                    {
                        var destinationExitDirection = destination.ExitDirection(exitCandidate);

                        var exitDirectionDistance = sourceExitDirection.x * (sourceExit.x - exitCandidate.x)
                                + sourceExitDirection.y * (sourceExit.y - exitCandidate.y);

                        
                        if (destinationExitDirection.IsInverseDirection(sourceExitDirection))
                        {
                            var diff = exitCandidate - sourceExit;
                            var absExitDirectionDistance = Mathf.Abs(exitDirectionDistance);

                            if (diff.x * diff.y == 0)
                            {
                                // Straight line
                                return absExitDirectionDistance > 2;
                            } else
                            {
                                // S-shape
                                return absExitDirectionDistance > 4;
                            }                      
                        }

                        // Elbow candidate must clear room by going out one step at least                        
                        return exitDirectionDistance > 1;
                    })
                    .ToList();
           
           if (destinationExitCandidates.Count == 0)
            {
                Debug.LogError($"Room {destination} had no possible exits that matched {sourceExit} of {source}");
                return null;
            }

            var destinationExit = destinationExitCandidates[Random.Range(0, destinationExitCandidates.Count)];
            var destinationExitDirection = destination.ExitDirection(destinationExit);

            var hallSource = sourceExit + sourceExitDirection;
            var hallDestination = destinationExit + destinationExitDirection;
            var hallway = new DungeonHallway(source, hallSource, sourceExit, destination, hallDestination, destinationExit, hallwayId);

            if (sourceExitDirection.IsInverseDirection(destinationExitDirection))
            {
                var hallDiff = hallDestination - hallSource;
                if (hallDiff.x == 0 || hallDiff.y == 0)
                {
                    hallway.Valid = DigStraightLine(hallway, hallSource, hallDestination);

                } else
                {
                    hallway.Valid = DigSShape(hallway, sourceExitDirection);
                }
            } else
            {
                hallway.Valid = DigElbow(hallway, sourceExitDirection);
            }

            return hallway;
        }

        private List<Vector2Int> ExitCandidates(DungeonRoom room, Vector2Int target, int tolerance)
        {
            // Debug.Log($"Finding potential exit from {room.RoomId} to reach {target}");

            var candidates = room.Perimeter
                .Where(candidate => {
                    if (!IsPerimeter(candidate)) return false;
                    
                    var diff = target - candidate;

                    // if (diff.x != 0 && diff.y != 0 && diff.SmallestDimension() < 2) return false;

                    // TODO: Missing some rules here I think...

                    foreach (var component in (diff).AsUnitComponents())
                    {
                        // Debug.Log($"Direction {component} for {candidate}->{target}: Empty out {IsEmpty(candidate + component)} and room in {IsAnyRoom(candidate + component * -1)}");
                        if (IsEmpty(candidate + component) && IsAnyRoom(candidate + component * -1)) return true;                        
                    }
                    
                    return false;
                })
                .OrderBy(candidate => {
                    Vector2Int candidateNeighbour;
                    if (!GetEmptyNeighbour(candidate, out candidateNeighbour)) return LevelSize;

                    return candidateNeighbour.ManhattanDistance(target);
                 })
                .ToList();

            if (candidates.Count == 0)
            {
                return candidates;
            }

            var closestDistance = candidates.First().ManhattanDistance(target);
            Debug.Log($"Closest distance is {closestDistance} with {candidates.Count} candiates {string.Join(", ", candidates.Select(c => c.ManhattanDistance(target)))}");

            return candidates
                .TakeWhile(c => c.ManhattanDistance(target) < Mathf.Min(closestDistance + tolerance, LevelSize))
                .ToList();
        }

        private bool IsPerimeter(Vector2Int point) => Is(point, ROOM_PERIMETER);
        private bool IsEmpty(Vector2Int point) => Is(point, EMPTY_SPACE);

        private bool IsAnyRoom(Vector2Int point)
        {
            var value = Grid[point.y, point.x];
            return value == ROOM_CORNER
                || value == ROOM_EXIT
                || value == ROOM_FORBIDDEN_EXIT
                || value == ROOM_INTERIOR
                || value == ROOM_PERIMETER;
        } 
        private bool Is(Vector2Int point, int value) => Grid[point.y, point.x] == value;

        private bool GetEmptyNeighbour(Vector2Int point, out Vector2Int neighbour)
        {
            for (int i = 0; i < 4; i++)
            {
                var direction = MathExtensions.CardinalDirections[i];

                if (IsEmpty(point + direction))
                {
                    neighbour = point + direction;
                    return true;
                }
            }

            neighbour = point;
            return false;
        }
        bool InBounds(Vector2Int point) =>
            point.x >= 0 && point.y >= 0 && point.x < settings.gridSizeColumns && point.y < settings.gridSizeRows;

        private bool RecordHallwayPosition(DungeonHallway hallway, Vector2Int point, Vector2Int direction)
        {
            return RecordHallwayPosition(hallway, point, direction, false);
        }

        private bool RecordHallwayPosition(DungeonHallway hallway, Vector2Int point, Vector2Int direction, bool requireForwardFree)
        {
            if (!InBounds(point))
            {
                Debug.LogError($"Tried to dig hallway at {point} which is out of bounds");
                return false;
            }

            var leftPt = point + direction.RotateCCW();
            var rightPt = point + direction.RotateCW();
            var fwdPt = point + direction;

            if (!IsEmpty(point))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already {Grid[point.y, point.x]} there."
                );
                return false;
            } else if (InBounds(leftPt) && !IsEmpty(leftPt))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already something to the left of it {Grid[leftPt.y, leftPt.x]}."
                );
                return false;
            } else if (InBounds(rightPt) && !IsEmpty(rightPt))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already something to the right of it {Grid[rightPt.y, rightPt.x]}."
                );
                return false;
            } else if (requireForwardFree && InBounds(fwdPt) && !IsEmpty(fwdPt))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already something to the ahead of it {Grid[fwdPt.y, fwdPt.x]}."
                );
                return false;
            }

            Grid[point.y, point.x] = hallway.Id;
            hallway.Hallway.Add(point);
            return true;
        }


        private bool DigStraightLine(DungeonHallway hallway, Vector2Int source, Vector2Int destination)
        {
            var direction = source.MainDirection(destination);
            if (hallway.Source == source)
            {
                if (!RecordHallwayPosition(hallway, source, direction))
                {
                    return false;
                }               
            }

            var hallPoint = source + direction;
            int i = 0;
            while (hallPoint != destination)
            {
                if (!RecordHallwayPosition(hallway, hallPoint, direction))
                {
                    return false;
                }

                i++;
                if (i > LevelSize)
                {
                    return false;
                }

                hallPoint += direction;
            }

            if (hallway.Destination == hallPoint)
            {
                if (!RecordHallwayPosition(hallway, hallPoint, direction))
                {
                    return false;
                }
            }

            return true;
        }
    
        private bool DigSShape(DungeonHallway hallway, Vector2Int startDirection)
        {
            if (!RecordHallwayPosition(hallway, hallway.Source, startDirection)) {
                Debug.LogWarning("Could not dig S-shape source");
                return false; 
            }

            var walkDiff = hallway.Destination - hallway.Source;
            var mainAxisAbsDistance = walkDiff * startDirection;
            var lastTurnStep = Mathf.Max(mainAxisAbsDistance.x, mainAxisAbsDistance.y) - 1;
            var turnAfterSteps = Random.Range(1, lastTurnStep);

            Debug.Log($"Digging out S-shape hallway from {hallway.Source} to {hallway.Destination} with turn after {turnAfterSteps} steps");

            var hallPoint = hallway.Source + startDirection;
            int hallwayLength = 1;

            while (hallPoint != hallway.Destination)
            {
                if (!RecordHallwayPosition(hallway, hallPoint, startDirection, turnAfterSteps == 0)) { 
                    Debug.LogWarning($"Failed to dig S-shape at {hallPoint} after {hallwayLength} digs");
                    return false;
                }

                turnAfterSteps--;

                if (turnAfterSteps == 0)
                {
                    // Dig elbow

                    var elbowTarget = hallPoint.OrthoIntersection(hallway.Destination, startDirection);
                    var elbowDirection = hallPoint.MainDirection(elbowTarget);

                    Debug.Log($"Turning at {hallPoint} to {elbowTarget} with direction {elbowDirection}");

                    while (hallPoint != elbowTarget)
                    {
                        hallPoint += elbowTarget;

                        if (!RecordHallwayPosition(hallway, hallPoint, elbowDirection, hallPoint == elbowTarget))
                        {
                            Debug.LogWarning($"Failed to dig elbow stretch at {hallPoint}");
                            return false;
                        }

                        hallwayLength++;

                        if (hallwayLength > LevelSize)
                        {
                            Debug.LogError($"Failed to dig hallway from {hallway.Source} to {hallway.Destination}; gave up after {hallwayLength}");
                            return false;
                        }
                    }
                }

                hallwayLength++;
                if (hallwayLength > LevelSize)
                {
                    Debug.LogError($"Failed to dig hallway from {hallway.Source} to {hallway.Destination}; gave up after {hallwayLength}");
                    return false;
                }

                hallPoint += startDirection;
            }
            
            if (!RecordHallwayPosition(hallway, hallway.Destination, startDirection)) {
                Debug.Log($"Failed to dig hallway destination at {hallway.Destination}");
                return false;
            }

            return true;
       
        }

        private bool DigElbow(DungeonHallway hallway, Vector2Int startDirection)
        {
            // Note that source and target are dug by straight lines

            var corner = startDirection.x == 0 ? new Vector2Int(hallway.Source.x, hallway.Destination.y) : new Vector2Int(hallway.Destination.x, hallway.Source.y);

            Debug.Log($"Digging out elbow from {hallway.Source} via {corner} to {hallway.Destination}");

            if (!DigStraightLine(hallway, hallway.Source, corner))
            {
                Debug.LogWarning($"Failed to dig from {hallway.Source} to corner {corner}");
                return false;
            }

            if (!RecordHallwayPosition(hallway, corner, startDirection, true))
            {
                Debug.LogWarning($"Failed to dig out corner {corner}");
                return false;
            }

            if (!DigStraightLine(hallway, corner, hallway.Destination))
            {
                Debug.LogWarning($"Failed to dig from {corner} to {hallway.Destination}");
                return false;
            }
            return true;
        }
    }
}
