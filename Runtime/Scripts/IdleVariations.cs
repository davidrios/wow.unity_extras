using UnityEngine;

namespace WoWUnityExtras
{
    public class IdleVariations : StateMachineBehaviour
    {
        private static readonly int stateParam = Animator.StringToHash("state");
        private static readonly int idleStateParam = Animator.StringToHash("idleState");

        public int idleVariations = 1;
        public IdleVariationsSettings settings;

        private float lastTime = 0;

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            lastTime = -(settings?.checkInterval ?? 0.1f); // force starting with a check
        }

        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var chance = settings?.variationChance ?? 0.1f;

            if (idleVariations > 1 && chance > 0 && animator.GetInteger(stateParam) == 0)
            {
                if (stateInfo.normalizedTime - lastTime >= (settings?.checkInterval ?? 0.1f))
                {
                    lastTime = stateInfo.normalizedTime;

                    if (Random.value < chance)
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