using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DistanceToFloor : MonoBehaviour
{
    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo))
        {
            var height = hitInfo.distance;
            Debug.Log(height);
        }
    }
}
