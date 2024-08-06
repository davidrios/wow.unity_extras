using System.Collections.Generic;
using UnityEngine;

namespace WoWUnityExtras
{
    public class CreatureSounds : MonoBehaviour
    {
        [SerializeField]
        private GameObject death;
        [SerializeField]
        private GameObject wingFlap;
        [SerializeField]
        private GameObject fidget1;
        [SerializeField]
        private GameObject fidget2;

        private SoundKit skDeath;
        private SoundKit skWingFlap;
        private SoundKit skFidget1;
        private SoundKit skFidget2;

        private readonly Dictionary<string, GameObject> skInstances = new();

        private SoundKit GetSoundKit(GameObject prefab, string name)
        {
            if (prefab == null)
                return null;

            if (!skInstances.TryGetValue(name, out GameObject instance))
            {
                instance = Instantiate(prefab, transform);
                skInstances.Add(name, instance);
            }

            return instance.GetComponentInChildren<SoundKit>();
        }

        public void PlayDeath()
        {
            if (death != null && skDeath == null)
                skDeath = GetSoundKit(death, "death");

            if (skDeath != null)
                skDeath.PlayRandom();
        }

        public void PlayWingFlap()
        {
            if (wingFlap != null && skWingFlap == null)
                skWingFlap = GetSoundKit(wingFlap, "wingFlap");

            if (skWingFlap != null)
                skWingFlap.PlayRandom();
        }

        public void PlayFidget1()
        {
            if (fidget1 != null && skFidget1 == null)
                skFidget1 = GetSoundKit(fidget1, "fidget1");

            if (skFidget1 != null)
                skFidget1.PlayRandom();
        }

        public void PlayFidget2()
        {
            if (fidget2 != null && skFidget2 == null)
                skFidget2 = GetSoundKit(fidget2, "fidget2");

            if (skFidget2 != null)
                skFidget2.PlayRandom();
        }
    }
}