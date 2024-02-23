using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon {
    public class DoorTransitionTrigger : MonoBehaviour
    {
        [SerializeField]
        AbstractDoorController doorController;

        bool mouseOver;

        private void Update()
        {
            if (mouseOver && Mouse.current.leftButton.isPressed)
            {
                doorController.Toggle();
            }
        }

        private void OnMouseEnter()
        {
            mouseOver = true;
        }

        private void OnMouseExit() {  
            mouseOver = false; 
        }
    }
}
