using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProcDungeon.UI
{
    public class Minimap : Singleton<Minimap>
    {
        [SerializeField]
        GameObject Map;

        [SerializeField]
        GameObject[] Buttons;

        [SerializeField]
        TMPro.TextMeshProUGUI OrientationModeText;

        private void Start()
        {
            OrientationModeText.text = MinimapCamera.instance.StaticNorth ? "N" : "P";
        }

        public void OnHideMinimap()
        {
            Map.SetActive(false);
            foreach (var button in Buttons)
            {
                button.SetActive(false);
            }
        }

        public void OnToggleOrientationMode()
        {
            MinimapCamera.instance.ToggleCameraMode();
            OrientationModeText.text = MinimapCamera.instance.StaticNorth ? "N" : "P";
        }

        public void OnZoomIn()
        {
            MinimapCamera.instance.ZoomIn();
        }

        public void OnZoomOut()
        {
            MinimapCamera.instance.ZoomOut();
        }
    }
}