using UnityEngine;

namespace WoWUnityExtras
{
    [CreateAssetMenu(fileName = "IdleVariationsSettings", menuName = "wow.unity_extras/IdleVariationsSettings", order = 1)]
    public class IdleVariationsSettings : ScriptableObject
    {
        public float variationChance = 0.1f;
        public float checkInterval = 0.1f;
    }
}