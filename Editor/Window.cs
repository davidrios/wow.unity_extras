using System.IO;
using UnityEditor;
using UnityEngine;

namespace WoWUnityExtras
{
    public class Window : EditorWindow
    {
        private TextAsset selectedJson;
        private GameObject selectedGameObject;

        private TextAsset creatureSpawnJson;
        private TextAsset spawnCreatureDataJson;
        private GameObject spawnMapReference;
        private int spawnMapId;

        [MenuItem("Window/wow.unity_extras")]
        public static void ShowWindow()
        {
            GetWindow<Window>("wow.unity_extras");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnJsonSelectionChange;
            Selection.selectionChanged += OnGameObjectSelectionChange;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnJsonSelectionChange;
            Selection.selectionChanged -= OnGameObjectSelectionChange;
        }

        void OnGameObjectSelectionChange()
        {
            var selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable | SelectionMode.ExcludePrefab);
            if (selected.Length > 0)
                selectedGameObject = selected[0];
            else
                selectedGameObject = null;

            Repaint();
        }

        void OnJsonSelectionChange()
        {
            var selected = Selection.GetFiltered<TextAsset>(SelectionMode.Unfiltered);
            if (selected.Length > 0)
            {
                if (Path.GetExtension(AssetDatabase.GetAssetPath(selected[0])).ToLower() != ".json")
                    selectedJson = null;
                else
                    selectedJson = selected[0];
            }

            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("Creatures", EditorStyles.boldLabel);
            selectedJson = EditorGUILayout.ObjectField("Creature Data JSON: ", selectedJson, typeof(TextAsset), false) as TextAsset;
            if (selectedJson != null)
            {
                if (GUILayout.Button("Create Prefabs"))
                    CreatureProcessor.CreateCreaturePrefabsFromTemplate(selectedJson);
            }

            GUILayout.Space(10);
            GUILayout.Label("Creature Spawners", EditorStyles.boldLabel);
            creatureSpawnJson = EditorGUILayout.ObjectField("Creature Table JSON: ", creatureSpawnJson, typeof(TextAsset), false) as TextAsset;
            if (creatureSpawnJson != null)
            {
                if (Path.GetExtension(AssetDatabase.GetAssetPath(creatureSpawnJson)).ToLower() != ".json")
                    creatureSpawnJson = null;
            }

            spawnMapReference = EditorGUILayout.ObjectField("Map Area Reference", spawnMapReference, typeof(GameObject), false) as GameObject;
            GUILayout.BeginHorizontal();
            try
            {
                spawnMapId = EditorGUILayout.IntField("Map ID", spawnMapId);
                if (GUILayout.Button("Print Containing Creature IDs"))
                    CreatureProcessor.PrintContainingCreatureIDs(creatureSpawnJson, spawnMapReference, spawnMapId);
            }
            finally
            {
                GUILayout.EndHorizontal();
            }

            spawnCreatureDataJson = EditorGUILayout.ObjectField("Creature Data JSON: ", spawnCreatureDataJson, typeof(TextAsset), false) as TextAsset;
            if (spawnCreatureDataJson != null)
            {
                if (Path.GetExtension(AssetDatabase.GetAssetPath(spawnCreatureDataJson)).ToLower() != ".json")
                    spawnCreatureDataJson = null;
            }

            if (selectedGameObject == null)
            {
                GUILayout.Label("Select a game object to place the spawners in.");
            }
            else
            {
                GUILayout.Label($"Place in game object: {selectedGameObject.name}");

                if (creatureSpawnJson != null && spawnMapReference != null)
                {
                    if (GUILayout.Button("Place Spawners"))
                        CreatureProcessor.PlaceCreatureSpawners(creatureSpawnJson, spawnMapReference, selectedGameObject, spawnMapId, spawnCreatureDataJson);
                }
            }
        }
    }
}
