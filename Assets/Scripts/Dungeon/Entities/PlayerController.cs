using ProcDungeon.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon.World 
{
    public class PlayerController : DungeonEntity
    {
        public bool PortalsMaintainsDirection;
        public bool PortalsAllowAnyDirectionEntry;

        public static PlayerController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) { 
                Instance = this;
            } else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public new EntityType EntityType => EntityType.Player;

        public DungeonGrid DungeonGrid {  get; set; }

        public List<MonoBehaviour> InputBlockers { get; private set; } = new List<MonoBehaviour>();
        public bool CanRecieveInput => InputBlockers.Count == 0;

        public Vector2Int Coordinates { get; private set; }
        public Vector2Int Direction {  get; private set; }

        public bool Teleport(Vector2Int target)
        {
            if (DungeonGrid.Accessible(target, EntityType))
            {
                transform.position = DungeonGrid.LocalWorldPosition(target);
                Coordinates = target;

                DungeonGrid.VisitPosition(Coordinates, Direction);
                return true;
            }
            return false;
        }

        public bool Teleport(Vector2Int target, Vector2Int direction, bool force = false)
        {
            if (force || DungeonGrid.Accessible(target, EntityType))
            {
                transform.position = DungeonGrid.LocalWorldPosition(target);
                transform.rotation = DungeonGrid.LocalWorldRotation(direction);

                Coordinates = target;
                Direction = direction;

                DungeonGrid.VisitPosition(Coordinates, Direction);
                return true;
            }
            return false;
        }

        public void Rotate(Vector2Int direction)
        {
            if (direction == Direction) return;
        }

        Teleporter CurrentTileTeleporter => DungeonGrid.Teleporters.FirstOrDefault(t => t.Coordinates == Coordinates);

        public void OnMoveForward(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput || !context.performed) return;

            if (!Teleport(Coordinates + Direction)) {
                var teleporter = CurrentTileTeleporter;

                if (teleporter != null)
                {                    
                    if (Direction.IsInverseDirection(teleporter.ExitDirection))
                    {
                        Teleport(teleporter.PairedTeleporter.Coordinates, teleporter.PairedTeleporter.ExitDirection, true);
                    }
                }
            }

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);
        }

        public void OnMoveBackward(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput || !context.performed) return;

            if (!Teleport(Coordinates - Direction))
            {
                var teleporter = CurrentTileTeleporter;

                if (teleporter != null && PortalsAllowAnyDirectionEntry)
                {
                    if (Direction == teleporter.ExitDirection)
                    {
                        Teleport(
                            teleporter.PairedTeleporter.Coordinates, 
                            PortalsMaintainsDirection ? -1 * teleporter.PairedTeleporter.ExitDirection : teleporter.PairedTeleporter.ExitDirection
                        );
                    }
                }
            }

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);
        }

        public void OnStrafeLeft(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput || !context.performed) return;

            if (!Teleport(Coordinates + Direction.RotateCCW())) { 
                var teleporter = CurrentTileTeleporter; 

                if (teleporter != null && PortalsAllowAnyDirectionEntry)
                {
                    if (Direction.RotateCW() == teleporter.ExitDirection)
                    {
                        Teleport(
                            teleporter.PairedTeleporter.Coordinates,
                            PortalsMaintainsDirection ? teleporter.PairedTeleporter.ExitDirection.RotateCW() : teleporter.PairedTeleporter.ExitDirection
                        );
                    }
                }
            };

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);

        }

        public void OnStrafeRight(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput || !context.performed) return;

            if (!Teleport(Coordinates + Direction.RotateCW()))
            {
                var teleporter = CurrentTileTeleporter;
                if (teleporter != null && PortalsAllowAnyDirectionEntry)
                {
                    if (Direction.RotateCCW() == teleporter.ExitDirection)
                    {
                        Teleport(
                            teleporter.PairedTeleporter.Coordinates,
                            PortalsMaintainsDirection ? teleporter.PairedTeleporter.ExitDirection.RotateCCW() : teleporter.PairedTeleporter.ExitDirection
                        );
                    }
                }
                
            }

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);

        }

        public void OnRotateCW(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput || !context.performed) return;

            Teleport(Coordinates, Direction.RotateCW());

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);
        }

        public void OnRotateCCW(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput || !context.performed) return;

            Teleport(Coordinates, Direction.RotateCCW());

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);
        }

        const string destroyTeleporterMessage = "Dismantle Teleporter";

        public void OnTeleporter(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;

            if (context.performed && DungeonHub.instance.AddTeleporterPair(Coordinates, Direction,  out var teleporter))
            {
                teleporter.name = $"Level Teleporter {Coordinates}";
                Debug.Log($"Added teleporter {teleporter}");
            } else if (
                context.performed 
                && DungeonHub.instance.FacingTeleporter(Coordinates, Direction) 
                && DungeonHub.instance.CanDestroyTeleporter(Coordinates)
             )
            {
                var coordinates = Coordinates;

                System.Action callback = () =>
                {
                    if (!DungeonHub.instance.DestroyTeleporter(coordinates))
                    {
                        Debug.Log("Can't destroy last teleporter");
                    }

                };

                DelayedAction.instance.ShowMessage(destroyTeleporterMessage, callback, 1.5f);
            } else
            {
                DelayedAction.instance.CancelMessage(destroyTeleporterMessage);

                Debug.Log("Invalid teleporter position");
            }
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
                    .Where(c => c.HubSeparation <= candidate.HubSeparation && c.HubSeparation > 0)
                    .OrderBy(_ => Random.value)
                    .FirstOrDefault();


                return ChooseStartPosition(room, dungeonGridLayer);
            }

            room = candidates
                .Where(c => c.HubSeparation >= candidate.HubSeparation - 1)
                .OrderBy(_ => Random.value)
                .FirstOrDefault();

            if ( room == null )
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