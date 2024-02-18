using UnityEngine;

namespace ProcDungeon
{
    public enum EntityType { Unknown, Player, NPC, Enemy };

    public class DungeonEntity : MonoBehaviour
    {
        public EntityType EntityType;
    }
}
