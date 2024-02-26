using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon.World
{
    public abstract class AbstractItem : MonoBehaviour 
    {        
        public DungeonItem Item { get; set; }


        virtual protected bool CanPickup() => true;
        virtual protected void OnHover() { }
        virtual protected void OnStopHover() { }

        /** Current implementation just disables first renderer it finds, overwrite with more detailed behaviour */
        virtual protected void HandlePickup()
        {
            GetComponentInChildren<Renderer>().enabled = false;
        }

        /** Current implementation just enables first renderer it finds, overwrite with more deailed behaviour */
        virtual protected void HandleDrop() {
            GetComponentInChildren<Renderer>().enabled = true;            
        }

        private bool mouseOver;

        private void OnMouseEnter() { mouseOver = true; OnHover(); }

        private void OnMouseExit() {  mouseOver = false; OnStopHover(); }

        private void Update()
        {
            if (
                mouseOver 
                && Mouse.current.leftButton.wasPressedThisFrame 
                && CanPickup()                
            )
            {
                if (Inventory.instance.PickUp(this))
                {
                    HandlePickup();
                } else
                {
                    Debug.Log($"Inventory refused picking up {Item.Id}");
                }
            }
        }

        public void Drop(Vector2Int coordinates)
        {
            // TODO: Actually figure out how to drop
            HandleDrop();
        }
    }
}