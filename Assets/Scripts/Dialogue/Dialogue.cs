using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace RPG.Dialogue
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue", order = 0)]
    public class Dialogue : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<DialogueNode> _nodes = new List<DialogueNode>();
        [SerializeField] private Vector2 _newNodeOffset = new Vector2(500, 0); 

        private Dictionary<string, DialogueNode> _nodeLookup = new Dictionary<string, DialogueNode>();

        private void OnValidate()
        {
            _nodeLookup.Clear();
            foreach(DialogueNode node in GetAllNodes())
            {
                _nodeLookup[node.name] = node;
            }
        }

        public IEnumerable<DialogueNode> GetAllNodes()
        {
            return _nodes;
        }

        public DialogueNode GetRootNode()
        {
            return _nodes[0];
        }

        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {
            foreach(string childID in parentNode.GetChildren())
            {
                if (_nodeLookup.ContainsKey(childID))
                {
                    yield return _nodeLookup[childID];
                }
            }
        }

#if UNITY_EDITOR
        public void CreateNode(DialogueNode parent)
        {
            DialogueNode newNode = MakeNode(parent);
            Undo.RegisterCreatedObjectUndo(newNode, "Create Dialogue Node");
            Undo.RecordObject(this, "Added Dialogue Node");
            AddNode(newNode);
        }

        public void DeleteNode(DialogueNode nodeToDelete)
        {
            Undo.RecordObject(this, "Deleted Dialogue Node");
            _nodes.Remove(nodeToDelete);          
            OnValidate();
            CleanDanglingNodes(nodeToDelete);
            Undo.DestroyObjectImmediate(nodeToDelete);
        }

        private DialogueNode MakeNode(DialogueNode parent)
        {
            DialogueNode newNode = CreateInstance<DialogueNode>();
            newNode.name = Guid.NewGuid().ToString();
            if (parent != null)
            {
                parent.AddChild(newNode.name);
                newNode.SetPlayerSpeaking(!parent.IsPlayerSpeaking());
                newNode.SetPosition(parent.GetRect().position + _newNodeOffset);
            }

            return newNode;
        }

        private void AddNode(DialogueNode newNode)
        {
            _nodes.Add(newNode);
            OnValidate();
        }

        private void CleanDanglingNodes(DialogueNode nodeToDelete)
        {
            foreach (DialogueNode node in GetAllNodes())
            {
                node.RemoveChild(nodeToDelete.name);
            }
        }
#endif

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (_nodes.Count == 0)
            {
                DialogueNode newNode = MakeNode(null);
                AddNode(newNode);
            }
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach(DialogueNode node in GetAllNodes())
                {
                    if(AssetDatabase.GetAssetPath(node) == "")
                    {
                        AssetDatabase.AddObjectToAsset(node, this);
                    }
                }
            }
#endif
        }

        public void OnAfterDeserialize()
        {
        }
    }
}


