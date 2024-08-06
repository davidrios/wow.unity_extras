using UnityEngine;

namespace WoWUnityExtras
{
    public enum CreatureAnimationState
    {
        Idle = 0,
        Walk = 1,
        Die = 2
    }

    [RequireComponent(typeof(Animator))]
    public class CreatureAnimation : MonoBehaviour
    {
        private Animator animator;
        private CreatureAnimationState currentState;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        void Update()
        {

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

        public void Die()
        {
            SetState(CreatureAnimationState.Die);
        }
    }
}