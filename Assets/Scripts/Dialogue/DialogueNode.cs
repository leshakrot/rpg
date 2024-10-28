using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace RPG.Dialogue
{
    public class DialogueNode : ScriptableObject
    {
        [SerializeField] private bool _isPlayerSpeaking = false;
        [SerializeField] private string _text;
        [SerializeField] private List<string> _children = new List<string>();
        [SerializeField] private Rect _rect = new Rect(0, 0, 200, 100);

        public Rect GetRect()
        {
            return _rect;
        }

        public string GetText()
        {
            return _text;
        }

        public List<string> GetChildren()
        {
            return _children;
        }

        public bool IsPlayerSpeaking()
        {
            return _isPlayerSpeaking;
        }

#if UNITY_EDITOR
        public void SetPosition(Vector2 newPosition)
        {
            Undo.RecordObject(this, "Move Dialogue Node");
            _rect.position = newPosition;
            EditorUtility.SetDirty(this);
        }

        public void SetText(string newText)
        {
            if(newText != _text)
            {
                Undo.RecordObject(this, "Update Dialogue Text");
                _text = newText;
                EditorUtility.SetDirty(this);
            }
        }

        public void AddChild(string childID)
        {
            Undo.RecordObject(this, "Add Dialogue Link");
            _children.Add(childID);
            EditorUtility.SetDirty(this);
        }

        public void RemoveChild(string childID)
        {
            Undo.RecordObject(this, "Remove Dialogue Link");
            _children.Add(childID);
            EditorUtility.SetDirty(this);
        }

        public void SetPlayerSpeaking(bool newIsPlayerSpeaking)
        {
            Undo.RecordObject(this, "Change Dialogue Speaker");
            _isPlayerSpeaking = newIsPlayerSpeaking;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}

