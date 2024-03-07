using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.UI
{
    public class LevelMapCamera : Singleton<LevelMapCamera>
    {
        Camera cam;
        float elevation;

        [SerializeField, Range(0, 4)]
        float sizeFactor = 0.5f;

        private void Start()
        {
            cam = GetComponent<Camera>();
            elevation = transform.position.y;
        }


        public void AdjustView()
        {
            var area = DungeonLevelGenerator.instance.DungeonGrid.BoundingBox;
            var center = area.center;
            var size = area.size;
            cam.transform.position = new Vector3(center.x, elevation, center.y);
            cam.orthographicSize = Mathf.Max(size.x, size.y) * sizeFactor;
        }

#if UNITY_EDITOR
        [SerializeField]
        bool debugAdjust = true;
        private void Update()
        {
            if (debugAdjust) AdjustView();
        }
#endif

    }

}