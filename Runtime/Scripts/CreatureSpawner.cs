using System.Collections.Generic;
using UnityEngine;

namespace WoWUnityExtras
{
    class CreatureSpawnerManager : MonoBehaviour
    {
        private float timeCounter = 0;
        private readonly HashSet<CreatureSpawner> spawners = new();
        private readonly Dictionary<string, GameObject> players = new();
        private readonly object spawnersLock = new();

        public void AddSpawner(CreatureSpawner spawner)
        {
            lock (spawnersLock)
            {
                spawners.Add(spawner);
            }
        }

        public void RemoveSpawner(CreatureSpawner spawner)
        {
            lock (spawnersLock)
            {
                spawners.Remove(spawner);
            }
        }

        private void Update()
        {
            timeCounter += Time.deltaTime;
            if (timeCounter >= 1)
            {
                timeCounter = 0;

                foreach (var spawner in spawners)
                {
                    if (!players.TryGetValue(spawner.sharedSettings.playerTag, out var player))
                    {
                        var objects = GameObject.FindGameObjectsWithTag(spawner.sharedSettings.playerTag);
                        if (objects.Length == 0)
                            return;

                        player = objects[0];
                        players.Add(spawner.sharedSettings.playerTag, player);
                    }

                    spawner.UpdateSpawner(player.transform);
                }
            }
        }
    }

    public class CreatureSpawner : MonoBehaviour
    {
        private static CreatureSpawnerManager manager;
        private static readonly object managerLock = new();

        public static void SetupManager()
        {
            lock (managerLock)
            {
                if (manager == null)
                {
                    var newgo = new GameObject("CreatureSpawnerManager");
                    manager = newgo.AddComponent<CreatureSpawnerManager>();
                }
            }
        }

        public CreatureSpawnerSettings sharedSettings;
        public float spawnTime = 10;

        private readonly List<GameObject> prefabs = new();
        private (GameObject go, Creature creature) alive;
        private readonly Queue<(GameObject go, float deathTime)> dead = new();
        private float timeOfDeath = -0xffffff;

        void Start()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.TryGetComponent<Creature>(out _))
                {
                    child.SetActive(false);
                    prefabs.Add(child);
                }
            }

            if (prefabs.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            if (sharedSettings == null)
            {
                Debug.LogWarning($"Spawner {name} has no globalSettings", this);
                return;
            }

            SetupManager();
            manager.AddSpawner(this);
        }

        private void OnDestroy()
        {
            if (manager != null)
                manager.RemoveSpawner(this);
        }

        public void UpdateSpawner(Transform playerTransform)
        {
            if (prefabs.Count == 0)
                return;

            if (dead.TryPeek(out var deadInstance))
            {
                if (Time.fixedTime - deadInstance.deathTime > sharedSettings.despawnTime)
                {
                    Destroy(deadInstance.go);
                    dead.Dequeue();
                }
            }

            var inRange = Vector3.Distance(transform.position, playerTransform.position) < sharedSettings.spawnDistance;

            if (inRange && alive.go == null && Time.realtimeSinceStartup - timeOfDeath > spawnTime)
            {
                var aliveI = Instantiate(prefabs[Random.Range(0, prefabs.Count)], transform);
                alive = (aliveI, aliveI.GetComponent<Creature>());
            }

            if (alive.creature != null && alive.creature.CreatureState == CreatureState.Dead)
            {
                dead.Enqueue((alive.go, Time.fixedTime));
                alive = (null, null);
                timeOfDeath = Time.realtimeSinceStartup;
            }

            if (alive.go != null)
                alive.go.SetActive(inRange);
        }
    }
}