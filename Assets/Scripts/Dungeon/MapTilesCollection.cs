using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon {
    public class MapTilesCollection : Singleton<MapTilesCollection> 
    {
        [SerializeField]
        List<string> keys = new List<string>();
        [SerializeField]
        List<GameObject> tiles = new List<GameObject>();

        Dictionary<string, GameObject> lookup;

        // TODO: Might want a fallback tile

        public GameObject GetPrefab(string key)
        {
            if (lookup == null)
            {
                InitLookup();
            }

            if (lookup.ContainsKey(key))
            {
                return lookup[key];
            }
            Debug.LogWarning($"Requested map tile '{key}' not know");
            return null;
        }

        private void InitLookup()
        {
            int nKeys = keys.Count;
            int nTiles = tiles.Count;
            if (nKeys != nTiles)
            {
                Debug.LogWarning("Number of keys not same as number of map tile prefabs");

            }

            lookup = new Dictionary<string, GameObject>();

            for (int i = 0, n = Mathf.Min(nKeys, nTiles); i < n; i++)
            {
                lookup.Add(keys[i], tiles[i]);
            }
        }
    }

}