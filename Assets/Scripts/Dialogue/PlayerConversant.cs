using GameDevTV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPG.Dialogue
{
    public class PlayerConversant : MonoBehaviour
    {
        [SerializeField] private string _playerName;

        Dialogue _currentDialogue;
        private DialogueNode _currentNode = null;
        private AIConversant _currentConversant = null;
        private bool _isChoosing = false;

        public event Action onConversationUpdated;

        public void StartDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            _currentConversant = newConversant;
            _currentDialogue = newDialogue;
            // ИСПРАВЛЕНИЕ: передаем эвалюаторы для выбора подходящей стартовой ноды
            _currentNode = _currentDialogue.GetRootNode(GetEvaluators());
            TriggerEnterAction();
            onConversationUpdated();
        }

        public void Quit()
        {           
            _currentDialogue = null;
            TriggerExitAction();
            _currentConversant = null;
            _currentNode = null;
            _isChoosing = false;
            onConversationUpdated();
        }

        public bool IsActive()
        {
            return _currentDialogue != null;
        }

        public bool IsChoosing()
        {
            return _isChoosing;
        }

        public string GetText()
        {
            if(_currentNode == null)
            {
                return "";
            }

            return _currentNode.GetText();
        }

        public IEnumerable<DialogueNode> GetChoices()
        {
            var choices = FilterOnCondition(_currentDialogue.GetPlayerChildren(_currentNode)).ToList();
            Debug.Log($"GetChoices found {choices.Count} choices."); 
            return choices;
        }

        public void SelectChoice(DialogueNode chosenNode)
        {
            _currentNode = chosenNode;
            TriggerEnterAction();
            _isChoosing = false;
            Next();
        }

        public void Next()
        {
            int numPlayerResponses = FilterOnCondition(_currentDialogue.GetPlayerChildren(_currentNode)).Count();
            if(numPlayerResponses > 0)
            {
                _isChoosing = true;
                TriggerExitAction();
                onConversationUpdated();
                return;
            }

            DialogueNode[] children = FilterOnCondition(_currentDialogue.GetAIChildren(_currentNode)).ToArray();
            int randomIndex = UnityEngine.Random.Range(0, children.Count());
            TriggerExitAction();
            Debug.Log($"Player responses count: {numPlayerResponses}");
            Debug.Log($"AI children count: {children.Length}");
            _currentNode = children[randomIndex];
            TriggerEnterAction();
            onConversationUpdated();
        }

        public bool HasNext()
        {
            string nodeID = (_currentNode != null) ? _currentNode.name : "NULL";
            Debug.Log($"HasNext: Called for node: {nodeID}");
            var allChildren = _currentDialogue.GetAllChildren(_currentNode);
            var allChildrenList = allChildren.ToList();
            int initialCount = allChildrenList.Count;
            string nodeName = (_currentNode != null) ? _currentNode.name : "NULL";
            Debug.Log($"HasNext: Node '{nodeName}' - Initial children count: {initialCount}");

            var filteredChildren = FilterOnCondition(allChildrenList);
            int filteredCount = filteredChildren.Count();
            Debug.Log($"HasNext: Node '{nodeName}' - Filtered children count: {filteredCount}");
            return filteredCount > 0;
        }

        private IEnumerable<DialogueNode> FilterOnCondition(IEnumerable<DialogueNode> inputNodes)
        {
            var evaluators = GetEvaluators().ToList();
            Debug.Log($"FilterOnCondition: Checking with {evaluators.Count} evaluators.");

            foreach(var node in inputNodes)
            {
                string nodeName = (node != null) ? node.name : "NULL_NODE";
                if (node == null)
                {
                    Debug.LogWarning("FilterOnCondition: Encountered a NULL node in inputNodes.");
                    continue;
                }

                bool conditionResult = node.CheckCondition(evaluators);
                Debug.Log($"FilterOnCondition: Checking node '{nodeName}'. Condition result: {conditionResult}");
                if (conditionResult)
                {
                    Debug.Log($"FilterOnCondition: Node '{nodeName}' PASSED.");
                    yield return node;
                }
            }
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators()
        {
            return GetComponents<IPredicateEvaluator>();
        }

        private void TriggerEnterAction()
        {
            if(_currentNode != null)
            {
                foreach (var action in _currentNode.GetOnEnterActions())
                {
                    TriggerAction(action);
                }
            }
        }

        private void TriggerExitAction()
        {
            if (_currentNode != null)
            {
                foreach (var action in _currentNode.GetOnExitActions())
                {
                    TriggerAction(action);
                }
            }
        }

        private void TriggerAction(string action)
        {
            if (action == "") return;

            foreach(DialogueTrigger trigger in _currentConversant.GetComponents<DialogueTrigger>())
            {
                trigger.Trigger(action);
            }
        }

        public string GetCurrentConversantName()
        {
            if (_isChoosing)
            {
                return _playerName;
            }
            else
            {
                return _currentConversant.GetName();
            }
        }
    }
}