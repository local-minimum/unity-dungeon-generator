using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon.World
{
    public class WorldFeatureSpawner : Singleton<WorldFeatureSpawner>
    {
        [SerializeField]
        GameObject fallbackFloorPrefab;

        [SerializeField]
        GameObject fallbackWallPrefab;

        [SerializeField]
        AbstractDoorController fallbackDoorPrefab;

        [SerializeField]
        SpecificKey fallbackKeyPrefab;

        public GameObject SpawnFloor(string floorVersion, Transform parent = null)
        {
            return Instantiate(fallbackFloorPrefab, parent);
        }

        public GameObject SpawnWall(string wallVersion, Transform paret = null)
        {
            return Instantiate(fallbackWallPrefab, paret);
        }

        public AbstractDoorController SpawnDoor(string doorVersion, Transform parent = null)
        {
            return Instantiate(fallbackDoorPrefab, parent);
        }

        public SpecificKey SpawnKey(string keyVersion, Transform parent = null)
        {
            return Instantiate(fallbackKeyPrefab, parent);
        }
    }
}
