using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon
{
    public class PuzzleGenerator
    {
        private readonly RoomGenerator RoomGenerator;
        public readonly List<DungeonDoor> Doors = new List<DungeonDoor>();
        private DungeonRoom playerSpawnRoom;
        private Vector2Int playerSpawn;
        private List<List<DungeonRoom>> roomSectors = new List<List<DungeonRoom>>();

        public int LevelSectors => roomSectors.Count;
        public int LargestSector => roomSectors.Select(s => s.Count).OrderByDescending(v => v).First();

        public PuzzleGenerator(RoomGenerator roomGenerator, DungeonRoom playerSpawnRoom, Vector2Int playerSpawn)
        {
            RoomGenerator = roomGenerator;
            this.playerSpawnRoom = playerSpawnRoom;
            this.playerSpawn = playerSpawn;

            var playerSector = new List<DungeonRoom>();
            playerSector.AddRange(roomGenerator.Rooms);
            roomSectors.Add(playerSector);
        }

        public DungeonDoor AddDoor()
        {
            int largestSector = LargestSector;
            if (largestSector < 3) { return null; }            
            var sectorCandidates = roomSectors.Where(sector => sector.Count >= LargestSector - 1).ToList();
            var sector = sectorCandidates[Random.Range(0, sectorCandidates.Count)];
            var sectorId = roomSectors.IndexOf(sector);
            
            var candidates = CandidateDoorPositions(CloseToHubRoom, sectorId)
                .Select(c => new { 
                    info=c,
                    split=CheckSectorSplit(sector, sectorId == 0 ? playerSpawnRoom : sector.First(), c.Hallway)
                })
                // Require 3 rooms from player spawn
                .Where(c => {
                    if (c.split[1].Count == 0 || (sectorId == 0 && c.split[0].Count < 3)) return false;

                    foreach (var split  in c.split)
                    {
                        if (split.Count == 1 && !split[0].IsTerminus) return false;
                    }
                    
                    return true;
                    })
                .ToList();

            if (candidates.Count == 0) return null;

            var doorCandidate = candidates[Random.Range(0, candidates.Count)];

            // TODO: Update other door refs if needed
            roomSectors[sectorId] = doorCandidate.split[0];
            roomSectors.Add(doorCandidate.split[1]); 

            var door = new DungeonDoor(doorCandidate.info.Room, doorCandidate.info.Hallway, new int[] { sectorId, LevelSectors});
            Doors.Add(door);

            return door;
        }

        private List<List<DungeonRoom>> CheckSectorSplit(List<DungeonRoom> sector, DungeonRoom seenRoom, DungeonHallway splitter)
        {
            var group = new List<DungeonRoom>();
            var seen = new Queue<DungeonRoom>();
            seen.Enqueue(seenRoom);

            while (seen.Count > 0)
            {
                var room = seen.Dequeue();
                group.Add(room);

                foreach (var newlySeen in room.Exits
                    .Where(hall => hall != splitter)
                    .Select(hall => hall.OtherRoom(room))
                    .Where(other => sector.Contains(other) && !group.Contains(other) && !seen.Contains(other)))
                {
                    seen.Enqueue(newlySeen);
                }
            }

            var unseen = new List<DungeonRoom>();
            unseen.AddRange(sector.Where(room => !group.Contains(room)));

            Debug.Log($"Checking split of sector {sector.Count} found {group.Count} & {unseen.Count} sector sizes (total sectors {LevelSectors})");
            return new List<List<DungeonRoom>> { group, unseen };
        }

        private bool NonHubRoom(DungeonRoom room) => room.HubSeparation > 0;
        private bool CloseToHubRoom(DungeonRoom room) => room.HubSeparation == 1 || room.HubSeparation == 2;

        private struct CandidateDoorPosition {
            public DungeonRoom Room;
            public DungeonHallway Hallway;

            public CandidateDoorPosition(DungeonRoom room, DungeonHallway hallway)
            {
                Room = room;
                Hallway = hallway;
            }
        }

        private IEnumerable<CandidateDoorPosition> CandidateDoorPositions(
            System.Func<DungeonRoom, bool> primaryRoomFilter,
            int sectorId = 0     
        )
        {
            foreach (var room in roomSectors[sectorId].Where(primaryRoomFilter))
            {
                // Only allow neighbouring one 
                // TODO: This could also be a predicate
                if (room.Exits.Count(hall => hall.OtherRoom(room)?.HubSeparation == 0) > 1)
                {
                    continue;
                }

                int maxSep = MaxHubSeparation(room, new List<DungeonRoom>());

                // TODO: This rule maybe should be via settings too
                if (maxSep > 2)
                {
                    yield return new CandidateDoorPosition(room, room.Exits[Random.Range(0, room.Exits.Count)]);
                } else
                {
                    // This little magic makes deadend also be filtered out
                    var candidates = room.Exits.Where(hall => (hall.OtherRoom(room)?.HubSeparation ?? room.HubSeparation) < room.HubSeparation).ToList();
                    yield return new CandidateDoorPosition(room, candidates[Random.Range(0, candidates.Count)]);
                }
            }
        }

        private int MaxHubSeparation(DungeonRoom room, List<DungeonRoom> seen)
        {
            seen.Add(room);

            var separation = room.HubSeparation;
            foreach (DungeonHallway hall in room.Exits)
            {
                var other = hall.OtherRoom(room);
                if (other == null || other.HubSeparation < separation || seen.Contains(other)) continue;
                
                var otherSep = MaxHubSeparation(other, seen);
                if (otherSep > separation)
                {
                    separation = otherSep;
                }
            }
            return separation;
        }
    }
}