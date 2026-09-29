using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Dialogue;
using RPG.Quests;

namespace RPG.Companions
{
    /// <summary>
    /// Подключается к диалогу игрока как IDynamicChoiceProvider: когда собеседник входит в ноду
    /// с меткой в OnEnterActions ("Веди меня к цели"), подменяет её статичных children на список
    /// активных незавершённых квестов, у которых прямо сейчас есть достижимый GuideTarget
    /// (Objective.guideTargetId, настраивается в Quest Editor).
    ///
    /// Лежит на той же собаке, что и CompanionGuide.
    /// Единственный ребёнок ноды-метки в графе диалога — общая AI-реплика "Хорошо, идём.",
    /// на которой в DialogueTrigger нужно повесить OnEnterAction → CompanionGuide.GuideToPendingSelection().
    ///
    /// ВАЖНО: сама строка-метка (marker) тоже проходит через общий TriggerAction() у PlayerConversant
    /// и будет искать одноимённое действие в DialogueTrigger собаки. Чтобы не было warning
    /// "Action ... not found" в консоли — добавь в DialogueTrigger собаки запись с таким же
    /// именем (marker) и пустым UnityEvent.
    /// </summary>
    [RequireComponent(typeof(CompanionGuide))]
    public class CompanionQuestGuideChoiceProvider : MonoBehaviour, IDynamicChoiceProvider
    {
        [Tooltip("Строка-метка в OnEnterActions ноды 'Веди меня к цели', по которой опознаётся точка подмены.")]
        [SerializeField] private string marker = "ShowActiveQuestsForGuide";

        private CompanionGuide _guide;
        private QuestList _questList;
        private PlayerConversant _conversant;

        private readonly Dictionary<DialogueNode, string> _liveChoices = new();

        private void Awake() => _guide = GetComponent<CompanionGuide>();

        private void OnEnable()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[CompanionQuestGuideChoiceProvider] Игрок не найден!", this);
                return;
            }

            _questList  = player.GetComponent<QuestList>();
            _conversant = player.GetComponent<PlayerConversant>();

            if (_questList == null)  Debug.LogError("[CompanionQuestGuideChoiceProvider] У игрока нет QuestList!", this);
            if (_conversant == null) Debug.LogError("[CompanionQuestGuideChoiceProvider] У игрока нет PlayerConversant!", this);

            _conversant?.RegisterDynamicChoiceProvider(this);
        }

        private void OnDisable()
        {
            _conversant?.UnregisterDynamicChoiceProvider(this);
            _liveChoices.Clear();
        }

        /// <summary>Есть ли хоть один квест, к цели которого можно повести прямо сейчас (для предиката диалога).</summary>
        public bool HasGuidableQuest() => BuildEntries().Any();

        // ── IDynamicChoiceProvider ───────────────────────────────────────

        public bool TryGetChoices(DialogueNode currentNode, out IEnumerable<DialogueNode> choices)
        {
            choices = null;
            if (currentNode == null || !currentNode.GetOnEnterActions().Contains(marker))
                return false;

            _liveChoices.Clear();
            var result = new List<DialogueNode>();

            foreach (var (quest, targetId) in BuildEntries())
            {
                DialogueNode node = DialogueNode.CreateRuntimeChoice(
                    quest.GetTitle(),
                    isPlayerSpeaking: true,
                    childIds: currentNode.GetChildren());

                _liveChoices[node] = targetId;
                result.Add(node);
            }

            choices = result;
            return true;
        }

        public bool TryHandleSelection(DialogueNode chosenNode)
        {
            if (!_liveChoices.TryGetValue(chosenNode, out string targetId)) return false;

            QuestGuideSelection.Set(targetId);
            return true;
        }

        // ── внутреннее ───────────────────────────────────────────────────

        /// <summary>
        /// По одной записи на активный незавершённый квест: первый его objective, у которого
        /// задан guideTargetId, он ещё не завершён и до цели сейчас есть маршрут.
        /// </summary>
        private IEnumerable<(Quest quest, string targetId)> BuildEntries()
        {
            if (_questList == null || _guide == null) yield break;

            foreach (QuestStatus status in _questList.GetStatuses())
            {
                if (status == null || status.GetQuest() == null || status.IsComplete()) continue;

                Quest quest = status.GetQuest();

                foreach (var objective in quest.GetObjectives())
                {
                    if (string.IsNullOrEmpty(objective.guideTargetId)) continue;
                    if (status.IsObjectiveComplete(objective.reference)) continue;
                    if (!_guide.IsGuideTargetReachable(objective.guideTargetId)) continue;

                    yield return (quest, objective.guideTargetId);
                    break; // одна цель на квест за раз
                }
            }
        }
    }
}
