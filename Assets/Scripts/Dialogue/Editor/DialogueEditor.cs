using Codice.Client.Common.TreeGrouper;
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.MPE;
using UnityEngine;

namespace RPG.Dialogue.Editor
{
    public class DialogueEditor : EditorWindow
    {
        private Dialogue _selectedDialogue = null;
        [NonSerialized]
        private GUIStyle _nodeStyle;
        [NonSerialized]
        private GUIStyle _playerNodeStyle;
        [NonSerialized]
        private DialogueNode _draggingNode = null;
        [NonSerialized]
        private Vector2 _draggingOffset;
        [NonSerialized]
        private DialogueNode _creatingNode = null;
        [NonSerialized]
        private DialogueNode _deletingNode = null;
        [NonSerialized]
        private DialogueNode _linkingparentNode = null;
        private Vector2 _scrollPosition;
        [NonSerialized]
        private bool _draggingCanvas = false;
        [NonSerialized]
        private Vector2 _draggingCanvasOffset;

        private const float _canvasSize = 4000;
        private const float _backgroundSize = 50f;

        [MenuItem("Window/Dialogue Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogueEditor), false, "Dialogue Editor");
        }

        [OnOpenAsset(1)]
        public static bool OpenDialogue(int instanceID, int line)
        {
            Dialogue dialogue = EditorUtility.InstanceIDToObject(instanceID) as Dialogue;
            if (dialogue != null)
            {
                ShowEditorWindow();
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;

            _nodeStyle = new GUIStyle();
            _nodeStyle.normal.background = EditorGUIUtility.Load("node2") as Texture2D;
            _nodeStyle.normal.textColor = Color.white;
            _nodeStyle.padding = new RectOffset(20, 20, 20, 20);
            _nodeStyle.border = new RectOffset(12, 12, 12, 12);

            _playerNodeStyle = new GUIStyle();
            _playerNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
            _playerNodeStyle.normal.textColor = Color.white;
            _playerNodeStyle.padding = new RectOffset(20, 20, 20, 20);
            _playerNodeStyle.border = new RectOffset(12, 12, 12, 12);
        }

        private void OnSelectionChanged()
        {
            Dialogue newDialogue = Selection.activeObject as Dialogue;
            if (newDialogue != null)
            {
                _selectedDialogue = newDialogue;
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (_selectedDialogue == null)
            {
                EditorGUILayout.LabelField("No Dialogue Selected");
            }
            else
            {
                ProcessEvent();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                Rect canvas = GUILayoutUtility.GetRect(_canvasSize, _canvasSize);
                Texture2D backgroundTexture = Resources.Load("background") as Texture2D;
                Rect textureCoords = new Rect(0, 0, _canvasSize/_backgroundSize, _canvasSize / _backgroundSize);
                GUI.DrawTextureWithTexCoords(canvas, backgroundTexture, textureCoords);

                foreach (DialogueNode node in _selectedDialogue.GetAllNodes())
                {
                    DrawConnections(node);
                }
                foreach (DialogueNode node in _selectedDialogue.GetAllNodes())
                {
                    DrawNode(node);
                }

                EditorGUILayout.EndScrollView(); 

                if(_creatingNode != null)
                {                    
                    _selectedDialogue.CreateNode(_creatingNode);
                    _creatingNode = null;
                }
                if(_deletingNode != null)
                {                   
                    _selectedDialogue.DeleteNode(_deletingNode);
                    _deletingNode = null;
                }
            }
        }

        private void ProcessEvent()
        {
            if(Event.current.type == EventType.MouseDown && _draggingNode == null)
            {
                _draggingNode = GetNodeAtPoint(Event.current.mousePosition + _scrollPosition);
                if(_draggingNode != null)
                {
                    _draggingOffset = _draggingNode.GetRect().position - Event.current.mousePosition;
                    Selection.activeObject = _draggingNode;
                }
                else
                {
                    _draggingCanvas = true;
                    _draggingCanvasOffset = Event.current.mousePosition + _scrollPosition;
                    Selection.activeObject = _selectedDialogue;
                }
            }
            else if(Event.current.type == EventType.MouseDrag && _draggingNode != null)
            {
                _draggingNode.SetPosition(Event.current.mousePosition + _draggingOffset);
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseDrag && _draggingCanvas)
            {
                _scrollPosition = _draggingCanvasOffset - Event.current.mousePosition;
                GUI.changed = true;
            }
            else if(Event.current.type == EventType.MouseUp && _draggingNode != null)
            {
                _draggingNode = null;
            }
            else if (Event.current.type == EventType.MouseUp && _draggingCanvas)
            {
                _draggingCanvas = false;
            }
        }

        private void DrawNode(DialogueNode node)
        {
            GUIStyle style = _nodeStyle;
            if (node.IsPlayerSpeaking())
            {
                style = _playerNodeStyle;
            }
            GUILayout.BeginArea(node.GetRect(), _nodeStyle);

            EditorGUILayout.LabelField("Node:", EditorStyles.whiteLabel);
            node.SetText(EditorGUILayout.TextField(node.GetText()));
            string newUniqueID = EditorGUILayout.TextField(node.name);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("X"))
            {
                _deletingNode = node;
            }
            DrawLinkButtons(node);
            if (GUILayout.Button("+"))
            {
                _creatingNode = node;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawLinkButtons(DialogueNode node)
        {
            if (_linkingparentNode == null)
            {
                if (GUILayout.Button("link"))
                {
                    _linkingparentNode = node;
                }
            }
            else if(_linkingparentNode == node)
            {
                if (GUILayout.Button("cancel"))
                {
                    _linkingparentNode = null;
                }
            }
            else if (_linkingparentNode.GetChildren().Contains(node.name))
            {
                if (GUILayout.Button("unlink"))
                {
                    _linkingparentNode.RemoveChild(node.name);
                    _linkingparentNode = null;
                }
            }
            else
            {
                if (GUILayout.Button("child"))
                {
                    _linkingparentNode.AddChild(node.name);
                    _linkingparentNode = null;
                }
            }
        }

        private void DrawConnections(DialogueNode node)
        {
            Vector3 startPosition = new Vector2(node.GetRect().xMax, node.GetRect().center.y);
            foreach (DialogueNode childNode in _selectedDialogue.GetAllChildren(node))
            {          
                Vector3 endPosition = new Vector2(childNode.GetRect().xMin, childNode.GetRect().center.y);
                Vector3 controlPointOffset = endPosition - startPosition;
                controlPointOffset.y = 0;
                controlPointOffset.x *= 0.8f;
                Handles.DrawBezier(startPosition, endPosition, startPosition + controlPointOffset, endPosition - controlPointOffset, Color.white, null, 4f);
            }
        }

        private DialogueNode GetNodeAtPoint(Vector2 point)
        {
            DialogueNode foundNode = null;
            foreach(DialogueNode node in _selectedDialogue.GetAllNodes())
            {
                if (node.GetRect().Contains(point))
                {
                    foundNode = node;
                }
            }
            return foundNode;
        }
    }
}
