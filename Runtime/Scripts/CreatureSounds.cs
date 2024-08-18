using System.Collections.Generic;
using UnityEngine;

namespace WoWUnityExtras
{
    public class CreatureSounds : MonoBehaviour
    {
        [SerializeField]
        private GameObject death;
        [SerializeField]
        private GameObject step;
        [SerializeField]
        private GameObject wingFlap;
        [SerializeField]
        private GameObject fidget1;
        [SerializeField]
        private GameObject fidget2;
        [SerializeField]
        private GameObject fidget3;
        [SerializeField]
        private GameObject fidget4;
        [SerializeField]
        private GameObject fidget5;

        private SoundKit skDeath;
        private SoundKit skStep;
        private SoundKit skWingFlap;
        private SoundKit skFidget1;
        private SoundKit skFidget2;
        private SoundKit skFidget3;
        private SoundKit skFidget4;
        private SoundKit skFidget5;

        private readonly Dictionary<string, GameObject> skInstances = new();

        public void SetDeath(GameObject prefab, bool replace = false) { if (replace || death == null) death = prefab; }
        public void SetWingFlap(GameObject prefab, bool replace = false) { if (replace || wingFlap == null) wingFlap = prefab; }
        public void SetFidget1(GameObject prefab, bool replace = false) { if (replace || fidget1 == null) fidget1 = prefab; }
        public void SetFidget2(GameObject prefab, bool replace = false) { if (replace || fidget2 == null) fidget2 = prefab; }
        public void SetFidget3(GameObject prefab, bool replace = false) { if (replace || fidget3 == null) fidget3 = prefab; }
        public void SetFidget4(GameObject prefab, bool replace = false) { if (replace || fidget4 == null) fidget4 = prefab; }
        public void SetFidget5(GameObject prefab, bool replace = false) { if (replace || fidget5 == null) fidget5 = prefab; }

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

        public void PlayStep()
        {
            if (step != null && skStep == null)
                skStep = GetSoundKit(step, "step");

            if (skStep != null)
                skStep.PlayRandom();
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

        public void PlayFidget3()
        {
            if (fidget3 != null && skFidget3 == null)
                skFidget3 = GetSoundKit(fidget3, "fidget3");

            if (skFidget3 != null)
                skFidget3.PlayRandom();
        }

        public void PlayFidget4()
        {
            if (fidget4 != null && skFidget4 == null)
                skFidget4 = GetSoundKit(fidget4, "fidget4");

            if (skFidget4 != null)
                skFidget4.PlayRandom();
        }

        public void PlayFidget5()
        {
            if (fidget5 != null && skFidget5 == null)
                skFidget5 = GetSoundKit(fidget5, "fidget5");

            if (skFidget5 != null)
                skFidget5.PlayRandom();
        }
    }
}