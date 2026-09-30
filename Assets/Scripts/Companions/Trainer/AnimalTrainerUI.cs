using System.Collections.Generic;
using RPG.Companions;
using RPG.Inventories;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI.Companions
{
    /// <summary>
    /// Окно прокачки питомца. Показывается, когда кто-то вызвал AnimalTrainer.OpenWindow().
    ///
    /// ВАЖНО: этот компонент должен лежать на ВСЕГДА активном объекте (например, на корне UI-канваса
    /// или на пустом объекте рядом с ShopUI), а не на самой панели окна. Панель — это windowRoot:
    /// её компонент включает/выключает сам.
    /// </summary>
    public class AnimalTrainerUI : MonoBehaviour
    {
        [Header("Окно")]
        [Tooltip("Панель окна — включается при открытии и выключается при закрытии. Не должна быть этим же объектом.")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private TextMeshProUGUI trainerNameField;
        [SerializeField] private TextMeshProUGUI balanceField;
        [Tooltip("Сообщение вида «Питомца нет рядом» (необязательно).")]
        [SerializeField] private TextMeshProUGUI messageField;
        [SerializeField] private Button closeButton;

        [Header("Выбор питомца")]
        [SerializeField] private TextMeshProUGUI companionNameField;
        [Tooltip("Стрелки показываются, только если рядом больше одного подходящего питомца.")]
        [SerializeField] private Button previousCompanionButton;
        [SerializeField] private Button nextCompanionButton;

        [Header("Общий уровень")]
        [Tooltip("Весь блок уровня — скрывается, если дрессировщик не предлагает повышение уровня.")]
        [SerializeField] private GameObject levelRoot;
        [SerializeField] private TextMeshProUGUI levelField;
        [SerializeField] private TextMeshProUGUI levelCostField;
        [SerializeField] private TextMeshProUGUI levelStatusField;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Color notEnoughMoneyColor = Color.red;

        [Header("Навыки")]
        [SerializeField] private Transform listRoot;
        [SerializeField] private TrainerSkillRowUI rowPrefab;

        private AnimalTrainer _trainer;
        private Purse _purse;
        private Color _originalLevelCostColor = Color.white;
        private readonly List<TrainerSkillRowUI> _rows = new List<TrainerSkillRowUI>();

        private void Awake()
        {
            if (windowRoot == gameObject)
            {
                Debug.LogError("[AnimalTrainerUI] windowRoot не должен быть тем же объектом, что и этот компонент.", this);
                enabled = false;
                return;
            }

            if (levelCostField != null) _originalLevelCostColor = levelCostField.color;

            if (closeButton != null)          closeButton.onClick.AddListener(Close);
            if (previousCompanionButton != null) previousCompanionButton.onClick.AddListener(() => CycleCompanion(-1));
            if (nextCompanionButton != null)     nextCompanionButton.onClick.AddListener(() => CycleCompanion(1));
            if (levelUpButton != null)        levelUpButton.onClick.AddListener(UpgradeLevel);

            if (windowRoot != null) windowRoot.SetActive(false);
        }

        private void OnEnable()
        {
            AnimalTrainer.onActiveChanged += OnActiveChanged;
            OnActiveChanged(AnimalTrainer.Active);
        }

        private void OnDisable()
        {
            AnimalTrainer.onActiveChanged -= OnActiveChanged;
            Unbind();
            if (windowRoot != null) windowRoot.SetActive(false);
        }

        /// <summary>Можно повесить на кнопку «Закрыть».</summary>
        public void Close()
        {
            if (AnimalTrainer.Active != null) AnimalTrainer.Active.CloseWindow();
        }

        // ── привязка к дрессировщику ──────────────────────────────────────

        private void OnActiveChanged(AnimalTrainer trainer)
        {
            Unbind();
            _trainer = trainer;

            if (_trainer == null)
            {
                if (windowRoot != null) windowRoot.SetActive(false);
                return;
            }

            _trainer.onChanged += Refresh;

            _purse = AnimalTrainer.GetPlayerPurse();
            if (_purse != null) _purse.onChange += Refresh;

            if (windowRoot != null) windowRoot.SetActive(true);
            Refresh();
        }

        private void Unbind()
        {
            if (_trainer != null) _trainer.onChanged -= Refresh;
            if (_purse != null) _purse.onChange -= Refresh;
            _trainer = null;
            _purse = null;
        }

        // ── действия кнопок ───────────────────────────────────────────────

        private void CycleCompanion(int direction)
        {
            if (_trainer != null) _trainer.CycleCompanion(direction);
        }

        private void UpgradeLevel()
        {
            if (_trainer != null) _trainer.UpgradeLevel();
        }

        // ── отрисовка ─────────────────────────────────────────────────────

        private void Refresh()
        {
            if (_trainer == null) return;

            AnimalTraining companion = _trainer.GetSelectedCompanion();
            bool hasCompanion = companion != null;

            SetText(trainerNameField, _trainer.TrainerName);
            SetText(balanceField, _purse != null ? $"Монеты: {_purse.GetBalance():N0}" : "");
            SetText(messageField, hasCompanion ? "" : "Питомца нет рядом");
            SetText(companionNameField, hasCompanion ? companion.DisplayName : "—");

            bool canSwitch = _trainer.GetAvailableCompanions().Count > 1;
            if (previousCompanionButton != null) previousCompanionButton.gameObject.SetActive(canSwitch);
            if (nextCompanionButton != null)     nextCompanionButton.gameObject.SetActive(canSwitch);

            RefreshLevel(hasCompanion);
            RefreshSkills(hasCompanion);
        }

        private void RefreshLevel(bool hasCompanion)
        {
            LevelOffer offer = _trainer.GetLevelOffer();

            if (levelRoot != null) levelRoot.SetActive(offer.blockReason != TrainingBlockReason.NotOffered);

            SetText(levelField, hasCompanion ? $"Уровень: {offer.currentLevel} / {offer.maxLevel}" : "Уровень: —");

            bool showCost = hasCompanion && offer.blockReason != TrainingBlockReason.MaxReached;
            SetText(levelCostField, showCost ? $"${offer.cost:N0}" : "");
            if (levelCostField != null)
                levelCostField.color = offer.blockReason == TrainingBlockReason.NotEnoughMoney ? notEnoughMoneyColor : _originalLevelCostColor;

            bool showStatus = offer.blockReason == TrainingBlockReason.MaxReached;
            SetText(levelStatusField, showStatus ? AnimalTrainer.GetReasonText(offer.blockReason) : "");

            if (levelUpButton != null) levelUpButton.interactable = offer.CanBuy;
        }

        private void RefreshSkills(bool hasCompanion)
        {
            if (listRoot == null || rowPrefab == null) return;

            List<SkillOffer> offers = hasCompanion ? _trainer.GetSkillOffers() : new List<SkillOffer>();

            while (_rows.Count < offers.Count)
                _rows.Add(Instantiate(rowPrefab, listRoot));

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < offers.Count;
                _rows[i].gameObject.SetActive(used);
                if (used) _rows[i].Setup(_trainer, offers[i]);
            }
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null) field.text = text;
        }
    }
}
