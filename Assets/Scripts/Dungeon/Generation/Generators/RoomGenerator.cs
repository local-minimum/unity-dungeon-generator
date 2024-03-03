using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon
{
    public class RoomGenerator
    {
        private List<DungeonRoom> _Rooms = new List<DungeonRoom>();
        public List<DungeonRoom> Rooms => _Rooms;

        private DungeonGridLayer Grid;
        private DungeonLevelSetting settings;

        public RoomGenerator(DungeonGridLayer grid, ref DungeonLevelSetting settings) { 
            Grid = grid;
            this.settings = settings;
        }

        const int CORNER_OVERLAP_THRESHOLD = -1;
        public void PlaceRooms(GridSegmenter gridSegmenter)
        {
            var nSegments = gridSegmenter.Segments.Count;
            var nRooms = Random.Range(settings.minRooms, settings.maxRooms);

            Debug.Log($"Want {nRooms}");

            List<int> discardedSegmentIdx = new List<int>();
            List<int> roomSetgmentIdx = new List<int>();
            int discardedTooLarge = 0;

            for (int candidateIdx = 0; candidateIdx < nSegments; candidateIdx++)
            {
                if (discardedSegmentIdx.Contains(candidateIdx) || roomSetgmentIdx.Contains(candidateIdx))
                {
                    continue;
                }

                var roomCoreCandidate = gridSegmenter.Segments[candidateIdx];
                var roomArea = roomCoreCandidate.Area();
                if (roomArea > settings.maxRoomArea)
                {
                    Debug.LogWarning($"Segment {settings} has too large area ({roomArea} > {settings.maxRoomArea})");
                    discardedSegmentIdx.Add(candidateIdx);
                    discardedTooLarge++;

                    continue;
                }

                var roomSegments = new List<RectInt>() { roomCoreCandidate };
                roomSetgmentIdx.Add(candidateIdx);

                var wantedSegments = Random.value < settings.multiSegmentRoomProbability ? Random.Range(2, settings.maxSegmentsPerRoom) : 1;

                for (int neighbourIdx = candidateIdx + 1; neighbourIdx < nSegments; neighbourIdx++)
                {
                    if (discardedSegmentIdx.Contains(neighbourIdx)) continue;

                    var neighbourSegment = gridSegmenter.Segments[neighbourIdx];
                    bool expand = roomSegments.Count < wantedSegments;

                    if (NeighbouringSegments(ref roomCoreCandidate, ref neighbourSegment, expand ? settings.roomPartMinOverlap : CORNER_OVERLAP_THRESHOLD))
                    {
                        var neightbourArea = neighbourSegment.Area();
                        if (expand && !roomCoreCandidate.UnitesToRect(neighbourSegment))
                        {
                            if (roomArea + neightbourArea < settings.maxRoomArea)
                            {
                                roomArea += neightbourArea;
                                roomSetgmentIdx.Add(neighbourIdx);
                                roomSegments.Add(neighbourSegment);

                                for (int nextNeighbourIdx = candidateIdx + 1; nextNeighbourIdx < nSegments; nextNeighbourIdx++)
                                {
                                    if (
                                        nextNeighbourIdx == neighbourIdx 
                                        || roomSetgmentIdx.Contains(nextNeighbourIdx)
                                        || discardedSegmentIdx.Contains(nextNeighbourIdx)
                                        ) continue;

                                    var nextNeighbour = gridSegmenter.Segments[nextNeighbourIdx];
                                    if (NeighbouringSegments(ref neighbourSegment, ref nextNeighbour, CORNER_OVERLAP_THRESHOLD))
                                    {
                                        discardedSegmentIdx.Add(nextNeighbourIdx);
                                    }
                                }
                            }
                            else
                            {
                                discardedTooLarge++;
                                discardedSegmentIdx.Add(neighbourIdx);
                            }
                        }
                        else
                        {
                            discardedSegmentIdx.Add(neighbourIdx);
                        }
                    }
                    else if (expand && NeighbouringSegments(ref roomCoreCandidate, ref neighbourSegment, CORNER_OVERLAP_THRESHOLD))
                    {
                        discardedSegmentIdx.Add(neighbourIdx);
                    }
                }

                var room = new DungeonRoom(_Rooms.Count + 1, roomSegments);
                RecordRoomOnGrid(room);
                _Rooms.Add(room);

                if (roomSegments.Count < wantedSegments)
                {
                    Debug.Log($"RoomGenerator: {room} wanted {roomSegments}");
                }
                if (_Rooms.Count >= nRooms)
                {
                    break;
                }
            }

            if (_Rooms.Count < nRooms)
            {
                Debug.LogWarning($"Made {_Rooms.Count} rooms using {roomSetgmentIdx.Count} segments (wanted {nRooms}) out of {nSegments} segments; discarded {discardedSegmentIdx.Count} ({discardedTooLarge} too large).");
            } else
            {
                Debug.Log($"Made {_Rooms.Count} rooms using {roomSetgmentIdx.Count} segments (wanted {nRooms}) out of {nSegments} segments; discarded {discardedSegmentIdx.Count} ({discardedTooLarge} too large).");
            }
        }

        bool SegmentsTouchColBorder(ref RectInt s1, ref RectInt s2) =>
            s1.min.x == s2.max.x || s1.max.x == s2.min.x;

        bool SegmentsTouchRowBorder(ref RectInt s1, ref RectInt s2) =>
        s1.min.y == s2.max.y || s1.max.y == s2.min.y;

        bool OverlappingLineSegments(int s1min, int s1max, int s2min, int s2max, int overlap) =>
            Mathf.Min(s1max, s2max) - Mathf.Max(s1min, s2min) >= overlap;
        private bool NeighbouringSegments(ref RectInt s1, ref RectInt s2, int overlapThreshold) =>
            SegmentsTouchColBorder(ref s1, ref s2)
                && OverlappingLineSegments(s1.min.y, s1.max.y, s2.min.y, s2.max.y, overlapThreshold)
            || SegmentsTouchRowBorder(ref s1, ref s2)
                && OverlappingLineSegments(s1.min.x, s1.max.x, s2.min.x, s2.max.x, overlapThreshold);

    
        void RecordRoomOnGrid(DungeonRoom room)
        {
            foreach (var interior in room.Interior)
            {
                Grid[interior] = DungeonGridLayer.ROOM_INTERIOR;
            }

            if (room.Perimeter.Count == 0) return;

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
                    Grid[perimeter] = DungeonGridLayer.ROOM_CORNER;

                    // Concave rotation
                    if (dNext.IsCCWRotationOf(dPrev))
                    {
                        Grid[prev] = DungeonGridLayer.ROOM_FORBIDDEN_EXIT;
                        Grid[next] = DungeonGridLayer.ROOM_FORBIDDEN_EXIT;
                        // Not yet bee set by concave rotation rule
                    }
                }
                else if (Grid[perimeter] == DungeonGridLayer.EMPTY_SPACE)
                {
                    // On the egde of the grid
                    if (perimeter.y == 0 || perimeter.x == 0 || perimeter.y == lastRow || perimeter.x == lastCol)
                    {
                        Grid[perimeter] = DungeonGridLayer.ROOM_FORBIDDEN_EXIT;
                    }
                    else
                    {
                        Grid[perimeter] = DungeonGridLayer.ROOM_PERIMETER;
                    }
                }

                prev = perimeter;
                perimeter = next;

            }

            if (Grid[perimeter] == DungeonGridLayer.EMPTY_SPACE)
            {
                if (perimeter.y == 0 || perimeter.x == 0 || perimeter.y == lastRow || perimeter.x == lastCol)
                {
                    Grid[perimeter] = DungeonGridLayer.ROOM_FORBIDDEN_EXIT;
                }
                else if (room.Perimeter[0] - perimeter == perimeter - prev)
                {
                    Grid[perimeter] = DungeonGridLayer.ROOM_PERIMETER;
                }
                else
                {
                    Grid[perimeter] = DungeonGridLayer.ROOM_CORNER;
                }
            }
        }

        public void CalculateHubSeparations()
        {
            var calculated = new List<DungeonRoom>();
            for (int i = 0, n = Rooms.Count; i < n; i++)
            {
                var room = Rooms[i];
                if (room.Exits.Count > 2)
                {
                    room.HubSeparation = 0;
                    calculated.Add(room);
                }
            }

            if (calculated.Count == 0)
            {
                for (int i = 0, n = Rooms.Count; i < n; i++)
                {
                    Rooms[i].HubSeparation = settings.maxRooms;
                }
                return;
            }

            int roomIdx = 0;
            while (roomIdx < calculated.Count)
            {
                var room = calculated[roomIdx];
                for (int exitId = 0, n = room.Exits.Count; exitId < n; exitId++)
                {
                    var otherRoom = room.Exits[exitId].OtherRoom(room);
                    if (otherRoom == null || calculated.Contains(otherRoom)) continue;

                    otherRoom.HubSeparation = room.HubSeparation + 1;
                    calculated.Add(otherRoom);
                }

                roomIdx++;
            }
        }

        public DungeonRoom CreateHub(int roomSize = 5)
        {
            int anchor = -10 - roomSize;

            if (roomSize < 0) throw new System.ArgumentException("Room size must be positive");
            if (roomSize % 2 == 0) throw new System.ArgumentException("Room size must not be even");

            return new DungeonRoom(-1, new List<RectInt>() {
                new RectInt(anchor, anchor, roomSize, roomSize),
                new RectInt(anchor - 1, anchor + roomSize / 2, roomSize + 2, 1),
                new RectInt(anchor + roomSize / 2, anchor - 1, 1, roomSize + 2),
            }); ;
        }
    }
}