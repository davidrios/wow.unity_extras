using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WoWUnityExtras
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundKit : MonoBehaviour
    {
        private readonly List<AudioSource> audioSources = new();
        private AudioSource currentPlaying;

        public void PopulateSources()
        {
            foreach (AudioSource source in GetComponentsInChildren<AudioSource>())
            {
                audioSources.Add(source);
            }
        }

        public void PlayRandom(bool stopFirst = false)
        {
            if (audioSources.Count == 0)
                PopulateSources();

            if (currentPlaying != null && stopFirst)
                currentPlaying.Stop();

            currentPlaying = audioSources[Random.Range(0, audioSources.Count)];
            currentPlaying.Play();
        }

        public void StopPlaying()
        {
            if (currentPlaying != null)
            {
                currentPlaying.Stop();
                currentPlaying = null;
            }
        }

        public IEnumerator FadeStop(float time = 5)
        {
            if (currentPlaying == null)
                yield break;

            var audioSource = currentPlaying;
            currentPlaying = null;
            float startVolume = audioSource.volume;

            while (audioSource.volume > 0)
            {
                audioSource.volume -= startVolume * Time.deltaTime / time;
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume;
        }

        public bool IsPlaying()
        {
            if (currentPlaying == null)
                return false;

            return currentPlaying.isPlaying;
        }
    }
}