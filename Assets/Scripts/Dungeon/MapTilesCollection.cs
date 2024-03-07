using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcDungeon {
    public class MapTilesCollection : Singleton<MapTilesCollection> 
    {
        [SerializeField]
        List<string> keys = new List<string>();

        [SerializeField]
        Renderer TilePrefab;

        [SerializeField]
        List<Material> materals = new List<Material>();

        Dictionary<string, Material> lookup;
        

        public GameObject GetPrefab(string key)
        {
            if (lookup == null)
            {
                InitLookup();
            }

            if (lookup.ContainsKey(key))
            {
                var rend = Instantiate(TilePrefab);
                rend.material = lookup[key];
                return rend.gameObject;
            }
            Debug.LogWarning($"Requested map tile '{key}' not know");
            return null;
        }

        private void InitLookup()
        {
            int nKeys = keys.Count;
            int nMats = materals.Count;
            if (nKeys != nMats)
            {
                Debug.LogWarning("Number of keys not same as number of map tile prefabs");

            }

            lookup = new Dictionary<string, Material>();

            for (int i = 0, n = Mathf.Min(nKeys, nMats); i < n; i++)
            {
                lookup.Add(keys[i], materals[i]);
            }
        }
    }

}