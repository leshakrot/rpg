using UnityEngine;
using System.Collections.Generic;

namespace RPG.Animation
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [Header("Start Animation")]
        [SerializeField] private string startParameter = "";
        [SerializeField] private bool setStartParameterOnStart = true;
        
        private Animator animator;
        private RuntimeAnimatorController runtimeController;
        
        // Кэш для параметров аниматора
        private Dictionary<string, int> parameterHashes = new Dictionary<string, int>();
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator != null)
            {
                runtimeController = animator.runtimeAnimatorController;
                CacheParameterHashes();
            }
        }
        
        private void Start()
        {
            if (setStartParameterOnStart && !string.IsNullOrEmpty(startParameter))
            {
                SetBool(startParameter, true);
            }
        }
        
        private void CacheParameterHashes()
        {
            if (animator == null) return;
            
            parameterHashes.Clear();
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var param = animator.parameters[i];
                parameterHashes[param.name] = param.nameHash;
            }
        }
        
        // Публичные методы для UnityEvents
        public void SetBool(string parameterName, bool value)
        {
            if (animator != null && parameterHashes.ContainsKey(parameterName))
            {
                animator.SetBool(parameterHashes[parameterName], value);
            }
        }
        
        public void SetBoolTrue(string parameterName)
        {
            SetBool(parameterName, true);
        }
        
        public void SetBoolFalse(string parameterName)
        {
            SetBool(parameterName, false);
        }
        
        public void SetInt(string parameterName, int value)
        {
            if (animator != null && parameterHashes.ContainsKey(parameterName))
            {
                animator.SetInteger(parameterHashes[parameterName], value);
            }
        }
        
        public void SetFloat(string parameterName, float value)
        {
            if (animator != null && parameterHashes.ContainsKey(parameterName))
            {
                animator.SetFloat(parameterHashes[parameterName], value);
            }
        }
        
        public void SetTrigger(string parameterName)
        {
            if (animator != null && parameterHashes.ContainsKey(parameterName))
            {
                animator.SetTrigger(parameterHashes[parameterName]);
            }
        }
        
        public void ResetTrigger(string parameterName)
        {
            if (animator != null && parameterHashes.ContainsKey(parameterName))
            {
                animator.ResetTrigger(parameterHashes[parameterName]);
            }
        }
        
        // Методы для работы с несколькими параметрами
        public void ResetAllBoolParameters()
        {
            if (animator == null) return;
            
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var param = animator.parameters[i];
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(param.nameHash, false);
                }
            }
        }
        
        public void SetMultipleBools(string[] parameterNames, bool value)
        {
            foreach (string paramName in parameterNames)
            {
                SetBool(paramName, value);
            }
        }
        
        // Утилиты
        public bool HasParameter(string parameterName)
        {
            return parameterHashes.ContainsKey(parameterName);
        }
        
        public void RefreshParameterCache()
        {
            CacheParameterHashes();
        }
    }
}