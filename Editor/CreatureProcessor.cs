using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WowUnity;
using WoWUnityExtras.Database;

namespace WoWUnityExtras
{
    public class CreatureProcessor
    {
        public static (CreatureData data, string rootDir, string creaturesDir, List<Database.DisplayInfo> creatureDisplays) ParseCreatureData(TextAsset creatureDataJson)
        {
            var creatureData = JsonConvert.DeserializeObject<CreatureData>(creatureDataJson.text);
            if (creatureData.displayInfo == null)
                throw new Exception("Invalid creature JSON");

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
                string prefabPath;

                if (creatureDisplay.extra != null)
                {
                    prefabPath = Path.Join(rootDir, Path.ChangeExtension(creatureDisplay.model.FileData, "prefab"));
                }
                else
                {
                    var textureName = Path.GetFileNameWithoutExtension(creatureDisplay.TextureVariationFileData[0]);
                    prefabPath = Path.Join(rootDir, Path.ChangeExtension(creatureDisplay.model.FileData, $"__{textureName}.prefab"));
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not found: {prefabPath}");
                    continue;
                }

                var creaturePath = Path.Join(creaturesDir, $"{creatureData.info.entry}_{creatureDisplay.ID}.prefab");
                var creaturePrefab = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(creaturePath) ?? prefab) as GameObject;

                creaturePrefab.transform.localScale = creatureDisplay.model.ModelScale * creatureDisplay.CreatureModelScale * Vector3.one;

                {
                    if (creaturePrefab.TryGetComponent<CharacterController>(out var characterController))
                        characterController.stepOffset = Math.Min(creatureDisplay.model.ModelScale * creatureDisplay.CreatureModelScale, characterController.height) / 3;
                }

                if (creaturePrefab.TryGetComponent<Creature>(out var creature))
                    creature.creatureName = creatureData.info.name;

                if (creatureDisplay.extra != null)
                {
                    var prefixGeo = creaturePrefab.transform.GetChild(0).name;
                    foreach (var geoset in creatureDisplay.geosets)
                    {
                        if (geoset.StartsWith("-"))
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[1..]}")?.gameObject.SetActive(false);
                        else
                        {
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[..^1]}1")?.gameObject.SetActive(false);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset}")?.gameObject.SetActive(true);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset}.001")?.gameObject.SetActive(true);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset}.002")?.gameObject.SetActive(true);
                        }
                    }

                    var material = MaterialUtility.GetBasicMaterial(Path.Join(rootDir, creatureDisplay.extra.BakeMaterialResourcesIDFile), (uint)MaterialUtility.BlendModes.Opaque);

                    var geoset0Obj = creaturePrefab.transform.Find($"{prefixGeo}_Geoset0")?.gameObject;
                    if (geoset0Obj != null && material != null)
                    {
                        var defaultMatName = geoset0Obj.GetComponent<Renderer>().sharedMaterial.name;
                        for (var i = 0; i < creaturePrefab.transform.childCount; i++)
                        {
                            var child = creaturePrefab.transform.GetChild(i).gameObject;
                            if (child.TryGetComponent<Renderer>(out var renderer))
                            {
                                if (renderer.sharedMaterial.name == defaultMatName)
                                {
                                    renderer.sharedMaterial = material;
                                }
                                else
                                {
                                    if (child.name.StartsWith($"{prefixGeo}_Hair") || child.name.StartsWith($"{prefixGeo}_Facial"))
                                    {
                                        var hairMaterial = MaterialUtility.GetBasicMaterial(Path.Join(rootDir, creatureDisplay.extra.HairTextureFile), (uint)MaterialUtility.BlendModes.AlphaKey);
                                        if (hairMaterial != null)
                                            renderer.sharedMaterial = hairMaterial;
                                    }
                                }
                            }
                        }
                    }

                    var attachmentsPath = Path.Join(rootDir, creatureDisplay.model.FileData.Replace(".m2", "_attachments.json"));
                    var attachmentsJson = AssetDatabase.LoadAssetAtPath<TextAsset>(attachmentsPath);
                    if (attachmentsJson != null)
                    {
                        void processSlot(string slotKey, int resourceIndex, int boneId)
                        {
                            if (creatureDisplay.itemSlots == null || !creatureDisplay.itemSlots.TryGetValue(slotKey, out var slotItem))
                                return;

                            var equipPrefabPath = Path.Join(rootDir, Path.ChangeExtension(slotItem.displayInfo.ModelResourcesIDFiles[resourceIndex], "prefab"));
                            var equipPrefab = M2Utility.FindPrefab(equipPrefabPath);
                            if (equipPrefab == null)
                            {
                                Debug.Log($"Couldn't find equipment prefab: {equipPrefabPath}");
                                return;
                            }

                            var bone = creaturePrefab.GetComponentsInChildren<Transform>().FirstOrDefault(item => item.gameObject.name == $"bone_{boneId}");
                            if (bone == null)
                            {
                                Debug.Log($"Couldn't find bone: {boneId}");
                                return;
                            }

                            if (bone.transform.Find("bone_equip") != null) // do nothing if it exists
                                return;

                            var equipInstance = PrefabUtility.InstantiatePrefab(equipPrefab, bone.transform) as GameObject;
                            equipInstance.name = "bone_equip";
                            equipInstance.transform.localRotation = Quaternion.Euler(0f, 180, 0f);
                            equipInstance.transform.localScale = Vector3.one * 0.01f;
                            if (equipInstance.TryGetComponent<LODGroup>(out var lodGroup))
                                UnityEngine.Object.DestroyImmediate(lodGroup);

                            equipInstance.isStatic = false;
                            foreach (var transform in equipInstance.GetComponentsInChildren<Transform>())
                                transform.gameObject.isStatic = false;

                            SetupEquipMaterials(equipPrefabPath, equipInstance, slotItem.displayInfo.ModelMaterialResourcesIDFiles[resourceIndex]);
                        }

                        var attachments = JsonConvert.DeserializeObject<ModelAttachments>(attachmentsJson.text).attachments;
                        foreach (var attachment in attachments)
                        {
                            if (attachment.id == 5)
                                processSlot("1", 1, attachment.bone);
                            else if (attachment.id == 6)
                                processSlot("1", 0, attachment.bone);
                            else if (attachment.id == 11)
                                processSlot("0", 0, attachment.bone);
                        }
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(creaturePrefab, creaturePath);
                UnityEngine.Object.DestroyImmediate(creaturePrefab);
            }
        }

        public static void SetupEquipMaterials(string prefabPath, GameObject instance, string modelTexture)
        {
            var equipJson = AssetConversionManager.ReadAssetJson(prefabPath);
            var metadata = JsonConvert.DeserializeObject<M2Utility.M2>(equipJson);

            if (metadata == null || metadata.textures.Count != 1)
                return;

            var texture = metadata.textures[0];
            texture.fileNameExternal = Path.GetFileName(modelTexture);
            metadata.textures[0] = texture;

            M2Utility.ProcessTextures(metadata.textures, Path.GetDirectoryName(prefabPath));
            var skinMaterials = MaterialUtility.GetSkinMaterials(metadata);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = skinMaterials[0].Item1;
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

        public static bool SetupTransitions(AnimatorState source, AnimatorStateTransition templateTransition)
        {
            templateTransition.name = "__tmp";
            AnimatorStateTransition tmp = null;
            var isDone = false;

            foreach (var transition in source.transitions)
            {
                if (transition.destinationState.name != templateTransition.destinationState.name)
                    continue;

                if (transition.name != templateTransition.name)
                {
                    while (transition.conditions.Length > 0)
                        transition.RemoveCondition(transition.conditions[^1]);

                    foreach (var condition in templateTransition.conditions)
                        transition.AddCondition(condition.mode, condition.threshold, condition.parameter);

                    transition.hasExitTime = templateTransition.hasExitTime;

                    isDone = true;
                }

                if (transition.name == templateTransition.name)
                    tmp = transition;
            }

            if (tmp != null)
                source.RemoveTransition(tmp);

            if (tmp == null && !isDone)
                source.AddTransition(templateTransition);

            return tmp != null && !isDone;
        }

        public static void SetupAnimations(AnimatorController controller)
        {
            var hasMissing = false;
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
            }

            List<AnimatorState> idleStates = new();
            List<AnimatorState> allStates = new();

            if (knownStates.TryGetValue("0_0", out var defaultIdle))
            {
                idleStates.Add(defaultIdle.state);
                allStates.Add(defaultIdle.state);

                for (var i = 1; i < 10; i++)
                {
                    if (!knownStates.TryGetValue($"0_{i}", out var altIdle))
                        continue;

                    idleStates.Add(altIdle.state);
                    allStates.Add(altIdle.state);

                    hasMissing = SetupTransitions(
                        defaultIdle.state,
                        new AnimatorStateTransition()
                        {
                            destinationState = altIdle.state,
                            hasExitTime = false,
                            conditions = new AnimatorCondition[2] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                                new () { parameter = "idleState", mode = AnimatorConditionMode.Equals, threshold = i },
                            }
                        }
                    ) || hasMissing;

                    hasMissing = SetupTransitions(
                        altIdle.state,
                        new AnimatorStateTransition()
                        {
                            destinationState = defaultIdle.state,
                            conditions = new AnimatorCondition[2] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                                new () { parameter = "idleState", mode = AnimatorConditionMode.Equals, threshold = 0 },
                            }
                        }
                    ) || hasMissing;
                }

                var hasVariations = false;
                foreach (var behaviour in defaultIdle.state.behaviours)
                    hasVariations = hasVariations || behaviour is IdleVariations;

                if (!hasVariations)
                {
                    var behaviour = defaultIdle.state.AddStateMachineBehaviour<IdleVariations>();
                    behaviour.idleVariations = idleStates.Count;
                }
            }

            if (knownStates.TryGetValue("4_0", out var walkState))
            {
                foreach (var state in allStates)
                {
                    hasMissing = SetupTransitions(
                        state,
                        new AnimatorStateTransition()
                        {
                            destinationState = walkState.state,
                            hasExitTime = false,
                            conditions = new AnimatorCondition[1] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 4 },
                            }
                        }
                    ) || hasMissing;
                }

                if (idleStates.Count > 0)
                {
                    hasMissing = SetupTransitions(
                        walkState.state,
                        new AnimatorStateTransition()
                        {
                            destinationState = idleStates[0],
                            hasExitTime = false,
                            conditions = new AnimatorCondition[1] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                            }
                        }
                    ) || hasMissing;
                }

                allStates.Add(walkState.state);
            }

            if (knownStates.TryGetValue("1_0", out var deathState))
            {
                foreach (var state in allStates)
                {
                    hasMissing = SetupTransitions(
                        state,
                        new AnimatorStateTransition()
                        {
                            destinationState = deathState.state,
                            hasExitTime = false,
                            conditions = new AnimatorCondition[1] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 1 },
                            }
                        }
                    ) || hasMissing;
                }
            }

            if (hasMissing)
                Debug.LogWarning($"{controller.name}: There're still transitions missing set up.");

            AssetDatabase.SaveAssets();
        }
    }
}