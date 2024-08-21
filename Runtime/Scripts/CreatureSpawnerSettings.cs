using UnityEngine;

namespace WoWUnityExtras
{
    [CreateAssetMenu(fileName = "CreatureSpawnerSettings", menuName = "wow.unity_extras/CreatureSpawnerSettings", order = 1)]
    public class CreatureSpawnerSettings : ScriptableObject
    {
        public float despawnTime = 30;
        public float spawnDistance = 100f;
        public string playerTag = "Player";
    }
}