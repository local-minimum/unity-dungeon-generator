using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


namespace ProcDungeon
{
    public class HallwayGenerator
    {
        private DungeonGridLayer Grid;
        private DungeonLevelSetting settings;
        private List<DungeonRoom> Rooms;
        private List<DungeonHallway> _Hallways = new List<DungeonHallway>();
        public List<DungeonHallway> Hallways => _Hallways;

        public HallwayGenerator(DungeonGridLayer grid, List<DungeonRoom> rooms, ref DungeonLevelSetting settings)
        {
            this.settings = settings;
            Rooms = rooms;
            Grid = grid;
        }

        public void MakeHallways()
        {
            _Hallways.Clear();

            ConnectRooms();

            HealGrid();

            LogGrid();            
        }

        void LogGrid()
        {
            Debug.Log($"HalwayGenerator Grid:\n{Grid.ToString(Rooms)}");
        }

        bool FindClosestRoom(DungeonRoom room, bool excludeConnected, out DungeonRoom closest)
        {
            int closestDistance = Grid.LargestManhattanDistance + 1;
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
                    FinalizeHallway(hallway);
                } else
                {
                    Debug.LogWarning($"Failed to connect {room} with {closestRoom}");
                    if (hallway != null) ClearHallway(hallway);
                }
            }
        }

        private void FinalizeHallway(DungeonHallway hallway)
        {
            Debug.Log($"Connected rooms {hallway.SourceRoom.RoomId} with {hallway.DestinationRoom.RoomId}");

            _Hallways.Add(hallway);
            if (hallway.SourceRoom != null)
            {
                AddRoomExitAndBlockNeighbours(hallway.SourceExit, hallway.Source - hallway.SourceExit);
                hallway.SourceRoom.Exits.Add(hallway);
            }
            if (hallway.DestinationRoom != null)
            {
                AddRoomExitAndBlockNeighbours(hallway.DestinationExit, hallway.Destination - hallway.DestinationExit);
                hallway.DestinationRoom.Exits.Add(hallway);
            }            
        }

        private void GroupRooms(List<DungeonRoom> grouped, Queue<DungeonRoom> newGrouped)
        {
            while (newGrouped.Count > 0)
            {
                var room = newGrouped.Dequeue();
                
                if (room == null || grouped.Contains(room)) continue;

                grouped.Add(room);

                foreach (var hallway in Hallways)
                {
                    if (hallway.SourceRoom == room)
                    {
                        if (!grouped.Contains(hallway.DestinationRoom) && !newGrouped.Contains(hallway.DestinationRoom))
                        {
                            newGrouped.Enqueue(hallway.DestinationRoom);
                        }
                    } else if (hallway.DestinationRoom == room)
                    {
                        if (!grouped.Contains(hallway.SourceRoom) && !newGrouped.Contains(hallway.SourceRoom))
                        {
                            newGrouped.Enqueue(hallway.SourceRoom);
                        }

                    }
                }
            }
        }

        private void HealGrid()
        {
            int nRooms = Rooms.Count;
            if (nRooms == 0)
            {
                Debug.LogError("There were no rooms in level so can't heal them");
                return;
            }

            List<DungeonRoom> grouped = new List<DungeonRoom>();
            Queue<DungeonRoom> newGrouped = new Queue<DungeonRoom>();
            List<DungeonRoom> ungrouped = new List<DungeonRoom>();
            List<int> impossibleConnection = new List<int>();

            System.Func<int, int, int> ConnectionId = (int a, int b) => (nRooms + 1) * a + b;

            newGrouped.Enqueue(Rooms[0]);
            GroupRooms(grouped, newGrouped);


            ungrouped.AddRange(Rooms.Where(room => !grouped.Contains(room)));

            Debug.Log($"Grouped {grouped.Count}, newGrouped {newGrouped.Count}, ungrouped {ungrouped.Count}");

            while (ungrouped.Count > 0)
            {
                int distance = Grid.LargestManhattanDistance + 1;
                DungeonRoom candidate = null;
                DungeonRoom connector = null;

                for (int i = 0, nUngrouped=ungrouped.Count, nGrouped=grouped.Count; i < nUngrouped; i++)
                {
                    DungeonRoom room = ungrouped[i];
                    for (int j = 0; j<nGrouped; j++)
                    {
                        DungeonRoom groupedRoom = grouped[j];
                        var dist = groupedRoom.CenterDistance(room);
                        if (dist < distance)
                        {
                            var conId = ConnectionId(room.RoomId, groupedRoom.RoomId);
                            if (!impossibleConnection.Contains(conId))
                            {
                                candidate = room;
                                connector = groupedRoom;
                                distance = dist;
                            }
                        }
                    }
                }

                if (candidate == null || connector == null)
                {
                    Debug.LogError($"Though there are {ungrouped.Count} rooms left ungrouped, non of them can be connected");
                    return;
                }

                var hallway = Connect(candidate, connector);
                if (hallway != null && hallway.Valid)
                {
                    FinalizeHallway(hallway);

                    ungrouped.Remove(candidate);
                    newGrouped.Enqueue(candidate);
                    GroupRooms(grouped, newGrouped);

                    ungrouped.Clear();
                    ungrouped.AddRange(Rooms.Where(r => !newGrouped.Contains(r) && !grouped.Contains(r)));
                }
                else
                {
                    Debug.LogError($"Failed to heal grid by connecting {candidate} to {connector}");
                    if (hallway!= null)
                    {
                        ClearHallway(hallway);
                    }
                    impossibleConnection.Add(ConnectionId(candidate.RoomId, connector.RoomId));

                }
            }
        }

        private void ClearHallway(DungeonHallway hallway)
        {
            foreach (var hallPt in hallway.Hallway)
            {
                Grid[hallPt] = DungeonGridLayer.EMPTY_SPACE;
            }
        }

        private void AddRoomExitAndBlockNeighbours(Vector2Int point, Vector2Int exitDirection)
        {
            Grid[point] = DungeonGridLayer.ROOM_EXIT;
            foreach (var direction in new[] { exitDirection.RotateCCW(), exitDirection.RotateCW() })
            {
                var neigbour = point + direction;
                if (Grid.InBounds(neigbour) && Grid[neigbour] == DungeonGridLayer.ROOM_PERIMETER)
                {
                    Grid[neigbour] = DungeonGridLayer.ROOM_FORBIDDEN_EXIT;
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

                        if (destinationExitDirection.IsInverseDirection(sourceExitDirection))
                        {
                            var diff = exitCandidate - sourceExit;
                            var exitDirectionDistance = sourceExitDirection.x * (sourceExit.x - exitCandidate.x)
                                    + sourceExitDirection.y * (sourceExit.y - exitCandidate.y);

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

                        var elbow = exitCandidate.OrthoIntersection(sourceExit, sourceExitDirection);
                        
                        return Grid.InBounds(elbow) && Grid.IsEmpty(elbow) && sourceExit.ManhattanDistance(elbow) > 1 && exitCandidate.ManhattanDistance(elbow) > 1;
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

            Debug.Log($"Attempting to connect room {source.RoomId} {sourceExit} to {destination.RoomId} {destinationExit}");

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
                .Select(candidate =>
                {
                    Vector2Int candidateNeighbour;
                    var hasEmptyNeighbour = Grid.GetEmptyNeighbour(candidate, out candidateNeighbour);

                    return new { candidate, candidateNeighbour, hasEmptyNeighbour };
                })
                .Where(c => {
                    if (!c.hasEmptyNeighbour || !Grid.IsPerimeter(c.candidate)) return false;
                    
                    if (c.candidateNeighbour.ManhattanDistance(target) >= c.candidate.ManhattanDistance(target)) return false;

                    var exitDirection = c.candidateNeighbour - c.candidate;
                    var diff = target - c.candidateNeighbour;
                    var prod = exitDirection * diff;
                    if (prod.x == 0 && prod.y == 0 || prod.x < 0 || prod.y < 0) return false;
                    
                    return true;
                })
                .OrderBy(c => {
                    return c.candidateNeighbour.ManhattanDistance(target);
                 })
                .Select(c => c.candidate)
                .ToList();

            if (candidates.Count == 0)
            {
                return candidates;
            }

            var closestDistance = candidates.First().ManhattanDistance(target);
            Debug.Log($"Closest distance is {closestDistance} with {candidates.Count} candiates {string.Join("; ", candidates.Select(c => $"{c}: {c.ManhattanDistance(target)}"))}");

            return candidates
                .TakeWhile(c => c.ManhattanDistance(target) < Mathf.Min(closestDistance + tolerance, Grid.LargestManhattanDistance))
                .ToList();
        }

        private bool RecordHallwayPosition(DungeonHallway hallway, Vector2Int point, Vector2Int direction)
        {
            return RecordHallwayPosition(hallway, point, direction, false);
        }

        private bool RecordHallwayPosition(DungeonHallway hallway, Vector2Int point, Vector2Int direction, bool requireForwardFree)
        {
            if (!Grid.InBounds(point))
            {
                Debug.LogError($"Tried to dig hallway at {point} which is out of bounds");
                return false;
            }

            var leftPt = point + direction.RotateCCW();
            var rightPt = point + direction.RotateCW();
            var fwdPt = point + direction;

            if (!Grid.IsEmpty(point))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already {Grid[point]} there."
                );
                return false;
            } else if (Grid.InBounds(leftPt) && !Grid.IsEmpty(leftPt))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already something to the left of it {Grid[leftPt]}."
                );
                return false;
            } else if (Grid.InBounds(rightPt) && !Grid.IsEmpty(rightPt))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already something to the right of it {Grid[rightPt]}."
                );
                return false;
            } else if (requireForwardFree && Grid.InBounds(fwdPt) && !Grid.IsEmpty(fwdPt))
            {
                Debug.LogWarning(
                    $"Tried to dig out hallway {point} but there was already something to the ahead of it {Grid[fwdPt]}."
                );
                return false;
            }

            Grid[point] = hallway.Id;
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
                if (i > Grid.LargestManhattanDistance)
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
                        hallPoint += elbowDirection;

                        if (!RecordHallwayPosition(hallway, hallPoint, elbowDirection, hallPoint == elbowTarget))
                        {
                            Debug.LogWarning($"Failed to dig elbow stretch at {hallPoint}");
                            return false;
                        }

                        hallwayLength++;

                        if (hallwayLength > Grid.LargestManhattanDistance)
                        {
                            Debug.LogError($"Failed to dig hallway from {hallway.Source} to {hallway.Destination}; gave up after {hallwayLength}");
                            return false;
                        }
                    }
                }

                hallwayLength++;
                if (hallwayLength > Grid.LargestManhattanDistance)
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
