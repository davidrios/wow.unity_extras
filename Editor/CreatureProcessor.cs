using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WowUnity;
using WoWUnityExtras.Database;

namespace WoWUnityExtras
{
    enum Race
    {
        Human = 1,
        Dwarf = 3,
        Scourge = 5
    }

    enum Sex
    {
        Male = 0,
        Female = 1
    }

    enum EquipSlot
    {
        Helm = 0,
        Shoulder = 1,
        OHWeapon = 102,
        Shield = 104
    }

    public class CreatureProcessor
    {
        static readonly Dictionary<(Race, Sex), Dictionary<(EquipSlot, int), Vector3>> EquipPositions = new() {
            {
                (Race.Human, Sex.Male), new() {
                    { (EquipSlot.OHWeapon, 0), new(0.00033f, -0.00035f, 0) },
                    { (EquipSlot.Shield, 0), new(0, 0.00199f, -0.0009f) }
                }
            },
            {
                (Race.Dwarf, Sex.Male), new() {
                    { (EquipSlot.Helm, 0), new(0.00054f, -0.0001577f, 0) }
                }
            },
            {
                (Race.Scourge, Sex.Male), new() {
                    { (EquipSlot.Shoulder, 0), new(-0.0004f, 0, 0) },
                    { (EquipSlot.Shoulder, 1), new(-0.00049f, 0, 0) }
                }
            }
        };

        public static void SetupCreatureModel(GameObject creatureModel, Texture2D[] creatureTextures)
        {
            var assetPath = AssetDatabase.GetAssetPath(creatureModel);
            if (assetPath == null)
            {
                Debug.LogWarning("invalid asset");
                return;
            }

            var assetDir = Path.GetDirectoryName(assetPath);
            var assetBaseName = Path.GetFileNameWithoutExtension(assetPath);

            var controllerPath = Path.Join(assetDir, $"{assetBaseName}.controller");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            var mainPrefabPath = Path.ChangeExtension(assetPath, ".prefab");
            var mainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainPrefabPath);
            if (mainPrefab == null)
            {
                var mainPrefabInstance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath)) as GameObject;
                mainPrefabInstance.AddComponent<Creature>();
                mainPrefabInstance.AddComponent<CreatureSounds>();

                var charController = mainPrefabInstance.GetComponent<CharacterController>();
                var center = charController.center;
                center.y = 1.06f;
                charController.center = center;

                var animator = mainPrefabInstance.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.runtimeAnimatorController = controller;

                mainPrefab = PrefabUtility.SaveAsPrefabAsset(mainPrefabInstance, mainPrefabPath);
                UnityEngine.Object.DestroyImmediate(mainPrefabInstance);
            }

            if (creatureTextures.Length == 0)
            {
                List<Texture2D> textures = new();
                foreach (var path in Directory.GetFiles(assetDir, $"{assetBaseName}skin*.png"))
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                        textures.Add(texture);
                }

                creatureTextures = textures.ToArray();
            }

            foreach (var texture in creatureTextures)
            {
                var texturePath = AssetDatabase.GetAssetPath(texture);
                MaterialUtility.GetBasicMaterial(texturePath, (uint)MaterialUtility.BlendModes.Opaque);
                MaterialUtility.GetBasicMaterial(texturePath, (uint)MaterialUtility.BlendModes.AlphaKey);

                var variantPath = Path.Join(assetDir, $"{assetBaseName}__{texture.name}.prefab");
                if (File.Exists(variantPath))
                    continue;

                var prefabInstance = PrefabUtility.InstantiatePrefab(mainPrefab) as GameObject;
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, variantPath);
                UnityEngine.Object.DestroyImmediate(prefabInstance);
            }
        }

        public static void CreateTextures(Texture2D[] textures)
        {
            foreach (var texture in textures)
            {
                var texturePath = AssetDatabase.GetAssetPath(texture);
                MaterialUtility.GetBasicMaterial(texturePath, (uint)MaterialUtility.BlendModes.Opaque);
                MaterialUtility.GetBasicMaterial(texturePath, (uint)MaterialUtility.BlendModes.AlphaKey);
            }
        }

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
                    prefabPath = Path.Join(
                        rootDir,
                        Path.GetDirectoryName(creatureDisplay.model.FileData),
                        $"{Path.GetFileNameWithoutExtension(creatureDisplay.model.FileData)}__{textureName}.prefab"
                    );
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
                        {
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[1..]}")?.gameObject.SetActive(false);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[1..]}.001")?.gameObject.SetActive(false);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[1..]}.002")?.gameObject.SetActive(false);
                        }
                        else
                        {
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[..^1]}1")?.gameObject.SetActive(false);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[..^1]}1.001")?.gameObject.SetActive(false);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset[..^1]}1.002")?.gameObject.SetActive(false);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset}")?.gameObject.SetActive(true);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset}.001")?.gameObject.SetActive(true);
                            creaturePrefab.transform.Find($"{prefixGeo}_{geoset}.002")?.gameObject.SetActive(true);
                        }
                    }

                    var bodyMaterial = MaterialUtility.GetBasicMaterial(Path.Join(rootDir, creatureDisplay.extra.BakeMaterialResourcesIDFile), (uint)MaterialUtility.BlendModes.Opaque);
                    var hairMaterial = MaterialUtility.GetBasicMaterial(Path.Join(rootDir, creatureDisplay.extra.HairTextureFile), (uint)MaterialUtility.BlendModes.AlphaKey);

                    for (var i = 0; i < creaturePrefab.transform.childCount; i++)
                    {
                        var child = creaturePrefab.transform.GetChild(i).gameObject;
                        if (!child.TryGetComponent<Renderer>(out var renderer))
                            continue;

                        if (renderer.sharedMaterial.name == "body" && bodyMaterial != null)
                            renderer.sharedMaterial = bodyMaterial;

                        if (renderer.sharedMaterial.name == "hair" && hairMaterial != null)
                            renderer.sharedMaterial = hairMaterial;
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

                        var equipSlot = (EquipSlot)int.Parse(slotKey);

                        if (creatureDisplay.extra != null && EquipPositions.TryGetValue(((Race)creatureDisplay.extra.DisplayRaceID, (Sex)creatureDisplay.extra.DisplaySexID), out var slotPositionMap))
                        {
                            if (slotPositionMap.TryGetValue((equipSlot, resourceIndex), out var localPosition))
                                equipInstance.transform.localPosition = localPosition;
                        }

                        if (equipInstance.TryGetComponent<LODGroup>(out var lodGroup))
                            UnityEngine.Object.DestroyImmediate(lodGroup);

                        equipInstance.isStatic = false;
                        foreach (var transform in equipInstance.GetComponentsInChildren<Transform>())
                            transform.gameObject.isStatic = false;

                        if (creaturePrefab.TryGetComponent<CreatureAnimation>(out var creatureAnimation))
                        {
                            creatureAnimation.rightHandClosed = creatureAnimation.rightHandClosed || equipSlot switch
                            {
                                EquipSlot.OHWeapon => true,
                                _ => false
                            };

                            creatureAnimation.leftHandClosed = creatureAnimation.leftHandClosed || equipSlot switch
                            {
                                EquipSlot.Shield => true,
                                _ => false
                            };
                        }

                        SetupEquipMaterials(equipPrefabPath, equipInstance, slotItem.displayInfo.ModelMaterialResourcesIDFiles[resourceIndex]);
                    }

                    var attachments = JsonConvert.DeserializeObject<ModelAttachments>(attachmentsJson.text).attachments;
                    foreach (var attachment in attachments)
                    {
                        if (attachment.id == 1)
                            processSlot("102", 0, attachment.bone);
                        else if (attachment.id == 2)
                            processSlot("104", 0, attachment.bone);
                        else if (attachment.id == 5)
                            processSlot("1", 1, attachment.bone);
                        else if (attachment.id == 6)
                            processSlot("1", 0, attachment.bone);
                        else if (attachment.id == 11)
                            processSlot("0", 0, attachment.bone);
                    }
                }

                if (creatureDisplay.model.sound != null)
                {
                    var creatureSounds = creaturePrefab.GetOrAddComponent<CreatureSounds>();
                    var soundKits = GetOrCreateSoundKits(jsonAsset);
                    if (soundKits != null)
                    {
                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundDeathID, out var deathSound))
                            creatureSounds.SetDeath(deathSound);

                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundWingFlapID, out var wingFlapSound))
                            creatureSounds.SetWingFlap(wingFlapSound);

                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundFidget[0], out var fidget1Sound))
                            creatureSounds.SetFidget1(fidget1Sound);

                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundFidget[1], out var fidget2Sound))
                            creatureSounds.SetFidget2(fidget2Sound);

                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundFidget[2], out var fidget3Sound))
                            creatureSounds.SetFidget3(fidget3Sound);

                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundFidget[3], out var fidget4Sound))
                            creatureSounds.SetFidget4(fidget4Sound);

                        if (soundKits.TryGetValue(creatureDisplay.model.sound.SoundFidget[4], out var fidget5Sound))
                            creatureSounds.SetFidget5(fidget5Sound);
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

        public static Dictionary<int, GameObject> GetOrCreateSoundKits(TextAsset jsonAsset)
        {
            var creatureData = JsonConvert.DeserializeObject<CreatureData>(jsonAsset.text);
            if (creatureData.displayInfo == null)
                throw new Exception("Invalid creature JSON");

            if (creatureData.soundKit == null)
                return null;

            var rootDir = Path.GetDirectoryName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(jsonAsset)));

            var soundKitPrefabs = new Dictionary<int, GameObject>();

            foreach (var (_, soundKit) in creatureData.soundKit)
            {
                if (soundKit.entries.Count == 0)
                    continue;

                var firstEntry = soundKit.entries[0];
                if (!File.Exists(Path.Join(rootDir, firstEntry.FileData)))
                    continue;

                var basePath = Path.Join(rootDir, Path.GetDirectoryName(firstEntry.FileData));

                string currentName = Path.GetFileNameWithoutExtension(firstEntry.FileData);

                foreach (var entry in soundKit.entries)
                {
                    var name = Path.GetFileNameWithoutExtension(entry.FileData);

                    var index = 0;
                    for (; index < currentName.Length && index < name.Length && currentName[index] == name[index]; index++) { }

                    currentName = name[..index];
                }

                GameObject soundKitPrefab;
                var path = Path.Join(basePath, currentName + ".prefab");
                if (File.Exists(path))
                {
                    soundKitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (soundKitPrefab != null)
                        soundKitPrefabs[soundKit.ID] = soundKitPrefab;

                    continue;
                }

                var soundKitObj = new GameObject(currentName);
                foreach (var entry in soundKit.entries)
                {
                    var audio = soundKitObj.AddComponent<AudioSource>();
                    audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Path.Join(rootDir, entry.FileData));
                    audio.volume = entry.Volume * soundKit.VolumeFloat;
                    audio.playOnAwake = false;
                    audio.spatialBlend = 1;
                    audio.rolloffMode = AudioRolloffMode.Custom;
                    var keys = new Keyframe[4]
                    {
                        new () { value = 1, time = soundKit.MinDistance / soundKit.DistanceCutoff, inTangent = -5.627249f, outTangent = -5.627249f },
                        new () { value = 0.4748f, time = 0.3555555f, outWeight = 0.389182f, inTangent = -1.583532f, outTangent = -1.583532f },
                        new () { value = 0.1702f, time = 0.64f, outWeight = 0.601159f, inTangent = -0.810969f, outTangent = -0.810969f },
                        new () { value = 0, time = 1, inTangent = -0.177848f, outTangent = -0.177848f },
                    };
                    var newCurve = new AnimationCurve(keys);
                    audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, newCurve);
                    audio.maxDistance = soundKit.DistanceCutoff;

                    soundKitObj.AddComponent<SoundKit>();
                }

                soundKitPrefab = PrefabUtility.SaveAsPrefabAsset(soundKitObj, path);
                AssetDatabase.SaveAssets();
                UnityEngine.Object.DestroyImmediate(soundKitObj);

                soundKitPrefabs[soundKit.ID] = soundKitPrefab;
            }

            return soundKitPrefabs;
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

        public static void PlaceCreatureSpawners(TextAsset creatureTableJson, GameObject mapArea, GameObject target, int mapId, TextAsset creatureDataJson, CreatureSpawnerSettings creatureSpawnerSettings, bool randomRotation = true)
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
                spawnerComponent.sharedSettings = creatureSpawnerSettings;

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

        public static void SetupTransitions(AnimatorState source, AnimatorStateTransition templateTransition)
        {
            var hasTransition = false;

            foreach (var transition in source.transitions)
            {
                hasTransition = hasTransition || transition.destinationState.name == templateTransition.destinationState.name;
            }

            if (!hasTransition)
            {
                var transition = source.AddTransition(templateTransition.destinationState);
                transition.hasExitTime = templateTransition.hasExitTime;
                transition.name = templateTransition.name;
                foreach (var condition in templateTransition.conditions)
                {
                    transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
                }
            }
        }

        static (string, string) GetIdVariation(string name)
        {
            var match = Regex.Match(name, @"^.+ID (\d+) variation (\d+)");
            if (!match.Success)
                return (null, null);

            var id = match.Groups[1].Value;
            var variation = match.Groups[2].Value;

            return id switch
            {
                "0" or "1" or "4" => (id, variation),
                _ => (null, null),
            };
        }

        public static void SetupAnimations(AnimatorController controller)
        {
            Dictionary<string, string> animationPaths = new();

            var assetPath = AssetDatabase.GetAssetPath(controller);
            if (assetPath != null)
            {
                foreach (var path in Directory.GetFiles(Path.GetDirectoryName(assetPath), $"{Path.GetFileNameWithoutExtension(assetPath).TrimEnd("_")}_*.anim"))
                {
                    var stateName = Path.GetFileNameWithoutExtension(path);
                    var nameLower = stateName.ToLower();
                    var (id, variation) = GetIdVariation(stateName);
                    if (id != null)
                    {
                        var key = $"{id}_{variation}";
                        animationPaths[key] = path;
                    }
                    else if (nameLower.Contains("handsclosed"))
                    {
                        if (nameLower.Contains("left"))
                            animationPaths["leftHand"] = path;
                        else
                            animationPaths["rightHand"] = path;
                    }
                    else if (nameLower.Contains("blink"))
                    {
                        animationPaths["blink"] = path;
                    }
                }
            }

            controller.parameters = new AnimatorControllerParameter[4] {
                new() { name = "state", type = AnimatorControllerParameterType.Int },
                new() { name = "idleState", type = AnimatorControllerParameterType.Int },
                new() { name = "leftHandClosed", type = AnimatorControllerParameterType.Bool },
                new() { name = "rightHandClosed", type = AnimatorControllerParameterType.Bool },
            };

            var hasLeftHand = false;
            var hasRightHand = false;
            var hasBlink = false;
            foreach (var layer in controller.layers)
            {
                hasLeftHand = hasLeftHand || layer.name == "leftHandClosed";
                hasRightHand = hasRightHand || layer.name == "rightHandClosed";
                hasBlink = hasBlink || layer.name == "blink";
            }

            if (!hasLeftHand)
            {
                controller.AddLayer("leftHandClosed");
                var layer = controller.layers.First(layer => layer.name == "leftHandClosed");
                layer.blendingMode = AnimatorLayerBlendingMode.Override;
                layer.defaultWeight = 1;
                layer.stateMachine.AddState("Empty");
                if (animationPaths.TryGetValue("leftHand", out var path))
                {
                    var newState = layer.stateMachine.AddState(Path.GetFileNameWithoutExtension(path));
                    newState.motion = AssetDatabase.LoadAssetAtPath<Motion>(path);
                    var trans = newState.AddTransition(layer.stateMachine.states[0].state);
                    trans.AddCondition(AnimatorConditionMode.IfNot, 0, "leftHandClosed");

                    var trans2 = layer.stateMachine.states[0].state.AddTransition(newState);
                    trans2.AddCondition(AnimatorConditionMode.If, 0, "leftHandClosed");
                }
            }

            if (!hasRightHand)
            {
                controller.AddLayer("rightHandClosed");
                var layer = controller.layers.First(layer => layer.name == "rightHandClosed");
                layer.defaultWeight = 1;
                layer.stateMachine.AddState("Empty");

                if (animationPaths.TryGetValue("rightHand", out var path))
                {
                    var newState = layer.stateMachine.AddState(Path.GetFileNameWithoutExtension(path));
                    newState.motion = AssetDatabase.LoadAssetAtPath<Motion>(path);
                    var trans = newState.AddTransition(layer.stateMachine.states[0].state);
                    trans.AddCondition(AnimatorConditionMode.IfNot, 0, "rightHandClosed");

                    var trans2 = layer.stateMachine.states[0].state.AddTransition(newState);
                    trans2.AddCondition(AnimatorConditionMode.If, 0, "rightHandClosed");
                }
            }

            if (!hasBlink)
            {
                controller.AddLayer("blink");
                var layer = controller.layers.First(layer => layer.name == "blink");
                layer.defaultWeight = 1;

                if (animationPaths.TryGetValue("blink", out var path))
                {
                    var newState = layer.stateMachine.AddState(Path.GetFileNameWithoutExtension(path));
                    newState.motion = AssetDatabase.LoadAssetAtPath<Motion>(path);

                    var empty = layer.stateMachine.AddState("Empty");
                    var trans = newState.AddTransition(empty);
                    trans.AddCondition(AnimatorConditionMode.Equals, 1, "state");
                }
            }

            var stateMachine = controller.layers[0].stateMachine;
            Dictionary<string, ChildAnimatorState> knownStates = new();
            foreach (var state in stateMachine.states)
            {
                var (id, variation) = GetIdVariation(state.state.name);
                if (id == null)
                    continue;

                knownStates[$"{id}_{variation}"] = state;
            }

            foreach (var (animType, path) in animationPaths)
            {
                var split = animType.Split("_");
                if (split.Length != 2)
                    continue;

                var (id, variation) = (split[0], split[1]);

                var key = $"{id}_{variation}";

                if (!knownStates.ContainsKey(key))
                {
                    var newState = stateMachine.AddState(Path.GetFileNameWithoutExtension(path));
                    newState.motion = AssetDatabase.LoadAssetAtPath<Motion>(path);
                    knownStates[$"{id}_{variation}"] = stateMachine.states[^1];
                    if (id == "0" && variation == "0")
                        stateMachine.defaultState = newState;
                }
            }

            List<AnimatorState> idleStates = new();
            List<(string, AnimatorState)> allStates = new();

            if (knownStates.TryGetValue("0_0", out var defaultIdle))
            {
                idleStates.Add(defaultIdle.state);
                allStates.Add(("idle", defaultIdle.state));

                for (var i = 1; i < 10; i++)
                {
                    if (!knownStates.TryGetValue($"0_{i}", out var altIdle))
                        continue;

                    idleStates.Add(altIdle.state);
                    allStates.Add(($"idle v{i}", altIdle.state));

                    SetupTransitions(
                        defaultIdle.state,
                        new AnimatorStateTransition()
                        {
                            name = $"idle > idle v{i}",
                            destinationState = altIdle.state,
                            hasExitTime = false,
                            conditions = new AnimatorCondition[2] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                                new () { parameter = "idleState", mode = AnimatorConditionMode.Equals, threshold = i },
                            }
                        }
                    );

                    SetupTransitions(
                        altIdle.state,
                        new AnimatorStateTransition()
                        {
                            name = $"idle v{i} > idle",
                            destinationState = defaultIdle.state,
                            conditions = new AnimatorCondition[2] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                                new () { parameter = "idleState", mode = AnimatorConditionMode.Equals, threshold = 0 },
                            }
                        }
                    );
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
                foreach (var (name, state) in allStates)
                {
                    SetupTransitions(
                        state,
                        new AnimatorStateTransition()
                        {
                            name = $"{name} > walk",
                            destinationState = walkState.state,
                            hasExitTime = false,
                            conditions = new AnimatorCondition[1] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 4 },
                            }
                        }
                    );
                }

                if (idleStates.Count > 0)
                {
                    SetupTransitions(
                        walkState.state,
                        new AnimatorStateTransition()
                        {
                            name = "walk > idle",
                            destinationState = idleStates[0],
                            hasExitTime = false,
                            conditions = new AnimatorCondition[1] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 0 },
                            }
                        }
                    );
                }

                allStates.Add(("walk", walkState.state));
            }

            if (knownStates.TryGetValue("1_0", out var deathState))
            {
                foreach (var (name, state) in allStates)
                {
                    SetupTransitions(
                        state,
                        new AnimatorStateTransition()
                        {
                            name = $"{name} > death",
                            destinationState = deathState.state,
                            hasExitTime = false,
                            conditions = new AnimatorCondition[1] {
                                new () { parameter = "state", mode = AnimatorConditionMode.Equals, threshold = 1 },
                            }
                        }
                    );
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}