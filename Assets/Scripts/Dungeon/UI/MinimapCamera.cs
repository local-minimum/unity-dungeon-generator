using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.UI
{
    public class MinimapCamera : Singleton<MinimapCamera>
    {        
        public bool StaticNorth { get; private set; } = true;
        Camera cam;

        float defaultZoom;

        float zoom;
        
        [SerializeField]
        float zoomStep = 3;

        [SerializeField]
        float minZoom = 12;

        [SerializeField]
        float maxZoom = 36;


        public void ZoomOut()
        {
            zoom = Mathf.Min(maxZoom, zoom + zoomStep);
            cam.orthographicSize = zoom;
        }

        public void ZoomIn()
        {
            zoom = Mathf.Max(minZoom, zoom - zoomStep);
            cam.orthographicSize = zoom;
        }

        public void ResetZoom()
        {
            zoom = defaultZoom;
            cam.orthographicSize = zoom;
        }

        private void Start()
        {
            cam = GetComponent<Camera>();
            defaultZoom = cam.orthographicSize;
            zoom = defaultZoom;
        }

        public void ToggleCameraMode()
        {
            StaticNorth = !StaticNorth;
        }

        private void Update()
        {
            if (StaticNorth)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(Vector3.down, transform.parent.forward);
            }
        }
    }
}