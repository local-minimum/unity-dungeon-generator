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
        private enum Movement { None, Forward, Backward, StrafeLeft, StrafeRight, TurnCCW, TurnCW };

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
                
        private Movement NextMovement = Movement.None;
        private Movement QueuedMovement = Movement.None;

        private List<Movement> ActiveMovements = new List<Movement>();

        Movement LastMovementInput
        {
            get
            {
                var idx = ActiveMovements.Count - 1;
                if (idx < 0) return Movement.None;

                return ActiveMovements[idx];
            }
        }

        Movement GetMovement()
        {
            var movement = NextMovement;
            NextMovement = QueuedMovement;

            if (movement == Movement.None)
            {
                return LastMovementInput;
            }

            return movement;
        }

        void RegisterMovement(InputAction.CallbackContext context, Movement movement)
        {
            if (context.performed)
            {
                if (NextMovement == Movement.None)
                {
                    NextMovement = movement;
                    Debug.Log($"Tigger {movement}");
                }
                else
                {
                    QueuedMovement = movement;
                    Debug.Log($"Queued up {movement}");
                }

                if (!ActiveMovements.Contains(movement))
                {
                    ActiveMovements.Add(movement);
                }
            }

            if (context.canceled)
            {
                if (QueuedMovement == movement && NextMovement == movement)
                {
                    Debug.Log($"Clean up queued {movement}");
                    QueuedMovement = Movement.None;
                }

                ActiveMovements.Remove(movement);

            }
        }

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
            if (!CanRecieveInput) return;
            RegisterMovement(context, Movement.Forward);
        }

        public void OnMoveBackward(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;
            RegisterMovement(context, Movement.Backward);
        }

        public void OnStrafeLeft(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;
            RegisterMovement(context, Movement.StrafeLeft);
        }

        public void OnStrafeRight(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;
            RegisterMovement(context, Movement.StrafeRight);
        }

        public void OnRotateCW(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;

            RegisterMovement(context, Movement.TurnCW);
        }

        public void OnRotateCCW(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;
            RegisterMovement(context, Movement.TurnCCW);
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

        [SerializeField, Range(0, 1)]
        float tickTime = 0.4f;

        float lastTick;

        private Vector2Int MovementToDirection(Movement movement, Vector2Int forwardDirection)
        {
            switch (movement)
            {
                case Movement.Forward:
                    return forwardDirection;
                case Movement.Backward:
                    return -forwardDirection;
                case Movement.StrafeLeft:
                    return forwardDirection.RotateCCW();
                case Movement.StrafeRight:
                    return forwardDirection.RotateCW();
                default:
                    return Vector2Int.zero;
            }

        }

        private void ExecuteMovement(Movement movement)
        {
            if (movement == Movement.None) return;

            Debug.Log($"Tick {movement}");

            if (movement == Movement.TurnCW || movement == Movement.TurnCCW)
            {
                Teleport(Coordinates, movement == Movement.TurnCW ? Direction.RotateCW() : Direction.RotateCCW());
            } else
            {
                var moveDirection = MovementToDirection(movement, Direction);
                if (!Teleport(Coordinates + moveDirection))
                {
                    var teleporter = CurrentTileTeleporter;
                    if (teleporter != null && (PortalsAllowAnyDirectionEntry || movement == Movement.Forward))
                    {
                        if (moveDirection == -teleporter.ExitDirection)
                        {
                            Teleport(
                                teleporter.PairedTeleporter.Coordinates,
                                PortalsMaintainsDirection 
                                    ? MovementToDirection(
                                        movement, 
                                        movement == Movement.StrafeLeft || movement == Movement.StrafeRight ? -teleporter.PairedTeleporter.ExitDirection : teleporter.PairedTeleporter.ExitDirection
                                      ) 
                                    : teleporter.PairedTeleporter.ExitDirection
                            );
                        }
                    }

                }
            }

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);
        }

        private void Update()
        {
            if (Time.timeSinceLevelLoad - tickTime > lastTick)
            {
                lastTick = Time.timeSinceLevelLoad;
                var movement = GetMovement();
                ExecuteMovement(movement);
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