using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon {
    public class PlayerController : MonoBehaviour
    {
        public DungeonGrid DungeonGrid {  get; set; }

        bool canRecieveInput = true;        

        public Vector2Int Coordinates { get; private set; }
        public Vector2Int Direction {  get; private set; }

        public bool Teleport(Vector2Int target)
        {
            if (DungeonGrid.Dungeon.InBounds(target) && DungeonGrid.Dungeon.Accessible(target))
            {
                transform.position = DungeonGrid.LocalWorldPosition(target);
                Coordinates = target;
                return true;
            }
            return false;
        }

        public bool Teleport(Vector2Int target, Vector2Int direction)
        {
            if (DungeonGrid.Dungeon.InBounds(target) && DungeonGrid.Dungeon.Accessible(target))
            {
                transform.position = DungeonGrid.LocalWorldPosition(target);
                transform.rotation = DungeonGrid.LocalWorldRotation(direction);

                Coordinates = target;
                Direction = direction;
                return true;
            }
            return false;
        }

        public void Rotate(Vector2Int direction)
        {
            if (direction == Direction) return;
        }

        public void OnMoveForward(InputAction.CallbackContext context)
        {
            if (!canRecieveInput || !context.performed) return;

            Teleport(Coordinates + Direction);
        }

        public void OnMoveBackward(InputAction.CallbackContext context)
        {
            if (!canRecieveInput || !context.performed) return;

            Teleport(Coordinates - Direction);
        }

        public void OnStrafeLeft(InputAction.CallbackContext context)
        {
            if (!canRecieveInput || !context.performed) return;

            Teleport(Coordinates + Direction.RotateCCW());
        }

        public void OnStrafeRight(InputAction.CallbackContext context)
        {
            if (!canRecieveInput || !context.performed) return;

            Teleport(Coordinates + Direction.RotateCW());
        }

        public void OnRotateCW(InputAction.CallbackContext context)
        {
            if (!canRecieveInput || !context.performed) return;

            Teleport(Coordinates, Direction.RotateCW());
        }

        public void OnRotateCCW(InputAction.CallbackContext context)
        {
            if (!canRecieveInput || !context.performed) return;

            Teleport(Coordinates, Direction.RotateCCW());
        }

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
                    .Where(c => c.HubSeparation <= candidate.HubSeparation && c.HubSeparation > 1)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();
                return ChooseStartPosition(room, dungeonGridLayer);
            }

            room = candidates
                .Where(c => c.HubSeparation >= candidate.HubSeparation - 1)
                .OrderBy(_ => Random.value)
                .FirstOrDefault();

            return ChooseStartPosition (room, dungeonGridLayer);    
        }
    }
}