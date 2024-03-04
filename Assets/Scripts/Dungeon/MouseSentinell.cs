using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon.World
{
    public class MouseSentinell : MonoBehaviour
    {
        [SerializeField, Tooltip("Who gets called else parent")] GameObject target;
        private bool mouseOver;

        GameObject Target => target == null ? transform.parent.gameObject : target;

        private void OnMouseEnter() {
            mouseOver = true;
            Target.SendMessage("OnBeginHover", SendMessageOptions.DontRequireReceiver);
        }

        private void OnMouseExit() { 
            mouseOver = false;
            Target.SendMessage("OnEndHover", SendMessageOptions.DontRequireReceiver);
        }

        private void Update()
        {
            if (mouseOver && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Target.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
