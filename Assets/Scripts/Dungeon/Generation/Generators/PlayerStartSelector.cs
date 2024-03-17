using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon
{
    public static class PlayerStartSelector
    {
        private static Vector2Int ChooseStartPosition(
    DungeonRoom room,
    DungeonGridLayer dungeonGridLayer
)
        {
            if (room.Interior.Count > 0)
            {
                return room.Interior.OrderBy(_ => Random.value).FirstOrDefault();
            }

            return room.Perimeter
                .Where(coords => dungeonGridLayer[coords] == DungeonGridLayer.ROOM_PERIMETER)
                .OrderBy(_ => Random.value)
                .FirstOrDefault();
        }

        public static Vector2Int ChooseStartPosition(
    List<DungeonRoom> rooms,
    DungeonGridLayer dungeonGridLayer,
    out DungeonRoom room
)
        {
            var candidates = rooms.OrderByDescending(r => r.HubSeparation).ToList();

            var candidate = candidates.FirstOrDefault();
            //Debug.Log($"Candidate {candidate.RoomId} has {candidate.HubSeparation} separation");

            if (candidate == null)
            {

                room = null;
                return Vector2Int.zero;

            }

            if (candidate.HubSeparation == 0)
            {
                room = candidate;
                return ChooseStartPosition(candidate, dungeonGridLayer);
            }

            if (candidate.HubSeparation < 4)
            {
                room = candidates
                    .Where(c => c.HubSeparation <= candidate.HubSeparation && c.HubSeparation > 0)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();


                return ChooseStartPosition(room, dungeonGridLayer);
            }

            room = candidates
                .Where(c => c.HubSeparation >= candidate.HubSeparation - 1)
                .OrderBy(_ => Random.value)
                .FirstOrDefault();

            if (room == null)
            {
                Debug.LogError(
                    $"Illogical fail to find start position from {candidates.Count} candidates based on {candidate} with separation {candidate.HubSeparation}"
                );
                return ChooseStartPosition(candidate, dungeonGridLayer);
            }

            return ChooseStartPosition(room, dungeonGridLayer);
        }

    }
}