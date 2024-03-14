using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon
{
    public class PuzzleGenerator
    {
        public readonly List<DungeonDoor> Doors = new List<DungeonDoor>();
        private DungeonRoom playerSpawnRoom;

        public readonly List<DungeonDoorKey> Keys = new List<DungeonDoorKey>();

        private List<List<DungeonRoom>> roomSectors = new List<List<DungeonRoom>>();
        
        private Dictionary<int, List<int>> sectorNeighbourGraph = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> upstreamSectorGraph = new Dictionary<int, List<int>>();

        public int LevelSectors => roomSectors.Count;
        public int LargestSector => roomSectors.Select(s => s.Count).OrderByDescending(v => v).First();

        public PuzzleGenerator(RoomGenerator roomGenerator, DungeonRoom playerSpawnRoom, Vector2Int playerSpawn)
        {
            this.playerSpawnRoom = playerSpawnRoom;

            var playerSector = new List<DungeonRoom>();
            playerSector.AddRange(roomGenerator.Rooms);
            roomSectors.Add(playerSector);
        }

        public int SectorDistance(DungeonDoor door, int sourceSectorId = 0)
        {
            if (door.FacesSector(sourceSectorId)) return 0;

            var seenSectors = new HashSet<int>() { sourceSectorId };
            var queue = new Queue<KeyValuePair<int, int>>();
            queue.Enqueue(new KeyValuePair<int, int>(sourceSectorId, 0));

            while (queue.Count > 0)
            {
                var (sectorId, distance) = queue.Dequeue();

                foreach (var neighbour in sectorNeighbourGraph[sectorId].Where(n => !seenSectors.Contains(n)))
                {
                    if (door.FacesSector(neighbour))
                    {
                        return distance + 1;
                    } else
                    {
                        seenSectors.Add(neighbour);
                        queue.Enqueue(new KeyValuePair<int, int>(neighbour, distance + 1));
                    }
                }
            }
  
            // Large number because there's no way there...
            return 999;
        }

        private IEnumerable<int> NeighbouringSectorsByDoors(int sectorId) =>
            Doors
                .Where(door => door.FacesSector(sectorId))
                .Select(door => door.OtherSector(sectorId))
                .Distinct();
                    

        public void UpdateSectorNeighbourGraph()
        {
            sectorNeighbourGraph.Clear();
            Queue<int> seenSectors = new Queue<int>();
            seenSectors.Enqueue(0);

            while (seenSectors.Count > 0)
            {
                var sector = seenSectors.Dequeue();
                var neighbours = new List<int>();
                sectorNeighbourGraph[sector] = neighbours;

                foreach (var neighbour in NeighbouringSectorsByDoors(sector))
                {
                    neighbours.Add(neighbour);
                    if (!seenSectors.Contains(neighbour) && !sectorNeighbourGraph.ContainsKey(neighbour))
                    {
                        seenSectors.Enqueue(neighbour);
                    }
                }               
            }
        }

        public void UpdateUpstreamSectorGraph()
        {
            upstreamSectorGraph.Clear();
            List<DungeonDoorKey> seenKeys = new List<DungeonDoorKey>();
            List<int> unlockedSectors = new List<int>() { 0 };

            for (int i = 0, n = LevelSectors; i< n; i++)
            {
                upstreamSectorGraph[i] = new List<int>();
            }            

            var idx = 0;
            while (idx < unlockedSectors.Count)
            {
                var sector = unlockedSectors[idx];
                seenKeys.AddRange(Keys.Where(k => k.SpawnSector == sector));

                unlockedSectors.AddRange(
                    sectorNeighbourGraph[sector].Where(neighbourSector => 
                        !unlockedSectors.Contains(neighbourSector) 
                        && seenKeys.Any(
                        // If a found key unlocks door to neighbouring sector we have access to it
                            key => key.Door.FacesSector(neighbourSector))
                        )
                );

                foreach (var neighbour in sectorNeighbourGraph[sector])
                {
                    if (upstreamSectorGraph[neighbour].Count > 0) continue;

                    var sectors = new List<int>() { sector };
                    sectors.AddRange(upstreamSectorGraph[sector]);
                    upstreamSectorGraph[neighbour] = sectors;
                }
                idx++;
            }

            // Debug log
            var toString = upstreamSectorGraph.AsEnumerable().Select(kvp => $"{kvp.Key} => [{string.Join(", ", kvp.Value.Select(v => v.ToString()))}]");
            Debug.Log(string.Join(" | ", toString));
        }

        public int AddDoors(int nDoors, bool locked = true)
        {
            int startDoors = Doors.Count;

            // Add doors
            for (int i = 0; i < nDoors; i++)
            {
                var door = AddDoor();

                if (door == null) break;

                door.Unlocked = !locked;

            }

            UpdateSectorNeighbourGraph();

            //Place keys
            if (locked)
            {
                var NewDoorsWithoutKeys = Doors
                    .Where((d, i) => i >= startDoors && !Keys.Any(k => k.Door == d))
                    .OrderBy(d => SectorDistance(d))
                    .ToList();

                foreach (var door in NewDoorsWithoutKeys)
                {
                    UpdateUpstreamSectorGraph();

                    var a = upstreamSectorGraph[door.Sectors[0]];
                    var b = upstreamSectorGraph[door.Sectors[1]];
                    var c = a.Count > b.Count ? a : b;
                    if (c.Count == 0)
                    {
                        c = new List<int> { 0 };
                        Debug.Log("Fallback key placement in sector 0");
                    }
                    Debug.Log($"Selecting key sector between {string.Join(", ", c)}");
                    var keySector = c[Random.Range(0, c.Count)];
                    var key = PlaceKeyInSector(door, keySector);
                    Keys.Add(key);

                    Debug.Log(key);
                }
            }

            return Doors.Count - startDoors;
        }

        private DungeonDoorKey PlaceKeyInSector(DungeonDoor door, int sectorId)
        {
            var sectorRooms = roomSectors[sectorId];
            var room = sectorRooms[Random.Range(0, sectorRooms.Count)];
            var spawnTile = room.RandomTile;

            return new DungeonDoorKey(door, spawnTile, sectorId);
        }

        public DungeonDoor AddDoor()
        {
            int largestSector = LargestSector;
            if (largestSector < 3) { return null; }            
            var sectorCandidates = roomSectors.Where(sector => sector.Count >= LargestSector - 1).ToList();
            var sector = sectorCandidates[Random.Range(0, sectorCandidates.Count)];
            var sectorId = roomSectors.IndexOf(sector);
            
            var candidates = CandidateDoorPositions(CloseToHubRoom, sectorId)
                .Select(instruction => new { 
                    instruction,
                    split=CheckSectorSplit(sector, sectorId == 0 ? playerSpawnRoom : sector.First(), instruction.Hallway)
                })
                .Where(c => {
                    // Require 3 rooms from player spawn
                    if (c.split[1].Count == 0 || (sectorId == 0 && c.split[0].Count < 3)) return false;

                    foreach (var split in c.split)
                    {
                        // Don't allow a door lock on room right after lock to room.
                        if (split.Count == 1 && !split[0].IsTerminus) return false;
                    }
                    
                    return true;
                    })
                .OrderBy(c => Mathf.Abs(c.split[1].Count - c.split[0].Count))
                // TODO: Perhaps also a future setting, only considering the most even splits
                .Take(3)
                .ToList();

            if (candidates.Count == 0) return null;

            var doorInstructionWithSplit = candidates[Random.Range(0, candidates.Count)];

            return CreateDoor(doorInstructionWithSplit.instruction, doorInstructionWithSplit.split, sectorId);
        }

        private DungeonDoor CreateDoor(DoorInstruction doorInstruction, List<List<DungeonRoom>> split, int sectorId)
        {
            roomSectors[sectorId] = split[0];
            var newSectorId = LevelSectors;
            roomSectors.Add(split[1]);

            // Update existing doors
            foreach (var existingDoor in Doors)
            {
                if (!existingDoor.FacesSector(sectorId)) continue;

                var room = existingDoor.Room;
                var otherRoom = existingDoor.Hallway.OtherRoom(room);

                if (roomSectors[newSectorId].Contains(room) || roomSectors[newSectorId].Contains(otherRoom))
                {
                    existingDoor.UpdateSector(sectorId, newSectorId);
                }
            }

            var door = new DungeonDoor(doorInstruction.Room, doorInstruction.Hallway, new int[] { sectorId, newSectorId });
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

        private struct DoorInstruction {
            public DungeonRoom Room;
            public DungeonHallway Hallway;

            public DoorInstruction(DungeonRoom room, DungeonHallway hallway)
            {
                Room = room;
                Hallway = hallway;
            }
        }

        private IEnumerable<DoorInstruction> CandidateDoorPositions(
            System.Func<DungeonRoom, bool> primaryRoomFilter,
            int sectorId = 0     
        )
        {
            foreach (var room in roomSectors[sectorId].Where(primaryRoomFilter))
            {
                int maxSep = MaxHubSeparation(room, new List<DungeonRoom>());

                // TODO: This rule maybe should be via settings too
                if (maxSep > 2)
                {
                    yield return new DoorInstruction(room, room.Exits[Random.Range(0, room.Exits.Count)]);
                } else
                {
                    // This little magic makes deadend also be filtered out
                    var candidates = room.Exits.Where(hall => (hall.OtherRoom(room)?.HubSeparation ?? room.HubSeparation) < room.HubSeparation).ToList();
                    yield return new DoorInstruction(room, candidates[Random.Range(0, candidates.Count)]);
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