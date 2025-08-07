using System.Collections.Generic;
using System.Linq;
using GameDevTV.Utils;
using UnityEditor;
using UnityEngine;

namespace RPG.Dialogue
{
    public class DialogueNode : ScriptableObject
    {
        [SerializeField]
        bool isPlayerSpeaking = false;
        [SerializeField][TextArea]
        string text;
        [SerializeField]
        List<string> children = new List<string>();
        [SerializeField]
        Rect rect = new Rect(0, 0, 200, 100);
        [SerializeField]
        List<string> onEnterActions = new List<string>();
        [SerializeField]
        List<string> onExitActions = new List<string>();
        [SerializeField]
        Condition condition;

        public Rect GetRect()
        {
            return rect;
        }

        public string GetText()
        {
            return text;
        }

        public List<string> GetChildren()
        {
            return children;
        }

        public bool IsPlayerSpeaking()
        {
            return isPlayerSpeaking;
        }

        public IEnumerable<string> GetOnEnterActions()
        {
            return onEnterActions ?? new List<string>();
        }

        public IEnumerable<string> GetOnExitActions()
        {
            return onExitActions ?? new List<string>();
        }

        public void AddOnEnterAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            if (onEnterActions == null) onEnterActions = new List<string>();
            
            if (!onEnterActions.Contains(action))
            {
                onEnterActions.Add(action);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void AddOnExitAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            if (onExitActions == null) onExitActions = new List<string>();
            
            if (!onExitActions.Contains(action))
            {
                onExitActions.Add(action);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void RemoveOnEnterAction(string action)
        {
            if (onEnterActions == null) return;
            onEnterActions.Remove(action);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveOnExitAction(string action)
        {
            if (onExitActions == null) return;
            onExitActions.Remove(action);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public bool CheckCondition(IEnumerable<IPredicateEvaluator> evaluators)
        {
            string nodeName = this.name; // Или другой идентификатор узла
            Debug.Log($"Node '{nodeName}': Starting CheckCondition.");
            // Логируйте условия, которые определены на этом узле
            // Debug.Log($"Node '{nodeName}': Conditions to check: [ваши условия]");
		    // Логируйте условия, которые определены на этом узле
		    // Debug.Log($"Node '{nodeName}': Conditions to check: [ваши условия]");

		    bool overallResult = condition.Check(evaluators);

    		// Логируйте финальный результат для этого узла
			    Debug.Log($"Node '{nodeName}': CheckCondition evaluated to: {overallResult}");
		    return overallResult;
        }

#if UNITY_EDITOR
        public void SetPosition(Vector2 newPosition)
        {
            Undo.RecordObject(this, "Move Dialogue Node");
            rect.position = newPosition;
            EditorUtility.SetDirty(this);
        }

        public void SetText(string newText)
        {
            if (newText != text)
            {
                Undo.RecordObject(this, "Update Dialogue Text");
                text = newText;
                EditorUtility.SetDirty(this);
            }
        }

        public void AddChild(string childID)
        {
            Undo.RecordObject(this, "Add Dialogue Link");
            children.Add(childID);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChild(string childID)
        {
            Undo.RecordObject(this, "Remove Dialogue Link");
            children.Remove(childID);
            EditorUtility.SetDirty(this);
        }

        public void SetPlayerSpeaking(bool newIsPlayerSpeaking)
        {
            Undo.RecordObject(this, "Change Dialogue Speaker");
            isPlayerSpeaking = newIsPlayerSpeaking;
            EditorUtility.SetDirty(this);
        }      
#endif
    }
}