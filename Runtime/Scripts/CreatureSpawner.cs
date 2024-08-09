using System.Collections.Generic;
using UnityEngine;

namespace WoWUnityExtras
{
    public class CreatureSpawner : MonoBehaviour
    {
        private readonly float DespawnTime = 30;

        public float spawnTime = 10;

        private readonly List<GameObject> prefabs = new();
        private (GameObject go, Creature creature) alive;
        private readonly Queue<(GameObject go, float deathTime)> dead = new();
        private float timeSinceDeath = 0xffffff;

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
        }

        void Update()
        {
            if (prefabs.Count == 0)
                return;

            if (dead.TryPeek(out var deadInstance))
            {
                if (Time.fixedTime - deadInstance.deathTime > DespawnTime)
                {
                    Destroy(deadInstance.go);
                    dead.Dequeue();
                }
            }

            timeSinceDeath += Time.deltaTime;

            if (alive.go == null && timeSinceDeath > spawnTime)
            {
                var aliveI = Instantiate(prefabs[Random.Range(0, prefabs.Count)], transform);
                aliveI.SetActive(true);
                alive = (aliveI, aliveI.GetComponent<Creature>());
            }

            if (alive.creature != null && alive.creature.CreatureState == CreatureState.Dead)
            {
                dead.Enqueue((alive.go, Time.fixedTime));
                alive = (null, null);
                timeSinceDeath = 0;
            }
        }
    }
}