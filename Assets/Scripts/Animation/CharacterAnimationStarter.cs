using UnityEngine;

namespace RPG.Animation
{
    public class CharacterAnimationStarter : MonoBehaviour
    {
        [Header("Start Animation Settings")]
        [SerializeField] private StartAnimationType startType = StartAnimationType.Idle;
        [SerializeField] private bool setOnStart = true;
        
        private Animator animator;
        
        public enum StartAnimationType
        {
            Idle,
            Sitting,
            Sleeping,
            Working,
            Talking,
            Custom
        }
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
        }
        
        private void Start()
        {
            if (setOnStart && animator != null)
            {
                SetStartAnimation();
            }
        }
        
        public void SetStartAnimation()
        {
            if (animator == null) return;
            
            // Сбрасываем все состояния
            ResetAllStates();
            
            // Устанавливаем нужное состояние
            switch (startType)
            {
                case StartAnimationType.Sitting:
                    animator.SetBool("isSitting", true);
                    break;
                case StartAnimationType.Sleeping:
                    animator.SetBool("isSleeping", true);
                    break;
                case StartAnimationType.Working:
                    animator.SetBool("isWorking", true);
                    break;
                case StartAnimationType.Talking:
                    animator.SetBool("isTalking", true);
                    break;
                case StartAnimationType.Idle:
                default:
                    // Idle - состояние по умолчанию, ничего не устанавливаем
                    break;
            }
        }
        
        public void SetStartAnimation(StartAnimationType type)
        {
            startType = type;
            SetStartAnimation();
        }
        
        private void ResetAllStates()
        {
            animator.SetBool("isSitting", false);
            animator.SetBool("isSleeping", false);
            animator.SetBool("isWorking", false);
            animator.SetBool("isTalking", false);
        }
        
        public void SwitchToIdle()
        {
            ResetAllStates();
        }
    }
}