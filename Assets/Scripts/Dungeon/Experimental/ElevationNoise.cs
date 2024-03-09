using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.Experimental
{
    public class ElevationNoise : Singleton<ElevationNoise>
    {
        [SerializeField, Range(0, 10)]
        float timeResolution = 1;

        [SerializeField, Range(0, 10)]
        float positionScaler = 1;

        [SerializeField, Range(0, 10)]
        float noiseMagnitude = 1;

        [SerializeField]
        bool active = true;

        [SerializeField]
        bool glitchy = false;

        [SerializeField, Range(0, 1)]
        float glitchOn = 0.01f;

        [SerializeField, Range(0, 1)]
        float glitchOff = 0.4f;

        public float Noise(Vector3 position) => active ? noiseMagnitude * Mathf.PerlinNoise(position.x * positionScaler + timeResolution * Time.timeSinceLevelLoad, position.z) : 0;


        private void Update()
        {
            if (glitchy)
            {
                if (active && Random.value < glitchOff)
                {
                    active = false;
                } else if (!active && Random.value < glitchOn)
                {
                    active = true;
                }
            }
        }
    }
}
