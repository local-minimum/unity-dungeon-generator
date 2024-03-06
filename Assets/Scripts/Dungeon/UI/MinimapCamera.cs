using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField]
    bool staticNorth = true;

    private void Update()
    {
        if (staticNorth)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        } else
        {
            transform.rotation = Quaternion.LookRotation(Vector3.down, transform.parent.forward);
        }
    }
}
