using ProcDungeon.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon.World 
{
    public class DoorTransitionTrigger : MonoBehaviour
    {
        [SerializeField]
        AbstractDoorController doorController;

        bool mouseOver;

        private void Start()
        {
            if (doorController == null)
            {
                doorController = GetComponentInParent<AbstractDoorController>();
            }
        }

        private void Update()
        {
            if (doorController == null) return;

            if (mouseOver && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!doorController.dungeonDoor.Unlocked && Inventory.instance.HasKeyToDoor(doorController.dungeonDoor, out string itemName))
                {
                    GameLog.instance.LogPlayer("opened", "door", itemName);
                    Debug.Log($"Unlocked {doorController.dungeonDoor} using {itemName}");
                    doorController.dungeonDoor.Unlocked = true;
                }

                if (doorController.dungeonDoor.Unlocked)
                {
                    var opened = doorController.Toggle();
                    GameLog.instance.LogPlayer(opened ? "opened" : "closed", "door");
                    Debug.Log($"Door {doorController.dungeonDoor} {(opened ? "opened" : "closed")}");
                } else
                {
                    GameLog.instance.LogPlayerFail("open", "door");
                    Debug.Log($"Door {doorController.dungeonDoor} is locked and missing key");
                }
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
