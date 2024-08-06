using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WoWUnityExtras
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundKit : MonoBehaviour
    {
        private readonly List<AudioSource> audioSources = new();

        public void PopulateSources()
        {
            foreach (AudioSource source in GetComponentsInChildren<AudioSource>())
            {
                audioSources.Add(source);
            }
        }

        public void PlayRandom()
        {
            if (audioSources.Count == 0)
                PopulateSources();

            audioSources[Random.Range(0, audioSources.Count)].Play();
        }
    }

    [CustomEditor(typeof(SoundKit))]
    public class SoundKitEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var soundKit = target as SoundKit;

            if (GUILayout.Button("Play random"))
                soundKit.PlayRandom();
        }
    }
}