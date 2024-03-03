using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon.World
{
    public class Teleporter : MonoBehaviour
    {
        public bool HubSide { get; set; }
        public Vector2Int Coordinates { get; set; }
        public Vector2Int ExitDirection { get; set; }

        public Teleporter PairedTeleporter { get; set; }

        [SerializeField] MeshRenderer PortalRenderer;
        [SerializeField, Range(64, 2048)] int CameraTextureHeight = 512;
        [SerializeField, Range(64, 2048)] int CameraTextureWidth = 256;
        [SerializeField] Camera ExitViewCamera;

        RenderTexture renderTexture;
        Material exitViewMaterial;

        private void Start()
        {
            renderTexture = new RenderTexture(CameraTextureWidth, CameraTextureHeight, 16);
            renderTexture.Create();
            renderTexture.name = "Exit View Texture";

            PairedTeleporter.ExitViewCamera.targetTexture = renderTexture;

            exitViewMaterial = PortalRenderer.material;
            exitViewMaterial.mainTexture = renderTexture;
            exitViewMaterial.color = Color.white;

            PortalRenderer.sharedMaterial = exitViewMaterial;

        }

        void Update()
        {
            // You can only do this in Built-in, not URP/HDRP but this will guarantee that the texture has been rendered before you use it
            PairedTeleporter.ExitViewCamera.Render();
        }

        public void ShowView()
        {
            PortalRenderer.enabled = true;            
        }

        public void HideView()
        {
            PortalRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            renderTexture.Release();
            Destroy(exitViewMaterial);
        }
    }
}
