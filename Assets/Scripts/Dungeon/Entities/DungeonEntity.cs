using UnityEngine;

namespace ProcDungeon
{
    public enum EntityType { Unknown, Player, NPC, Enemy };

    /** Anything alive that may move about in the world and interact it and other entities */
    public class DungeonEntity : MonoBehaviour
    {
        public EntityType EntityType;
    }
}
