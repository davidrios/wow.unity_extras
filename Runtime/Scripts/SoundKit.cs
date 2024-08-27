using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WoWUnityExtras
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundKit : MonoBehaviour
    {
        private readonly List<AudioSource> audioSources = new();
        private readonly Dictionary<AudioSource, float> originalVolume = new();
        private readonly Dictionary<AudioSource, bool> isStopping = new();
        private AudioSource currentPlaying;

        public void PopulateSources()
        {
            foreach (AudioSource source in GetComponentsInChildren<AudioSource>())
            {
                audioSources.Add(source);
                originalVolume.Add(source, source.volume);
            }
        }

        public void PlayRandom(bool stopFirst = false)
        {
            if (audioSources.Count == 0)
                PopulateSources();

            if (currentPlaying != null && stopFirst)
                currentPlaying.Stop();

            currentPlaying = audioSources[Random.Range(0, audioSources.Count)];
            isStopping[currentPlaying] = false;
            currentPlaying.volume = originalVolume[currentPlaying];
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
            isStopping[audioSource] = true;
            currentPlaying = null;
            var startVolume = originalVolume[audioSource];

            while (audioSource.volume > 0)
            {
                if (!isStopping[audioSource])
                    yield break;

                audioSource.volume -= startVolume * Time.deltaTime / time;
                yield return null;
            }

            if (!isStopping[audioSource])
                yield break;

            audioSource.Stop();
            audioSource.volume = originalVolume[audioSource];
            isStopping[audioSource] = false;
        }

        public bool IsPlaying()
        {
            if (currentPlaying == null)
                return false;

            return currentPlaying.isPlaying;
        }
    }
}