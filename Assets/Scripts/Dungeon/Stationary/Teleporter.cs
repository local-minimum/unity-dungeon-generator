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
        [SerializeField] int PortalMaterialIndex;
        [SerializeField, Range(64, 2048)] int CameraTextureHeight = 512;
        [SerializeField, Range(64, 2048)] int CameraTextureWidth = 256;
        [SerializeField] Camera ExitViewCamera;
        [SerializeField] Shader shader;
        [SerializeField] string TextureName = "_MainTex";

        RenderTexture renderTexture;
        Material disabledMaterial;

        private void Start()
        {
            renderTexture = new RenderTexture(CameraTextureWidth, CameraTextureHeight, 16);
            renderTexture.Create();
            renderTexture.name = "Exit View Texture";            

            disabledMaterial = PortalRenderer.materials[PortalMaterialIndex];
            ExitViewCamera.targetTexture = renderTexture;    
        }

        public void ShowView()
        {
            var material = new Material(shader);
            material.name = "Exit View Material";
            material.SetTexture(TextureName, renderTexture);
                        
            Debug.Log($"Using {material}");
            PortalRenderer.SetMaterials(PortalRenderer.materials.Select((m, idx) => idx == PortalMaterialIndex ? material : m).ToList());            
        }

        public void HideView()
        {
            PortalRenderer.SetMaterials(PortalRenderer.materials.Select((m, idx) => idx == PortalMaterialIndex ? disabledMaterial : m).ToList());
        }

        private void OnDestroy()
        {
            renderTexture.Release();
        }
    }
}
