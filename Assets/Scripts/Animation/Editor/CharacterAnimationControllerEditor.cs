using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Animation
{
    [CustomEditor(typeof(CharacterAnimationController))]
    public class CharacterAnimationControllerEditor : UnityEditor.Editor
    {
        private CharacterAnimationController controller;
        private Animator animator;
        private List<string> availableParameters = new List<string>();
        private List<string> boolParameters = new List<string>();
        
        private void OnEnable()
        {
            controller = (CharacterAnimationController)target;
            animator = controller.GetComponent<Animator>();
            RefreshParameterLists();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Character Animation Controller", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (animator == null)
            {
                EditorGUILayout.HelpBox("Animator component required!", MessageType.Error);
                return;
            }
            
            if (animator.runtimeAnimatorController == null)
            {
                EditorGUILayout.HelpBox("Runtime Animator Controller not assigned!", MessageType.Warning);
                return;
            }
            
            // Кнопка обновления параметров
            if (GUILayout.Button("Refresh Parameters from Animator"))
            {
                RefreshParameterLists();
            }
            
            EditorGUILayout.Space();
            
            // Стартовая анимация
            EditorGUILayout.LabelField("Start Animation Settings", EditorStyles.boldLabel);
            
            var setOnStartProperty = serializedObject.FindProperty("setStartParameterOnStart");
            EditorGUILayout.PropertyField(setOnStartProperty, new GUIContent("Set Parameter on Start"));
            
            if (setOnStartProperty.boolValue)
            {
                var startParameterProperty = serializedObject.FindProperty("startParameter");
                
                if (boolParameters.Count > 0)
                {
                    int currentIndex = Mathf.Max(0, boolParameters.IndexOf(startParameterProperty.stringValue));
                    int newIndex = EditorGUILayout.Popup("Start Parameter", currentIndex, boolParameters.ToArray());
                    
                    if (newIndex >= 0 && newIndex < boolParameters.Count)
                    {
                        startParameterProperty.stringValue = boolParameters[newIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No bool parameters found in Animator Controller", MessageType.Info);
                    EditorGUILayout.PropertyField(startParameterProperty, new GUIContent("Start Parameter (Manual)"));
                }
            }
            
            EditorGUILayout.Space();
            
            // Информация о доступных параметрах
            EditorGUILayout.LabelField("Available Parameters", EditorStyles.boldLabel);
            
            if (availableParameters.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                
                var groupedParams = GetGroupedParameters();
                
                foreach (var group in groupedParams)
                {
                    EditorGUILayout.LabelField($"{group.Key}:", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    
                    foreach (var param in group.Value)
                    {
                        EditorGUILayout.LabelField($"• {param}");
                    }
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(2);
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No parameters found in Animator Controller", MessageType.Info);
            }
            
            // Подсказки по использованию
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Usage Tips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "• Use SetBool(paramName, value) for boolean parameters\n" +
                "• Use SetInt(paramName, value) for integer parameters\n" +
                "• Use SetFloat(paramName, value) for float parameters\n" +
                "• Use SetTrigger(paramName) for trigger parameters\n" +
                "• All methods are available in UnityEvents", 
                MessageType.Info);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void RefreshParameterLists()
        {
            availableParameters.Clear();
            boolParameters.Clear();
            
            if (animator == null || animator.runtimeAnimatorController == null) return;
            
            var parameters = animator.parameters;
            
            foreach (var param in parameters)
            {
                availableParameters.Add($"{param.name} ({param.type})");
                
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    boolParameters.Add(param.name);
                }
            }
        }
        
        private Dictionary<string, List<string>> GetGroupedParameters()
        {
            var grouped = new Dictionary<string, List<string>>();
            
            if (animator == null || animator.runtimeAnimatorController == null) 
                return grouped;
            
            var parameters = animator.parameters;
            
            foreach (var param in parameters)
            {
                string type = param.type.ToString();
                
                if (!grouped.ContainsKey(type))
                {
                    grouped[type] = new List<string>();
                }
                
                grouped[type].Add(param.name);
            }
            
            return grouped;
        }
    }
}