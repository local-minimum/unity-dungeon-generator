using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.World
{
    
    public class SlidingDoorController : AbstractDoorController
    {
        private enum DoorState { Closed, Opening, Opened, Closing };

        private DoorState _doorState;

        [SerializeField]
        private Transform _doorTransform;

        [SerializeField]
        Vector3 slideDirection;

        [SerializeField]
        float slideDistance;

        [SerializeField]
        AnimationCurve slideTransition;

        [SerializeField]
        float slideDuration;

        float slideStartTime;

        float slideProgression => Mathf.Clamp01((Time.timeSinceLevelLoad - slideStartTime) / slideDuration);

        Vector3 closedPosition;

        private void Start()
        {
            closedPosition = _doorTransform.position;
        }

        override public void Open()
        {
            if (_doorState != DoorState.Closed) return;
            slideStartTime = Time.timeSinceLevelLoad;
            _doorState = DoorState.Opening;
        }

        override public void Close()
        {
            if (_doorState != DoorState.Opened) return;
            slideStartTime = Time.timeSinceLevelLoad;
            _doorState = DoorState.Closing;
            dungeonDoor.Closed = true;
        }

        public override bool Toggle()
        {
            if (_doorState == DoorState.Closed) { 
                Open();
                return true;
            } else if (_doorState == DoorState.Opened)
            {
                Close();
                return false;
            }
            return false;
        }

        private void Update()
        {
            if (_doorState == DoorState.Opening)
            {
                var progress = slideProgression;

                if (progress > 0.8f && dungeonDoor.Closed)
                {
                    dungeonDoor.Closed = false;
                }

                if ( progress == 1)
                {
                    _doorState = DoorState.Opened;                  
                }

                _doorTransform.position = closedPosition + slideDirection * slideDistance * slideTransition.Evaluate(progress);
            } else if ( _doorState == DoorState.Closing)
            {
                var progress = slideProgression;

                if (progress > 0.2f && !dungeonDoor.Closed)
                {
                    dungeonDoor.Closed = true;
                }

                if (progress == 1)
                {
                    _doorState = DoorState.Closed;
                }

                _doorTransform.position = closedPosition + slideDirection * slideDistance * (1 - slideTransition.Evaluate(progress));

            }
        }
    }
}