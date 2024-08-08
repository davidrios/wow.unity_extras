using UnityEngine;
using UnityEngine.AI;

namespace WoWUnityExtras
{
    public enum CreatureState
    {
        Idle = 0,
        Moving = 1,
        Dead = 2,
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CreatureAnimation))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Creature : MonoBehaviour
    {
        private static readonly float gravity = -9.81f;
        private static readonly float smoothTurnTime = 0.05f;
        private static readonly float baseSpeed = 2;

        public string creatureName;

        [SerializeField]
        private float walkSpeed = 1;

        public float wanderRange = 0;
        public float wanderMinDistance = 2;
        [SerializeField]
        private float wanderNowaitChance = 0.2f;
        [SerializeField]
        private float wanderMinWait = 5;
        [SerializeField]
        private float wanderMaxWait = 5;

        private CharacterController characterController;
        private CreatureAnimation creatureAnimation;
        private CreatureState creatureState;
        public CreatureState CreatureState => creatureState;
        private float downVelocity;
        private Vector3 direction;
        private float turnVelocity;

        private NavMeshAgent navMeshAgent;
        private bool isWandering;
        private Vector3 startPosition;
        private float wanderWait;
        private float lastWanderSeconds;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            creatureAnimation = GetComponent<CreatureAnimation>();

            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.angularSpeed = 0;
            navMeshAgent.speed = 1;
            navMeshAgent.autoBraking = false;
            navMeshAgent.isStopped = true;

            if (navMeshAgent.stoppingDistance == 0)
                navMeshAgent.stoppingDistance = 0.5f;

            startPosition = gameObject.transform.position;
        }

        void Update()
        {
            if (navMeshAgent.isActiveAndEnabled)
                Wander();

            ApplyGravity();
            ApplyMovement();
            ApplyRotation();
        }

        void ApplyGravity()
        {
            if (characterController.isGrounded)
                downVelocity = -1;
            else
                downVelocity += gravity * Time.deltaTime;

            direction.y = downVelocity;
        }

        void ApplyMovement()
        {
            if (creatureState != CreatureState.Dead)
            {
                if (direction.x == 0 && direction.z == 0 && direction.y == -1)
                {
                    if (creatureState == CreatureState.Moving)
                    {
                        creatureState = CreatureState.Idle;
                        creatureAnimation.Idle();
                    }
                }
                else
                {
                    if (creatureState == CreatureState.Idle)
                    {
                        creatureState = CreatureState.Moving;
                        creatureAnimation.Walk();
                    }
                }
            }

            characterController.Move(Time.deltaTime * walkSpeed * baseSpeed * direction);
        }

        void ApplyRotation()
        {
            if (creatureState == CreatureState.Dead)
                return;

            if (direction.x == 0 && direction.z == 0 && direction.y == -1)
                return;

            var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, smoothTurnTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        void Wander()
        {
            if (wanderRange == 0 || !navMeshAgent.isOnNavMesh)
            {
                if (isWandering)
                    StopWandering();

                return;
            }

            if (creatureState == CreatureState.Idle && !isWandering)
            {
                lastWanderSeconds += Time.deltaTime;
                if (lastWanderSeconds > wanderWait)
                {
                    var point = new Vector2(startPosition.x, startPosition.z) + (Random.insideUnitCircle * Random.Range(0, wanderRange));
                    if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), point) > wanderMinDistance)
                    {
                        var rayStart = new Vector3(
                            point.x,
                            startPosition.y + 100,
                            point.y
                        );

                        if (Physics.Raycast(rayStart, Vector3.down, out var hit, 200))
                        {
                            navMeshAgent.SetDestination(hit.point);
                            navMeshAgent.isStopped = false;
                            isWandering = true;

                            if (Random.value < wanderNowaitChance)
                                wanderWait = 0;
                            else
                                wanderWait = Random.Range(wanderMinWait, wanderMaxWait);
                        }
                    }
                }
            }

            if (isWandering)
            {
                if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                    InternalMove(walkSpeed * new Vector2(navMeshAgent.desiredVelocity.x, navMeshAgent.desiredVelocity.z));
                else
                    StopWandering();
            }
        }

        void StopWandering()
        {
            if (navMeshAgent.isActiveAndEnabled)
                navMeshAgent.isStopped = true;

            isWandering = false;
            lastWanderSeconds = 0;
            InternalMove(Vector2.zero);
        }

        private void InternalMove(Vector2 direction)
        {
            if (creatureState == CreatureState.Dead)
                return;

            this.direction = new Vector3(direction.x, 0, direction.y);
        }

        public void Move(Vector2 direction)
        {
            isWandering = false;
            InternalMove(direction);
        }

        public void Die()
        {
            if (creatureState == CreatureState.Dead)
                return;

            creatureState = CreatureState.Dead;
            StopWandering();
            direction = Vector3.zero;
            creatureAnimation.Death();
        }
    }
}