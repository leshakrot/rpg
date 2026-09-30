using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RPG.Inventories;
using RPG.Stats;

namespace RPG.Companions
{
    public enum TrainingBlockReason
    {
        None,
        NoCompanion,     // подходящего питомца нет рядом с игроком
        NoPurse,         // у игрока нет кошелька
        NotOffered,      // дрессировщик этого не предлагает
        NotConfigured,   // у питомца нет эффекта для этого навыка (bonusConfig в AnimalTraining)
        MaxReached,      // достигнут максимум
        LevelTooLow,     // уровень питомца ниже требуемого
        NotEnoughMoney
    }

    /// <summary>Предложение по навыку — для UI / диалогов.</summary>
    public struct SkillOffer
    {
        public int index;
        public Trait trait;
        public string displayName;
        public int currentPoints;
        public int maxPoints;
        public int requiredLevel;
        public float cost;
        public TrainingBlockReason blockReason;
        public bool CanBuy => blockReason == TrainingBlockReason.None;
    }

    /// <summary>Предложение по повышению общего уровня питомца.</summary>
    public struct LevelOffer
    {
        public int currentLevel;
        public int maxLevel;
        public float cost;
        public TrainingBlockReason blockReason;
        public bool CanBuy => blockReason == TrainingBlockReason.None;
    }

    /// <summary>
    /// Дрессировщик животных-компаньонов. Вешается на NPC; таких NPC в игре может быть сколько угодно,
    /// у каждого свой набор навыков и свои цены.
    ///
    /// Тренирует питомцев, которые СЕЙЧАС сопровождают игрока (нанят + активен + есть AnimalTraining).
    /// Если таких несколько — в окне можно переключаться между ними.
    ///
    /// Открыть окно можно из чего угодно, что умеет вызвать публичный метод:
    ///   • клик по NPC — добавь AnimalTrainerInteractable;
    ///   • нода диалога — в onEnterActions вызови AnimalTrainer.OpenWindow();
    ///   • код — trainer.OpenWindow().
    /// Само окно рисует AnimalTrainerUI (слушает AnimalTrainer.Active).
    /// </summary>
    public class AnimalTrainer : MonoBehaviour
    {
        // ── «текущая сессия» окна ─────────────────────────────────────────

        /// <summary>Дрессировщик, у которого сейчас открыто окно (null — окно закрыто).</summary>
        public static AnimalTrainer Active { get; private set; }

        /// <summary>Окно открыли/закрыли/переключили на другого дрессировщика. Аргумент — новый Active.</summary>
        public static event Action<AnimalTrainer> onActiveChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            onActiveChanged = null;
        }

        // ── настройки ─────────────────────────────────────────────────────

        [Header("Общее")]
        [SerializeField] private string trainerName = "Дрессировщик";
        [Tooltip("Каких питомцев он тренирует. Пусто — любого питомца с AnimalTraining, который сопровождает игрока.")]
        [SerializeField] private CompanionData[] allowedCompanions = new CompanionData[0];
        [Tooltip("Если игрок отошёл от дрессировщика дальше этого расстояния — окно закроется. 0 = не закрывать.")]
        [SerializeField, Min(0f)] private float autoCloseDistance = 6f;

        [Header("Навыки, которые можно прокачать")]
        [Tooltip("Только навыки из этого списка доступны у данного дрессировщика. " +
                 "Чтобы навык что-то давал, он должен быть настроен в AnimalTraining на самом питомце.")]
        [SerializeField] private TrainableSkill[] skills = new TrainableSkill[0];

        [Header("Повышение общего уровня")]
        [SerializeField] private bool offerLevelUp = true;
        [Tooltip("Цена первого повышения уровня.")]
        [SerializeField, Min(0f)] private float levelUpBaseCost = 200f;
        [Tooltip("Насколько дорожает каждое следующее повышение (прибавляется за каждый уже купленный уровень).")]
        [SerializeField, Min(0f)] private float levelUpCostIncrease = 100f;
        [Tooltip("Сколько уровней питомец может получить у дрессировщиков суммарно. 0 = до максимального уровня по Progression.")]
        [SerializeField, Min(0)] private int maxTrainedLevels = 0;

        [Header("События")]
        public UnityEvent onWindowOpened;
        public UnityEvent onWindowClosed;
        [Tooltip("OpenWindow() вызвали, но подходящего питомца рядом нет (например, показать сообщение или реплику).")]
        public UnityEvent onOpenFailed;

        /// <summary>Один навык, доступный у дрессировщика. Цена очка = baseCost + costIncreasePerPoint × уже вложено.</summary>
        [Serializable]
        public class TrainableSkill
        {
            public Trait trait;
            [Tooltip("Название для UI. Пусто — берётся имя Trait.")]
            public string displayName;
            [Min(1)] public int maxPoints = 5;
            [Tooltip("Минимальный уровень питомца, чтобы качать этот навык.")]
            [Min(1)] public int requiredCompanionLevel = 1;
            [Min(0f)] public float baseCost = 100f;
            [Min(0f)] public float costIncreasePerPoint = 50f;

            public string GetDisplayName() => string.IsNullOrWhiteSpace(displayName) ? trait.ToString() : displayName;
        }

        /// <summary>Что-то изменилось (покупка, смена питомца) — обновить UI.</summary>
        public event Action onChanged;

        private string _selectedId;
        private Transform _player;

        public string TrainerName => trainerName;
        public int SkillCount => skills != null ? skills.Length : 0;

        // ── lifecycle ────────────────────────────────────────────────────

        private void Update()
        {
            if (Active != this || autoCloseDistance <= 0f) return;

            if (_player == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p == null) return;
                _player = p.transform;
            }

            if (Vector3.Distance(transform.position, _player.position) > autoCloseDistance)
                CloseWindow();
        }

        private void OnDisable()
        {
            CloseWindow();
        }

        // ── окно ─────────────────────────────────────────────────────────

        /// <summary>
        /// Открыть окно прокачки. Вызывай при клике на дрессировщика или из ноды диалога.
        /// Если подходящего питомца рядом нет — окно не откроется, сработает onOpenFailed.
        /// </summary>
        public void OpenWindow()
        {
            if (GetAvailableCompanions().Count == 0)
            {
                Debug.Log("[AnimalTrainer] Нет питомца, которого можно тренировать (не нанят, не активен или не подходит этому дрессировщику).", this);
                onOpenFailed.Invoke();
                return;
            }

            if (Active == this) return;
            if (Active != null) Active.CloseWindow();

            Active = this;
            onActiveChanged?.Invoke(this);
            onWindowOpened.Invoke();
        }

        public void CloseWindow()
        {
            if (Active != this) return;

            Active = null;
            onActiveChanged?.Invoke(null);
            onWindowClosed.Invoke();
        }

        // ── питомцы ──────────────────────────────────────────────────────

        /// <summary>Питомцы, которых этот дрессировщик может тренировать прямо сейчас.</summary>
        public List<AnimalTraining> GetAvailableCompanions()
        {
            var result = new List<AnimalTraining>();

            CompanionManager manager = CompanionManager.Instance;
            if (manager == null) return result;

            foreach (string id in manager.GetActiveCompanionIDs())
            {
                if (!IsAllowed(id)) continue;

                CompanionController controller = manager.GetActiveCompanion(id);
                if (controller == null) continue;

                AnimalTraining training = controller.GetComponent<AnimalTraining>();
                if (training != null && training.Stats != null)
                    result.Add(training);
            }

            result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
            return result;
        }

        /// <summary>Выбранный питомец (если выбранного нет — первый доступный). null — тренировать некого.</summary>
        public AnimalTraining GetSelectedCompanion()
        {
            List<AnimalTraining> list = GetAvailableCompanions();
            if (list.Count == 0) return null;

            foreach (AnimalTraining training in list)
                if (training.CompanionID == _selectedId) return training;

            _selectedId = list[0].CompanionID;
            return list[0];
        }

        public void SelectCompanion(string companionId)
        {
            _selectedId = companionId;
            onChanged?.Invoke();
        }

        /// <summary>Переключить питомца: +1 следующий, −1 предыдущий (для кнопок «‹ ›»).</summary>
        public void CycleCompanion(int direction)
        {
            List<AnimalTraining> list = GetAvailableCompanions();
            if (list.Count < 2) return;

            int current = Mathf.Max(0, list.FindIndex(t => t.CompanionID == _selectedId));
            int next = ((current + direction) % list.Count + list.Count) % list.Count;

            _selectedId = list[next].CompanionID;
            onChanged?.Invoke();
        }

        private bool IsAllowed(string companionId)
        {
            if (allowedCompanions == null || allowedCompanions.Length == 0) return true;

            foreach (CompanionData data in allowedCompanions)
                if (data != null && data.CompanionID == companionId) return true;

            return false;
        }

        // ── предложения (для UI и диалогов) ───────────────────────────────

        public List<SkillOffer> GetSkillOffers()
        {
            AnimalTraining training = GetSelectedCompanion();

            var result = new List<SkillOffer>(SkillCount);
            for (int i = 0; i < SkillCount; i++)
                result.Add(BuildSkillOffer(i, training));
            return result;
        }

        public SkillOffer GetSkillOffer(int index) => BuildSkillOffer(index, GetSelectedCompanion());

        public LevelOffer GetLevelOffer() => BuildLevelOffer(GetSelectedCompanion());

        private SkillOffer BuildSkillOffer(int index, AnimalTraining training)
        {
            var offer = new SkillOffer { index = index };
            if (index < 0 || index >= SkillCount)
            {
                offer.blockReason = TrainingBlockReason.NotOffered;
                return offer;
            }

            TrainableSkill skill = skills[index];
            offer.trait         = skill.trait;
            offer.displayName   = skill.GetDisplayName();
            offer.maxPoints     = skill.maxPoints;
            offer.requiredLevel = skill.requiredCompanionLevel;

            if (training == null)
            {
                offer.blockReason = TrainingBlockReason.NoCompanion;
                return offer;
            }

            offer.currentPoints = training.GetPoints(skill.trait);
            offer.cost = skill.baseCost + skill.costIncreasePerPoint * offer.currentPoints;

            if (!training.IsTraitConfigured(skill.trait))
                offer.blockReason = TrainingBlockReason.NotConfigured;
            else if (offer.currentPoints >= skill.maxPoints)
                offer.blockReason = TrainingBlockReason.MaxReached;
            else if (training.Stats.GetLevel() < skill.requiredCompanionLevel)
                offer.blockReason = TrainingBlockReason.LevelTooLow;
            else
                offer.blockReason = CheckFunds(offer.cost);

            return offer;
        }

        private LevelOffer BuildLevelOffer(AnimalTraining training)
        {
            var offer = new LevelOffer();

            if (!offerLevelUp)
            {
                offer.blockReason = TrainingBlockReason.NotOffered;
                return offer;
            }

            if (training == null)
            {
                offer.blockReason = TrainingBlockReason.NoCompanion;
                return offer;
            }

            int trained = training.GetTrainedLevels();
            offer.currentLevel = training.Stats.GetLevel();
            offer.maxLevel     = training.Stats.GetMaxLevel();
            offer.cost         = levelUpBaseCost + levelUpCostIncrease * trained;

            if (offer.currentLevel >= offer.maxLevel || (maxTrainedLevels > 0 && trained >= maxTrainedLevels))
                offer.blockReason = TrainingBlockReason.MaxReached;
            else
                offer.blockReason = CheckFunds(offer.cost);

            return offer;
        }

        // ── покупка ───────────────────────────────────────────────────────

        public bool TryUpgradeSkill(int index)
        {
            AnimalTraining training = GetSelectedCompanion();
            SkillOffer offer = BuildSkillOffer(index, training);
            if (!offer.CanBuy)
            {
                Debug.Log($"[AnimalTrainer] Навык «{offer.displayName}» недоступен: {GetReasonText(offer.blockReason)}", this);
                return false;
            }

            if (!training.AddPoints(offer.trait)) return false;

            GetPlayerPurse().UpdateBalance(-offer.cost);
            onChanged?.Invoke();
            return true;
        }

        public bool TryUpgradeLevel()
        {
            AnimalTraining training = GetSelectedCompanion();
            LevelOffer offer = BuildLevelOffer(training);
            if (!offer.CanBuy)
            {
                Debug.Log($"[AnimalTrainer] Повышение уровня недоступно: {GetReasonText(offer.blockReason)}", this);
                return false;
            }

            if (!training.AddLevels()) return false;

            GetPlayerPurse().UpdateBalance(-offer.cost);
            onChanged?.Invoke();
            return true;
        }

        // ── методы для UnityEvent (void, чтобы были видны в инспекторе) ──

        /// <summary>Прокачать навык по номеру в списке «Skills» (с нуля) у выбранного питомца.</summary>
        public void UpgradeSkill(int index) => TryUpgradeSkill(index);

        public void UpgradeLevel() => TryUpgradeLevel();

        // ── предикаты для условий диалога ─────────────────────────────────

        public bool CheckCompanionPresent()     => GetAvailableCompanions().Count > 0;
        public bool CheckCanUpgradeSkill(int i) => GetSkillOffer(i).CanBuy;
        public bool CheckCanUpgradeLevel()      => GetLevelOffer().CanBuy;

        // ── вспомогательное ───────────────────────────────────────────────

        public static string GetReasonText(TrainingBlockReason reason)
        {
            switch (reason)
            {
                case TrainingBlockReason.NoCompanion:    return "питомца нет рядом";
                case TrainingBlockReason.NoPurse:        return "у игрока нет кошелька";
                case TrainingBlockReason.NotOffered:     return "дрессировщик этого не предлагает";
                case TrainingBlockReason.NotConfigured:  return "питомец этому не обучается";
                case TrainingBlockReason.MaxReached:     return "достигнут максимум";
                case TrainingBlockReason.LevelTooLow:    return "уровень питомца слишком низкий";
                case TrainingBlockReason.NotEnoughMoney: return "недостаточно денег";
                default:                                 return "";
            }
        }

        private static TrainingBlockReason CheckFunds(float cost)
        {
            Purse purse = GetPlayerPurse();
            if (purse == null) return TrainingBlockReason.NoPurse;
            return purse.GetBalance() >= cost ? TrainingBlockReason.None : TrainingBlockReason.NotEnoughMoney;
        }

        public static Purse GetPlayerPurse()
        {
            GameObject player = GameObject.FindWithTag("Player");
            return player != null ? player.GetComponent<Purse>() : null;
        }

        private void OnValidate()
        {
            if (skills == null) return;

            var seen = new HashSet<Trait>();
            foreach (TrainableSkill skill in skills)
            {
                if (!seen.Add(skill.trait))
                    Debug.LogWarning($"[AnimalTrainer] Навык {skill.trait} указан несколько раз — очки будут общими.", this);
            }
        }
    }
}
