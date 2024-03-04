using ProcDungeon.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcDungeon.World
{
    public class Inventory : Singleton<Inventory>
    {
        List<AbstractItem> inventory = new List<AbstractItem>();
        public IEnumerable<DungeonItem> Items => inventory.Select(i => i.Item);

        public bool PickUp(AbstractItem item)
        {
            inventory.Add(item);

            item.transform.parent = transform;

            Debug.Log($"Picked up {item.Item.Id}; {inventory.Count} items in inventory");
            GameLog.instance.LogPlayer("picked up", item.Item.Name);
            return true;
        }

        public bool HasKeyToDoor(DungeonDoor door, out string itemName)
        {
            foreach (AbstractItem item in inventory)
            {
                if (item is SpecificKey)
                {
                    var key = (SpecificKey)item;

                    if (key == null) continue;

                    if (key.Key.Door == door)
                    {
                        itemName = key.Key.Id;
                        return true;
                    }
                }                
            }

            itemName = null;
            return false;
        }
    }
}