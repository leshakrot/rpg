using UnityEngine;
using UnityEditor;
using RPG.Combat;

namespace RPG.Combat.Editor
{
    [CustomPropertyDrawer(typeof(ComponentToAdd))]
    public class ComponentToAddDrawer : PropertyDrawer
    {
        private const float SPACING = 2f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            SerializedProperty templateProp = property.FindPropertyRelative("componentTemplate");
            SerializedProperty eventBindingProp = property.FindPropertyRelative("eventBinding");
            
            float currentY = position.y;
            
            Rect foldoutRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            currentY += EditorGUIUtility.singleLineHeight + SPACING;
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                Rect templateRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(templateRect, templateProp, new GUIContent("Component Template", "Drag a component from any GameObject here as template"));
                currentY += EditorGUIUtility.singleLineHeight + SPACING;
                
                if (templateProp.objectReferenceValue != null)
                {
                    MonoBehaviour template = templateProp.objectReferenceValue as MonoBehaviour;
                    if (template != null)
                    {
                        currentY += SPACING * 2;
                        
                        Rect labelRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                        EditorGUI.LabelField(labelRect, "Template Component Settings:", EditorStyles.boldLabel);
                        currentY += EditorGUIUtility.singleLineHeight + SPACING;
                        
                        SerializedObject templateObject = new SerializedObject(template);
                        SerializedProperty iterator = templateObject.GetIterator();
                        
                        bool enterChildren = true;
                        while (iterator.NextVisible(enterChildren))
                        {
                            enterChildren = false;
                            
                            if (iterator.propertyPath == "m_Script") continue;
                            
                            float propertyHeight = EditorGUI.GetPropertyHeight(iterator, true);
                            Rect propertyRect = new Rect(position.x, currentY, position.width, propertyHeight);
                            
                            EditorGUI.BeginChangeCheck();
                            EditorGUI.PropertyField(propertyRect, iterator, true);
                            if (EditorGUI.EndChangeCheck())
                            {
                                templateObject.ApplyModifiedProperties();
                            }
                            
                            currentY += propertyHeight + SPACING;
                        }
                        
                        currentY += SPACING * 2;
                    }
                }
                
                Rect eventBindingRect = new Rect(position.x, currentY, position.width, 
                    EditorGUI.GetPropertyHeight(eventBindingProp, true));
                EditorGUI.PropertyField(eventBindingRect, eventBindingProp, true);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            
            float totalHeight = EditorGUIUtility.singleLineHeight + SPACING;
            
            SerializedProperty templateProp = property.FindPropertyRelative("componentTemplate");
            SerializedProperty eventBindingProp = property.FindPropertyRelative("eventBinding");
            
            totalHeight += EditorGUIUtility.singleLineHeight + SPACING;
            
            if (templateProp.objectReferenceValue != null)
            {
                MonoBehaviour template = templateProp.objectReferenceValue as MonoBehaviour;
                if (template != null)
                {
                    totalHeight += SPACING * 2;
                    totalHeight += EditorGUIUtility.singleLineHeight + SPACING;
                    
                    SerializedObject templateObject = new SerializedObject(template);
                    SerializedProperty iterator = templateObject.GetIterator();
                    
                    bool enterChildren = true;
                    while (iterator.NextVisible(enterChildren))
                    {
                        enterChildren = false;
                        
                        if (iterator.propertyPath == "m_Script") continue;
                        
                        totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + SPACING;
                    }
                    
                    totalHeight += SPACING * 2;
                }
            }
            
            totalHeight += EditorGUI.GetPropertyHeight(eventBindingProp, true);
            
            return totalHeight;
        }
    }
}
