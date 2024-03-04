using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProcDungeon.World
{
    public class SavePlace : MonoBehaviour
    {
        private void OnBeginHover()
        {
            Debug.Log("Considering saving");
        }

        private void OnEndHover()
        {
            Debug.Log("Done considering saving");
        }

        private void OnClick()
        {
            Debug.Log("Save!");

            Rest();
            SaveState();            
        }

        void Rest()
        {

        }

        void SaveState()
        {
            string levelName = "xxx";
            int seed = DungeonLevelGenerator.instance.Seed;
            // Do we need settings?
            var hubTeleporters = DungeonHub.instance.TeleporterPlacements.ToList();

            // Characters
            var inventory = Inventory.instance.Items.ToList();

        }
    }
}
