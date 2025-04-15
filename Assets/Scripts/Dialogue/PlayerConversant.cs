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
            _currentNode = _currentDialogue.GetRootNode();
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
	    	string nodeID = (_currentNode != null) ? _currentNode.name : "NULL"; // Предполагаем, что есть GetUniqueID() или используем name
		    Debug.Log($"HasNext: Called for node: {nodeID}");
		    var allChildren = _currentDialogue.GetAllChildren(_currentNode);
		    // Конвертируем в список, чтобы посчитать, если GetAllChildren возвращает ленивый IEnumerable
		    var allChildrenList = allChildren.ToList();
		    int initialCount = allChildrenList.Count;
		    // Добавим имя текущего узла для контекста
		    string nodeName = (_currentNode != null) ? _currentNode.name : "NULL";
		    Debug.Log($"HasNext: Node '{nodeName}' - Initial children count: {initialCount}");

		    // Теперь фильтруем список, который уже в памяти
		    var filteredChildren = FilterOnCondition(allChildrenList);
		    int filteredCount = filteredChildren.Count(); // Или ToList().Count()
		    Debug.Log($"HasNext: Node '{nodeName}' - Filtered children count: {filteredCount}");
		    return filteredCount > 0;
	    }

	    private IEnumerable<DialogueNode> FilterOnCondition(IEnumerable<DialogueNode> inputNodes)
	    {
		    // Получаем эвалюаторы один раз и логируем их количество
		    var evaluators = GetEvaluators().ToList();
		    Debug.Log($"FilterOnCondition: Checking with {evaluators.Count} evaluators.");

		    foreach(var node in inputNodes)
		    {
			    // Проверяем, что узел не null перед доступом к имени
			    string nodeName = (node != null) ? node.name : "NULL_NODE";
			    if (node == null)
			    {
				    Debug.LogWarning("FilterOnCondition: Encountered a NULL node in inputNodes.");
				    continue; // Пропускаем null узлы, если они вдруг есть
			    }

			    bool conditionResult = node.CheckCondition(evaluators);
			    Debug.Log($"FilterOnCondition: Checking node '{nodeName}'. Condition result: {conditionResult}");
			    if (conditionResult)
			    {
				    // Этот лог покажет, какие узлы ПРОШЛИ проверку
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
                TriggerAction(_currentNode.GetOnEnterAction());
            }
        }

        private void TriggerExitAction()
        {
            if (_currentNode != null)
            {
                TriggerAction(_currentNode.GetOnExitAction());
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
