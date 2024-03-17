using ProcDungeon.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static ProcDungeon.World.PlayerController;
using static UnityEngine.GraphicsBuffer;

namespace ProcDungeon.World 
{
    public class PlayerController : DungeonEntity
    {
        public enum Movement { None, Forward, Backward, StrafeLeft, StrafeRight, TurnCCW, TurnCW };

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
        Teleporter CurrentTileTeleporter => DungeonGrid.Teleporters.FirstOrDefault(t => t.Coordinates == Coordinates);

        #region Movement
        private Movement LatestExecutedTranslationMovement { get; set; } = Movement.None;
        private Movement LatestExecutedRotationMovement { get; set; } = Movement.None;

        private Movement NextMovement { get; set; } = Movement.None;
        private Movement QueuedMovement { get; set; } = Movement.None;

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

        bool IsTurning(Movement movement) => movement == Movement.TurnCCW || movement == Movement.TurnCW;
        bool IsTranslating(Movement movement) => movement != Movement.None && !IsTurning(movement);

        Movement ShiftMovements()
        {
            var movement = NextMovement;
            NextMovement = QueuedMovement;
            if (NextMovement == Movement.None)
            {
                NextMovement = LastMovementInput;
            } else
            {
                QueuedMovement = LastMovementInput;
            }            
            return movement;
        }

        bool BonusMovement(out Movement bonusMovement)
        {
            if (PlayerSettings.InstantMovement.Value || Rotate != null || Translate == null || !IsTurning(NextMovement))
            {
                bonusMovement = Movement.None;
                return false;
            }


            bonusMovement = ShiftMovements();
            return true;
        }

        Movement GetMovement(out Movement bonusTurnMovment)
        {
            var movement = ShiftMovements();

            if (movement == Movement.None)
            {
                bonusTurnMovment = Movement.None;
                return movement;                
            }

            
            if (!PlayerSettings.InstantMovement.Value && IsTranslating(movement) && IsTurning(NextMovement)) {
                bonusTurnMovment = NextMovement;
                NextMovement = QueuedMovement;
            } else
            {
                bonusTurnMovment = Movement.None;
            }
            return movement;
        }

        void RegisterMovement(InputAction.CallbackContext context, Movement movement)
        {
            if (context.performed)
            {
                KeyPressHud.instance.Press(movement);

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

                WriteDebugText();
            }

            if (context.canceled)
            {
                KeyPressHud.instance.Release(movement);

                if (QueuedMovement == movement && NextMovement == movement)
                {
                    Debug.Log($"Clean up queued {movement}");
                    QueuedMovement = Movement.None;

                }

                if (NextMovement == movement)
                {
                    if (IsTurning(movement))
                    {
                        if (LatestExecutedRotationMovement == movement)
                        {
                            NextMovement = Movement.None;
                        }
                    }
                    else if (LatestExecutedTranslationMovement == movement)
                    {
                        NextMovement = Movement.None;
                    }
                }


                ActiveMovements.Remove(movement);

                WriteDebugText();
            }
        }
        #endregion

        #region Teleport / Instant Movement
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

        private void ExecuteInstantMovement(Movement movement)
        {
            if (movement == Movement.None)
            {
                LatestExecutedRotationMovement = Movement.None;
                LatestExecutedTranslationMovement = Movement.None;
                return;
            }

            Debug.Log($"Tick {movement}");

            if (movement == Movement.TurnCW || movement == Movement.TurnCCW)
            {
                LatestExecutedRotationMovement = movement;
                Teleport(Coordinates, movement == Movement.TurnCW ? Direction.RotateCW() : Direction.RotateCCW());
            }
            else
            {
                var moveDirection = MovementToDirection(movement, Direction);
                if (Teleport(Coordinates + moveDirection))
                {
                    LatestExecutedTranslationMovement = movement;
                } else 
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

                            LatestExecutedTranslationMovement = movement;
                        }
                    }

                } 
            }

            DelayedAction.instance.CancelMessage(destroyTeleporterMessage);
        }

        #endregion

        #region Smooth Movement
        [SerializeField, Tooltip("Time/Progress skewing should always start at 0 and end with 1")]
        AnimationCurve easeIn;
        [SerializeField, Tooltip("Time/Progress skewing should always start at 0 and end with 1")] 
        AnimationCurve easeOut;
        [SerializeField, Range(0, 1), Tooltip("Fraction of transition that each ease does")]
        float easeDuration = 0.2f;

        System.Func<bool> Translate;
        System.Func<bool> Rotate;

        float EaseProgress(float progress, bool continueIn, bool continueOut)
        {
            if (progress < easeDuration && !continueIn)
            {
                return easeIn.Evaluate(progress / easeDuration) * easeDuration;
               
            }
            else if (progress > 1 - easeDuration && !continueOut)
            {
                var easeProgress = progress - (1 - easeDuration);
                return 1 - easeDuration + easeDuration * easeOut.Evaluate(easeProgress / easeDuration);
            }
            else
            {
                return progress;
            }
        }

        void ConstructSmoothTranslation(Movement movement)
        {
            var moveDirection = MovementToDirection(movement, Direction);
            var targetCoordinates = Coordinates + moveDirection;

            if (DungeonGrid.Accessible(targetCoordinates, EntityType))
            {
                var startPosition = transform.position;
                var targetPosition = DungeonGrid.LocalWorldPosition(targetCoordinates);
                var startTime = Time.timeSinceLevelLoad;
                var continueIn = movement == LatestExecutedTranslationMovement;
                LatestExecutedTranslationMovement = movement;

                Translate = () =>
                {
                    var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / tickTime);

                    transform.position = Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        EaseProgress(progress, continueIn, movement == NextMovement)
                    );

                    // Once almost there we trigger map updates
                    if (progress > 0.9f && !DungeonGrid.Visited(Coordinates))
                    {
                        DungeonGrid.VisitPosition(targetCoordinates, Direction);                        
                    }

                    if (progress == 1f)
                    {
                        Coordinates = targetCoordinates;
                        DungeonGrid.VisitPosition(Coordinates, Direction);
                    }

                    return progress == 1f;
                };

            }
            else
            {
                // Consider teleportation
            }

        }

        void ConstructSmoothRotation(Movement movement)
        {
            var direction = movement == Movement.TurnCW ? Direction.RotateCW() : Direction.RotateCCW();
            var startRotation = transform.rotation;
            var targetRotation = DungeonGrid.LocalWorldRotation(direction);
            var startTime = Time.timeSinceLevelLoad;
            var continueIn = movement == LatestExecutedRotationMovement;
            LatestExecutedRotationMovement = movement;
            WriteDebugText();

            Rotate = () =>
            {
                var progress = Mathf.Clamp01((Time.timeSinceLevelLoad - startTime) / tickTime);

                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, EaseProgress(progress, continueIn, movement == NextMovement));

                // Once almost there we trigger map updates
                if (progress > 0.9f && !DungeonGrid.Visited(Coordinates))
                {
                    DungeonGrid.VisitPosition(Coordinates, direction);
                }

                if (progress == 1)
                {
                    Direction = direction;
                    DungeonGrid.VisitPosition(Coordinates, Direction);
                }


                return progress == 1f;
            };

        }

        void ExecuteSmoothMovment(Movement movement, Movement overloadedTurn = Movement.None)
        {
            if (movement == Movement.None)
            {
                LatestExecutedRotationMovement = Movement.None;
                LatestExecutedTranslationMovement = Movement.None;
                WriteDebugText();
                return;
            }

            if (IsTranslating(movement))
            {
                ConstructSmoothTranslation(movement);

                if (IsTurning(overloadedTurn))
                {
                    ConstructSmoothRotation(overloadedTurn);
                }
            } else if (IsTurning(movement))
            {
                ConstructSmoothRotation(movement);
            }
        }
        #endregion

        #region Input Handlers
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
        public void OnTeleporter(InputAction.CallbackContext context)
        {
            if (!CanRecieveInput) return;

            if (context.performed && DungeonHub.instance.AddTeleporterPair(Coordinates, Direction, out var teleporter))
            {
                teleporter.name = $"Level Teleporter {Coordinates}";
                Debug.Log($"Added teleporter {teleporter}");
            }
            else if (
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
            }
            else
            {
                DelayedAction.instance.CancelMessage(destroyTeleporterMessage);

                Debug.Log("Invalid teleporter position");
            }
        }

        #endregion

        const string destroyTeleporterMessage = "Dismantle Teleporter";

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

        private void Update()
        {            
            if (Translate != null)
            {
                if (Translate())
                {
                    Translate = null;
                }
            }
            if (Rotate != null)
            {
                if (Rotate())
                {
                    Rotate = null;
                    WriteDebugText();
                }
            }

            // WriteDebugText();

            if (Time.timeSinceLevelLoad - tickTime > lastTick)
            {
                lastTick = Time.timeSinceLevelLoad;
                var movement = GetMovement(out var bonusTurnMovment);
                if (PlayerSettings.InstantMovement.Value)
                {
                    ExecuteInstantMovement(movement);
                } else
                {
                    ExecuteSmoothMovment(movement, bonusTurnMovment);
                }                
            } else if (BonusMovement(out var bonusTurnMovment))
            {
                if (PlayerSettings.InstantMovement.Value)
                {
                    ExecuteInstantMovement(bonusTurnMovment);
                } else
                {
                    ExecuteSmoothMovment(bonusTurnMovment);
                }
            }
        }

        void WriteDebugText()
        {
            var status = $"Translating: <b>{Translate != null}</b>\nRotating: <b>{Rotate != null}</b>\n";
            var latest = $"Latest M: <i>{LatestExecutedTranslationMovement}</i>\nLatest T: <i>{LatestExecutedRotationMovement}</i>\n";
            var queue = $"Next: <i>{NextMovement}</i>\nQueued: <i>{QueuedMovement}</i>\nLast Inp: <i>{LastMovementInput}</i>\n";
            DebugText.instance.Text = $"{status}{latest}{queue}";
        }
    }
}