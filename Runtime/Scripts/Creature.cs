using UnityEngine;

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
    public class Creature : MonoBehaviour
    {
        private static readonly float gravity = -9.81f;
        private static readonly float smoothTurnTime = 0.05f;
        private static readonly float baseSpeed = 2;

        [SerializeField]
        private float wanderRange = 0;
        [SerializeField]
        private float walkSpeed = 1;

        private CharacterController characterController;
        private CreatureAnimation creatureAnimation;
        private CreatureState creatureState;
        private float downVelocity;
        private Vector3 direction;
        private float turnVelocity;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            creatureAnimation = GetComponent<CreatureAnimation>();
        }

        void Update()
        {
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

        public void Move(Vector2 direction)
        {
            if (creatureState == CreatureState.Dead)
                return;

            this.direction = new Vector3(direction.x, 0, direction.y);
        }

        public void Die()
        {
            if (creatureState == CreatureState.Dead)
                return;

            creatureState = CreatureState.Dead;
            creatureAnimation.Die();
        }
    }
}