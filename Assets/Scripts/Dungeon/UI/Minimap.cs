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
            if (PlayerSettings.ShowMinimap.Value)
            {
                OnShowMinimap();
            } else
            {
                OnHideMinimap();
            }
        }

        private bool Visible
        {
            set
            {
                Map.SetActive(value);
                foreach (var button in Buttons)
                {
                    button.SetActive(value);
                }
            }
        }

        public void OnHideMinimap()
        {
            Visible = false;
        }

        public void OnShowMinimap()
        {
            Visible = true;
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