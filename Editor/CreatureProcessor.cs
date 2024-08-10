using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WoWUnityExtras.Database;

namespace WoWUnityExtras
{
    public class CreatureProcessor
    {
        public static (CreatureData data, string rootDir, string creaturesDir, List<Database.DisplayInfo> creatureDisplays) ParseCreatureData(TextAsset creatureDataJson)
        {
            var creatureData = JsonConvert.DeserializeObject<CreatureData>(creatureDataJson.text);

            var fileDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(creatureDataJson));
            var rootDir = Path.GetDirectoryName(fileDir);

            var creaturesDir = Path.Join(fileDir, "creatures");
            if (!Directory.Exists(creaturesDir))
                Directory.CreateDirectory(creaturesDir);

            List<Database.DisplayInfo> creatureDisplays = new()
            {
                creatureData.displayInfo[creatureData.info.modelid1.ToString()]
            };
            if (creatureData.displayInfo.TryGetValue(creatureData.info.modelid2.ToString(), out var model2))
                creatureDisplays.Add(model2);
            if (creatureData.displayInfo.TryGetValue(creatureData.info.modelid3.ToString(), out var model3))
                creatureDisplays.Add(model3);
            if (creatureData.displayInfo.TryGetValue(creatureData.info.modelid4.ToString(), out var model4))
                creatureDisplays.Add(model4);

            return (creatureData, rootDir, creaturesDir, creatureDisplays);
        }

        public static void CreateCreaturePrefabsFromTemplate(TextAsset jsonAsset)
        {
            var (creatureData, rootDir, creaturesDir, creatureDisplays) = ParseCreatureData(jsonAsset);

            foreach (var creatureDisplay in creatureDisplays)
            {
                var textureName = Path.GetFileNameWithoutExtension(creatureDisplay.TextureVariationFileData[0]);
                var prefabPath = Path.Join(rootDir, $"{creatureDisplay.model.FileData.Replace(".m2", "")}__{textureName}.prefab");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not found: {prefabPath}");
                    continue;
                }

                var creaturePath = Path.Join(creaturesDir, $"{creatureData.info.entry}_{creatureDisplay.ID}.prefab");
                var creaturePrefab = PrefabUtility.SaveAsPrefabAsset(prefab, creaturePath);
                creaturePrefab.transform.localScale = creatureDisplay.model.ModelScale * creatureDisplay.CreatureModelScale * Vector3.one;

                {
                    if (creaturePrefab.TryGetComponent<CharacterController>(out var characterController))
                        characterController.stepOffset = creatureDisplay.model.ModelScale * creatureDisplay.CreatureModelScale / 3;
                }

                if (creaturePrefab.TryGetComponent<Creature>(out var creature))
                    creature.creatureName = creatureData.info.name;
                PrefabUtility.SavePrefabAsset(creaturePrefab);
            }
        }

        public static Vector3 GetCreaturePosition(CreatureTableRow creatureRow)
        {
            return new Vector3(creatureRow.position_y, creatureRow.position_z, -creatureRow.position_x);
        }

        public static (List<CreatureTableRow> table, Bounds bounds) GetTableAndBounds(TextAsset creatureTableJson, GameObject mapArea)
        {
            Bounds bounds = mapArea.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;

            foreach (var meshFilter in mapArea.GetComponentsInChildren<MeshFilter>())
            {
                bounds.Encapsulate(meshFilter.sharedMesh.bounds);
            }

            return (JsonConvert.DeserializeObject<List<CreatureTableRow>>(creatureTableJson.text), bounds);
        }

        public static void PrintContainingCreatureIDs(TextAsset creatureTableJson, GameObject mapArea, int mapId)
        {
            var (creatureTable, bounds) = GetTableAndBounds(creatureTableJson, mapArea);

            Dictionary<int, int> ids = new();
            foreach (var creatureRow in creatureTable)
            {
                if (creatureRow.map != mapId)
                    continue;

                if (!bounds.Contains(GetCreaturePosition(creatureRow)))
                    continue;

                if (ids.TryGetValue(creatureRow.id1, out var count))
                    ids[creatureRow.id1] = count + 1;
                else
                    ids.Add(creatureRow.id1, 1);
            }

            var idsS = ids.Select(item => (item.Key, item.Value)).ToList();
            idsS.Sort();
            Debug.Log($"ids: ...\n{String.Join("\n", idsS)}");
        }

        public static void PlaceCreatureSpawners(TextAsset creatureTableJson, GameObject mapArea, GameObject target, int mapId, TextAsset creatureDataJson, bool randomRotation = true)
        {
            var (creatureData, _, creaturesDir, creatureDisplays) = ParseCreatureData(creatureDataJson);

            List<GameObject> prefabs = new();
            foreach (var creatureDisplay in creatureDisplays)
            {
                var creaturePath = Path.Join(creaturesDir, $"{creatureData.info.entry}_{creatureDisplay.ID}.prefab");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(creaturePath);

                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not found: {creaturePath}");
                    continue;
                }

                prefabs.Add(prefab);
            }

            if (prefabs.Count == 0)
            {
                Debug.LogWarning("No prefabs to add.");
                return;
            }

            var (creatureTable, bounds) = GetTableAndBounds(creatureTableJson, mapArea);

            foreach (var creatureRow in creatureTable)
            {
                if (creatureRow.id1 != creatureData.info.entry || creatureRow.map != mapId)
                    continue;

                var point = GetCreaturePosition(creatureRow);
                if (!bounds.Contains(point))
                    continue;

                var spawner = new GameObject($"Spawner_{creatureRow.guid}");
                spawner.transform.parent = target.transform;
                spawner.transform.localPosition = point;
                if (randomRotation)
                    spawner.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);

                var spawnerComponent = spawner.AddComponent<CreatureSpawner>();
                spawnerComponent.spawnTime = creatureRow.spawntimesecs;

                foreach (var prefab in prefabs)
                {
                    var instance = PrefabUtility.InstantiatePrefab(prefab, spawner.transform) as GameObject;
                    if (instance.TryGetComponent<Creature>(out var creature))
                    {
                        creature.wanderRange = creatureRow.wander_distance;
                        creature.wanderMinDistance = creature.wanderRange / 3;
                    }
                }
            }
        }

        public static void SetupAnimations(AnimatorController controller)
        {
            controller.parameters = new AnimatorControllerParameter[2] {
                new() { name = "state", type = AnimatorControllerParameterType.Int },
                new() { name = "idleState", type = AnimatorControllerParameterType.Int },
            };

            Dictionary<string, ChildAnimatorState> knownStates = new();
            foreach (var state in controller.layers[0].stateMachine.states)
            {
                var match = Regex.Match(state.state.name, @"^.+ID (\d+) variation (\d+)");
                if (!match.Success)
                    continue;

                var id = match.Groups[1].Value;
                var variation = match.Groups[2].Value;

                switch (id)
                {
                    case "0":
                    case "1":
                    case "4":
                        break;

                    default:
                        continue;

                };

                knownStates[$"{id}_{variation}"] = state;
                state.state.transitions = new AnimatorStateTransition[0];
            }

            List<AnimatorState> idleStates = new();
            List<AnimatorState> allStates = new();

            if (knownStates.TryGetValue("0_0", out var defaultIdle))
            {
                idleStates.Add(defaultIdle.state);
                allStates.Add(defaultIdle.state);

                for (var i = 1; i < 10; i++)
                {
                    if (knownStates.TryGetValue($"0_{i}", out var altIdle))
                    {
                        idleStates.Add(altIdle.state);
                        allStates.Add(altIdle.state);

                        defaultIdle.state.AddTransition(new AnimatorStateTransition()
                        {
                            destinationState = altIdle.state,
                            conditions = new AnimatorCondition[2] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                                new () { parameter = "idleState", mode = AnimatorConditionMode.Equals, threshold = i },
                            },
                            hasExitTime = false
                        });

                        altIdle.state.AddTransition(new AnimatorStateTransition()
                        {
                            destinationState = defaultIdle.state,
                            conditions = new AnimatorCondition[2] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                                new () { parameter = "idleState", mode = AnimatorConditionMode.Equals, threshold = 0 },
                            },
                        });
                    }
                }

                var hasVariations = false;
                foreach (var behaviour in defaultIdle.state.behaviours)
                    hasVariations = hasVariations || behaviour is IdleVariations;

                if(!hasVariations)
                {
                    var behaviour = defaultIdle.state.AddStateMachineBehaviour<IdleVariations>();
                    behaviour.idleVariations = idleStates.Count;
                }
            }

            if (knownStates.TryGetValue("4_0", out var walkState))
            {
                foreach (var state in allStates)
                {
                    state.AddTransition(new AnimatorStateTransition()
                    {
                        destinationState = walkState.state,
                        conditions = new AnimatorCondition[1] {
                            new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 4 },
                        },
                        hasExitTime = false
                    });
                }

                if (idleStates.Count > 0)
                {
                    walkState.state.AddTransition(new AnimatorStateTransition()
                    {
                        destinationState = idleStates[0],
                        conditions = new AnimatorCondition[1] {
                            new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                        },
                        hasExitTime = false
                    });
                }

                allStates.Add(walkState.state);
            }

            if (knownStates.TryGetValue("1_0", out var deathState))
            {
                foreach (var state in allStates)
                {
                    state.AddTransition(new AnimatorStateTransition()
                    {
                        destinationState = deathState.state,
                        conditions = new AnimatorCondition[1] {
                            new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 1 },
                        },
                        hasExitTime = false
                    });
                }

                Debug.LogWarning($"Remember to set up loop time for the animations.");
            }

            AssetDatabase.SaveAssetIfDirty(controller);
        }
    }
}