using UnityEngine;

namespace WoWUnityExtras
{
    [RequireComponent(typeof(CharacterController))]
    public class Creature : MonoBehaviour
    {
        private CharacterController characterController;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
        }

        void Update()
        {

        }
    }
}