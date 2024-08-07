using UnityEditor;
using UnityEngine;

namespace WoWUnityExtras
{
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