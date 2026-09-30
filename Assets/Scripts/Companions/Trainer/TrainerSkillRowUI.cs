using RPG.Companions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI.Companions
{
    /// <summary>Строка навыка в окне дрессировщика (префаб для AnimalTrainerUI.rowPrefab).</summary>
    public class TrainerSkillRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameField;
        [Tooltip("«3 / 5»")]
        [SerializeField] private TextMeshProUGUI pointsField;
        [SerializeField] private TextMeshProUGUI costField;
        [Tooltip("Почему нельзя прокачать (необязательно).")]
        [SerializeField] private TextMeshProUGUI statusField;
        [SerializeField] private Button buyButton;
        [SerializeField] private Color notEnoughMoneyColor = Color.red;

        private AnimalTrainer _trainer;
        private int _index;
        private Color _originalCostColor = Color.white;

        private void Awake()
        {
            if (costField != null) _originalCostColor = costField.color;
            if (buyButton != null) buyButton.onClick.AddListener(Buy);
        }

        public void Setup(AnimalTrainer trainer, SkillOffer offer)
        {
            _trainer = trainer;
            _index = offer.index;

            SetText(nameField, offer.displayName);
            SetText(pointsField, $"{offer.currentPoints} / {offer.maxPoints}");

            bool showCost = offer.blockReason != TrainingBlockReason.MaxReached
                         && offer.blockReason != TrainingBlockReason.NotConfigured
                         && offer.blockReason != TrainingBlockReason.NoCompanion;
            SetText(costField, showCost ? $"${offer.cost:N0}" : "");
            if (costField != null)
                costField.color = offer.blockReason == TrainingBlockReason.NotEnoughMoney ? notEnoughMoneyColor : _originalCostColor;

            SetText(statusField, GetStatus(offer));
            if (buyButton != null) buyButton.interactable = offer.CanBuy;
        }

        private static string GetStatus(SkillOffer offer)
        {
            switch (offer.blockReason)
            {
                case TrainingBlockReason.None:           return "";
                case TrainingBlockReason.LevelTooLow:    return $"нужен уровень {offer.requiredLevel}";
                case TrainingBlockReason.NotEnoughMoney: return "";   // уже видно по красной цене
                default:                                 return AnimalTrainer.GetReasonText(offer.blockReason);
            }
        }

        private void Buy()
        {
            if (_trainer != null) _trainer.UpgradeSkill(_index);
        }

        private static void SetText(TextMeshProUGUI field, string text)
        {
            if (field != null) field.text = text;
        }
    }
}
