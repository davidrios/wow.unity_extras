using UnityEngine;

namespace WoWUnityExtras
{
    public enum CreatureAnimationState
    {
        Idle = 0,
        Death = 1,
        Walk = 4
    }

    [RequireComponent(typeof(Animator))]
    public class CreatureAnimation : MonoBehaviour
    {
        public bool leftHandClosed = false;
        public bool rightHandClosed = false;

        private Animator animator;
        private CreatureAnimationState currentState;

        void Start()
        {
            animator = GetComponent<Animator>();
            if (leftHandClosed || rightHandClosed)
            {
                animator.SetBool("leftHandClosed", leftHandClosed);
                animator.SetBool("rightHandClosed", rightHandClosed);
            }
        }

        private void SetState(CreatureAnimationState state)
        {
            currentState = state;
            animator.SetInteger("state", (int)currentState);
        }

        public void Idle()
        {
            SetState(CreatureAnimationState.Idle);
        }

        public void Walk()
        {
            SetState(CreatureAnimationState.Walk);
        }

        public void Death()
        {
            SetState(CreatureAnimationState.Death);
        }
    }
}