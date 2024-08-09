using UnityEngine;

namespace WoWUnityExtras
{
    public class IdleVariations : StateMachineBehaviour
    {
        private static readonly int stateParam = Animator.StringToHash("state");
        private static readonly int idleStateParam = Animator.StringToHash("idleState");

        [SerializeField]
        public int idleVariations = 1;
        [SerializeField]
        private float idleVariationChance = 0.1f;
        [SerializeField]
        private float checkInterval = 0.5f;

        private float lastTime = 0;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            lastTime = 0;
        }

        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (idleVariations > 1 && idleVariationChance > 0 && animator.GetInteger(stateParam) == 0)
            {
                if (stateInfo.normalizedTime - lastTime >= checkInterval)
                {
                    lastTime = stateInfo.normalizedTime;

                    if (Random.value < idleVariationChance)
                        animator.SetInteger(idleStateParam, Random.Range(1, idleVariations));
                }
            }
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetInteger(idleStateParam, 0);
        }
    }
}