using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace WoWUnityExtras
{
    [CustomEditor(typeof(Creature))]
    public class CreatureEditor : Editor
    {
        private bool isMoving = false;
        private Vector2 direction = Vector2.zero;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var creature = target as Creature;

            GUILayout.Space(10);
            if (GUILayout.Button("Die"))
                creature.Die();

            GUILayout.Space(10);
            GUILayout.Label("Move", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            try
            {
                Dictionary<string, bool> pressed = new()
                {
                    { "left", GUILayout.RepeatButton("←") },
                    { "right", GUILayout.RepeatButton("→") },
                    { "up", GUILayout.RepeatButton("↑") },
                    { "down", GUILayout.RepeatButton("↓") }
                };

                var match = pressed.FirstOrDefault(item => item.Value);
                if (match.Key != null && !isMoving)
                {
                    direction = match.Key switch
                    {
                        "left" => Vector2.left,
                        "right" => Vector2.right,
                        "up" => Vector2.up,
                        "down" => Vector2.down,
                        _ => Vector2.zero,
                    };

                    isMoving = true;
                    EditorCoroutineUtility.StartCoroutineOwnerless(Move());
                }
                else
                {
                    isMoving = false;
                }
            }
            finally
            {
                GUILayout.EndHorizontal();
            }
        }

        private IEnumerator Move()
        {
            var creature = target as Creature;

            while (isMoving)
            {
                creature.Move(direction);
                yield return null;
            }

            creature.Move(Vector2.zero);
        }
    }
}