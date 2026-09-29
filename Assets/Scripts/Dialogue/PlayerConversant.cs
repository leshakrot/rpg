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

        // Провайдеры динамических списков ответов (см. IDynamicChoiceProvider) —
        // например, CompanionQuestGuideChoiceProvider подменяет статичные children
        // ноды "Веди меня к цели" списком активных квестов.
        private readonly List<IDynamicChoiceProvider> _dynamicProviders = new List<IDynamicChoiceProvider>();

        public event Action onConversationUpdated;

        public void RegisterDynamicChoiceProvider(IDynamicChoiceProvider provider)
        {
            if (provider != null && !_dynamicProviders.Contains(provider))
                _dynamicProviders.Add(provider);
        }

        public void UnregisterDynamicChoiceProvider(IDynamicChoiceProvider provider)
        {
            _dynamicProviders.Remove(provider);
        }

        public void StartDialogue(AIConversant newConversant, Dialogue newDialogue)
        {
            _currentConversant = newConversant;
            _currentDialogue = newDialogue;
            _currentNode = _currentDialogue.GetRootNode(GetEvaluators());
            TriggerEnterAction();
            onConversationUpdated();
        }

        public void Quit()
        {
            // ИСПРАВЛЕНИЕ: TriggerExitAction вызывается ДО сброса _currentNode и _currentConversant,
            // иначе последняя нода не успевает выполнить свои OnExitActions
            TriggerExitAction();
            _currentDialogue = null;
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
            if (_currentNode == null)
            {
                return "";
            }

            return _currentNode.GetText();
        }

        public IEnumerable<DialogueNode> GetChoices()
        {
            // Если для текущей ноды зарегистрирован провайдер динамических вариантов —
            // отдаём его список вместо статичных children из ассета Dialogue.
            foreach (var provider in _dynamicProviders)
            {
                if (provider != null && provider.TryGetChoices(_currentNode, out IEnumerable<DialogueNode> dynamicChoices))
                {
                    return dynamicChoices;
                }
            }

            return FilterOnCondition(_currentDialogue.GetPlayerChildren(_currentNode));
        }

        public void SelectChoice(DialogueNode chosenNode)
        {
            // Даём провайдерам шанс распознать выбранную ноду как "свою" (например,
            // динамически сгенерированный пункт "веди меня к цели X") и обработать выбор
            // до того, как сработают её OnEnterActions.
            foreach (var provider in _dynamicProviders)
            {
                if (provider != null && provider.TryHandleSelection(chosenNode))
                {
                    break;
                }
            }

            _currentNode = chosenNode;
            TriggerEnterAction();
            _isChoosing = false;
            Next();
        }

        public void Next()
        {
            int numPlayerResponses = GetChoices().Count();
            if (numPlayerResponses > 0)
            {
                _isChoosing = true;
                TriggerExitAction();
                onConversationUpdated();
                return;
            }

            DialogueNode[] children = FilterOnCondition(_currentDialogue.GetAIChildren(_currentNode)).ToArray();

            // ИСПРАВЛЕНИЕ: TriggerExitAction вызывается до перехода на следующую ноду,
            // пока _currentNode ещё указывает на текущую
            TriggerExitAction();

            // ИСПРАВЛЕНИЕ: если детей нет — это последняя нода, завершаем диалог.
            // Раньше здесь был IndexOutOfRangeException и OnExitActions не успевали выполниться.
            if (children.Length == 0)
            {
                _currentDialogue = null;
                _currentConversant = null;
                _currentNode = null;
                _isChoosing = false;
                onConversationUpdated();
                return;
            }

            // ИСПРАВЛЕНИЕ: используем children.Length вместо children.Count() —
            // у int-массива Count() это лишний вызов через LINQ, Length корректнее.
            // Random.Range(int, int) исключает верхнюю границу, поэтому Length правильно.
            int randomIndex = UnityEngine.Random.Range(0, children.Length);
            _currentNode = children[randomIndex];
            TriggerEnterAction();
            onConversationUpdated();
        }

        public bool HasNext()
        {
            return FilterOnCondition(_currentDialogue.GetAllChildren(_currentNode)).Any();
        }

        private IEnumerable<DialogueNode> FilterOnCondition(IEnumerable<DialogueNode> inputNodes)
        {
            var evaluators = GetEvaluators().ToList();

            foreach (var node in inputNodes)
            {
                if (node == null)
                {
                    Debug.LogWarning("FilterOnCondition: Encountered a NULL node in inputNodes.");
                    continue;
                }

                if (node.CheckCondition(evaluators))
                {
                    yield return node;
                }
            }
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators()
        {
            // Берём предикаты с игрока
            var evaluators = GetComponents<IPredicateEvaluator>().ToList();

            // Добавляем предикаты с текущего NPC-собеседника
            if (_currentConversant != null)
            {
                evaluators.AddRange(_currentConversant.GetComponents<IPredicateEvaluator>());
            }

            return evaluators;
        }

        private void TriggerEnterAction()
        {
            if (_currentNode != null)
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
            if (string.IsNullOrEmpty(action)) return;

            foreach (DialogueTrigger trigger in _currentConversant.GetComponents<DialogueTrigger>())
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
