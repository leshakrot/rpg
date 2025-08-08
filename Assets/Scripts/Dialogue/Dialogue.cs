using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GameDevTV.Utils;

namespace RPG.Dialogue
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue", order = 0)]
    public class Dialogue : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<DialogueNode> nodes = new List<DialogueNode>();
        [SerializeField]
        Vector2 newNodeOffset = new Vector2(250, 0);

        Dictionary<string, DialogueNode> nodeLookup = new Dictionary<string, DialogueNode>();      
        
        private void OnEnable()
        {
            // Заполняем словарь при включении объекта
            // OnEnable может вызываться несколько раз, поэтому Clear() важен
            nodeLookup.Clear();
            if (nodes != null)
            {
                foreach (DialogueNode node in nodes)
                {
                    if (node != null)
                    {
                        // Здесь доступ к node.name должен быть безопасен
                        nodeLookup[node.name] = node;
                    }
                }
            }
        }

        public IEnumerable<DialogueNode> GetAllNodes()
        {
            return nodes;
        }

        // ИСПРАВЛЕННЫЙ МЕТОД: теперь принимает эвалюаторы и ищет подходящую стартовую ноду
        public DialogueNode GetRootNode(IEnumerable<IPredicateEvaluator> evaluators = null)
        {
            // Получаем все потенциальные корневые узлы (те, которые не являются детьми других узлов)
            List<DialogueNode> rootNodes = GetRootNodes();
            
            Debug.Log($"=== DIALOGUE ROOT NODE SELECTION ===");
            Debug.Log($"Found {rootNodes.Count} potential root nodes");
            
            // Если эвалюаторы не переданы, возвращаем первый найденный корневой узел
            if (evaluators == null)
            {
                Debug.Log("No evaluators provided, returning first root node");
                return rootNodes.Count > 0 ? rootNodes[0] : nodes[0];
            }
            
            // Собираем все подходящие узлы с их текстом для отладки
            List<DialogueNode> validNodes = new List<DialogueNode>();
            
            for (int i = 0; i < rootNodes.Count; i++)
            {
                DialogueNode rootNode = rootNodes[i];
                bool conditionResult = rootNode.CheckCondition(evaluators);
                
                Debug.Log($"Root node #{i}: '{rootNode.name}' | Text: '{rootNode.GetText()}' | Condition: {conditionResult}");
                
                if (conditionResult)
                {
                    validNodes.Add(rootNode);
                }
            }
            
            Debug.Log($"Valid nodes found: {validNodes.Count}");
            
            if (validNodes.Count > 0)
            {
                // Возвращаем первый валидный узел
                DialogueNode selectedNode = validNodes[0];
                Debug.Log($"SELECTED: '{selectedNode.name}' | Text: '{selectedNode.GetText()}'");
                return selectedNode;
            }
            
            // Если ни один корневой узел не подошел, возвращаем первый (fallback)
            Debug.LogWarning("No root nodes met their conditions, falling back to first available root node");
            DialogueNode fallbackNode = rootNodes.Count > 0 ? rootNodes[0] : nodes[0];
            Debug.Log($"FALLBACK: '{fallbackNode.name}' | Text: '{fallbackNode.GetText()}'");
            return fallbackNode;
        }
        
        // НОВЫЙ МЕТОД: находит все корневые узлы (не являющиеся детьми других узлов)
        private List<DialogueNode> GetRootNodes()
        {
            HashSet<string> childNodeIds = new HashSet<string>();
            
            // Собираем все ID узлов, которые являются детьми
            foreach (DialogueNode node in nodes)
            {
                foreach (string childId in node.GetChildren())
                {
                    childNodeIds.Add(childId);
                }
            }
            
            // Корневые узлы - это те, которые НЕ являются детьми других узлов
            List<DialogueNode> rootNodes = new List<DialogueNode>();
            foreach (DialogueNode node in nodes)
            {
                if (!childNodeIds.Contains(node.name))
                {
                    rootNodes.Add(node);
                }
            }
            
            // Сортируем по порядку в списке nodes для предсказуемости
            rootNodes.Sort((a, b) => nodes.IndexOf(a).CompareTo(nodes.IndexOf(b)));
            
            Debug.Log("=== ROOT NODES ORDER ===");
            for (int i = 0; i < rootNodes.Count; i++)
            {
                Debug.Log($"Root #{i}: '{rootNodes[i].name}' | Text: '{rootNodes[i].GetText()}'");
            }
            
            return rootNodes;
        }

        public IEnumerable<DialogueNode> GetAllChildren(DialogueNode parentNode)
        {	
            foreach (string childID in parentNode.GetChildren())
            {
                if (nodeLookup.ContainsKey(childID))
                {
                    yield return nodeLookup[childID];
                }
            }
        }

        public IEnumerable<DialogueNode> GetPlayerChildren(DialogueNode currentNode)
        {
            foreach (DialogueNode node in GetAllChildren(currentNode))
            {
                if (node.IsPlayerSpeaking())
                {
                    yield return node;
                }
            }
        }

        public IEnumerable<DialogueNode> GetAIChildren(DialogueNode currentNode)
        {
            foreach (DialogueNode node in GetAllChildren(currentNode))
            {
                if (!node.IsPlayerSpeaking())
                {
                    yield return node;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            nodeLookup.Clear();
            foreach (DialogueNode node in GetAllNodes())
            {
                nodeLookup[node.name] = node;
            }
        }
        
        public void CreateNode(DialogueNode parent)
        {
            DialogueNode newNode = MakeNode(parent);
            Undo.RegisterCreatedObjectUndo(newNode, "Created Dialogue Node");
            Undo.RecordObject(this, "Added Dialogue Node");
            AddNode(newNode);
        }

        public void DeleteNode(DialogueNode nodeToDelete)
        {
            Undo.RecordObject(this, "Deleted Dialogue Node");
            nodes.Remove(nodeToDelete);
            OnValidate();
            CleanDanglingChildren(nodeToDelete);
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
                newNode.SetPosition(parent.GetRect().position + newNodeOffset);
            }

            return newNode;
        }

        private void AddNode(DialogueNode newNode)
        {
            nodes.Add(newNode);
            OnValidate();
        }

        private void CleanDanglingChildren(DialogueNode nodeToDelete)
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
            if (nodes.Count == 0)
            {
                DialogueNode newNode = MakeNode(null);
                AddNode(newNode);
            }

            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (DialogueNode node in GetAllNodes())
                {
                    if (AssetDatabase.GetAssetPath(node) == "")
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