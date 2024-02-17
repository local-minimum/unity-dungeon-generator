using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ActionMapsController : MonoBehaviour
{
    [SerializeField]
    bool AllowDebugActions;

    [SerializeField]
    string DebugActionMap;

    PlayerInput playerInput;

    string previousActionMap;


    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        previousActionMap = playerInput.currentActionMap.name;
    }

    public void OnUseDebugActionMap(InputAction.CallbackContext context)
    {
        if (AllowDebugActions && context.performed) {
            playerInput.SwitchCurrentActionMap(DebugActionMap);
        }
    }

    public void OnRestoreActionMapOnCancel(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            playerInput.SwitchCurrentActionMap(previousActionMap);
        }
    }

}
