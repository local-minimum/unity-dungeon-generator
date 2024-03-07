using ProcDungeon.World;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon.UI
{
    public class LevelMap : MonoBehaviour
    {
        [SerializeField]
        GameObject map;

        bool showMap = false;

        private void Start()
        {
            map.SetActive(showMap);
        }



        public void ToggleMap(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                showMap = !showMap;
                map.SetActive(showMap);
                if (showMap)
                {
                    PlayerController.Instance.InputBlockers.Add(this);
                } else
                {
                    PlayerController.Instance.InputBlockers.Remove(this);
                }
            }
        }
    }
}