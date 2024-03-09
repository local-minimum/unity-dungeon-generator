using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.Experimental
{
    public class ElevationNoiseSubscriber : MonoBehaviour
    {

        bool setBaseY;
        float baseY;

        void Update()
        {
            if (!setBaseY)
            {
                baseY = transform.position.y;
                setBaseY = true;
            }

            transform.position = new Vector3(transform.position.x, baseY + ElevationNoise.instance.Noise(transform.position), transform.position.z);
        }
    }
}