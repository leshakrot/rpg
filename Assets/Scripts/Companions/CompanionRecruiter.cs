using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Dialogue;
using RPG.Control;
using RPG.Inventories;

namespace RPG.Companions
{
    /// <summary>
    /// Компонент NPC-компаньона. 
    /// Лежит на том же объекте, что и CompanionController.
    /// Методы Hire, Dismiss и Call вызываются через UnityEvent из DialogueTrigger.
    /// </summary>
    public class CompanionRecruiter : MonoBehaviour
    {
        [Header("Данные компаньона")]
        [SerializeField] private CompanionData companionData;

        [Header("Позиция «базы» (опционально)")]
        [Tooltip("Если не задано — компаньон остаётся на месте при отправке на базу.")]
        [SerializeField] private Transform basePosition;

        private CompanionController _controller;

        // ── lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _controller = GetComponent<CompanionController>();

            if (_controller == null)
                Debug.LogError("[CompanionRecruiter] На объекте нет CompanionController!", this);
        }

        // ── действия диалога ──────────────────────────────────────────────

        public void Hire()
        {
            if (companionData == null) { LogError("companionData не назначен"); return; }

            if (CompanionManager.Instance.IsCompanionHired(companionData.CompanionID))
            {
                Debug.Log("[CompanionRecruiter] Уже нанят.");
                return;
            }

            // Проверяем кошелёк
            Purse purse = GetPlayerPurse();
            if (purse == null) return;

            if (purse.GetBalance() < companionData.HireCost)
            {
                Debug.Log("[CompanionRecruiter] Недостаточно денег.");
                return;
            }

            purse.UpdateBalance(-companionData.HireCost);

            CompanionManager.Instance.HireCompanion(
                companionData.CompanionID,
                SceneManager.GetActiveScene().name,
                transform.position);

            ActivateController();
        }

        public void Dismiss()
        {
            if (companionData == null) return;

            if (!CompanionManager.Instance.IsCompanionHired(companionData.CompanionID))
            {
                Debug.Log("[CompanionRecruiter] Компаньон не нанят.");
                return;
            }

            // Менеджер внутри вызовет controller.Deactivate()
            CompanionManager.Instance.DismissCompanion(companionData.CompanionID);

            ReturnToBase();
        }

        public void Call()
        {
            if (companionData == null) return;

            if (!CompanionManager.Instance.IsCompanionHired(companionData.CompanionID))
            {
                Debug.Log("[CompanionRecruiter] Компаньон не нанят.");
                return;
            }

            if (CompanionManager.Instance.IsCompanionActive(companionData.CompanionID))
            {
                Debug.Log("[CompanionRecruiter] Уже активен.");
                return;
            }

            CompanionManager.Instance.ActivateCompanion(companionData.CompanionID);
            ActivateController();
        }

        // ── внутреннее ───────────────────────────────────────────────────

        private void ActivateController()
        {
            if (_controller == null) { LogError("CompanionController не найден"); return; }
            _controller.Activate(companionData);
        }

        private void ReturnToBase()
        {
            if (basePosition == null) return;

            transform.position = basePosition.position;
            transform.rotation = basePosition.rotation;
        }

        // ── методы для предикатов (CompanionPredicates) ───────────────────

        public bool CheckCompanionHired() =>
            companionData != null && CompanionManager.Instance.IsCompanionHired(companionData.CompanionID);

        public bool CheckCompanionActive() =>
            companionData != null && CompanionManager.Instance.IsCompanionActive(companionData.CompanionID);

        public bool CheckCanAfford()
        {
            if (companionData == null) return false;
            Purse purse = GetPlayerPurse();
            return purse != null && purse.GetBalance() >= companionData.HireCost;
        }

        // ── утилиты ──────────────────────────────────────────────────────

        private static Purse GetPlayerPurse()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) { Debug.LogError("[CompanionRecruiter] Игрок не найден!"); return null; }

            Purse purse = player.GetComponent<Purse>();
            if (purse == null) Debug.LogError("[CompanionRecruiter] У игрока нет Purse!");
            return purse;
        }

        private void LogError(string msg) =>
            Debug.LogError($"[CompanionRecruiter] {msg}", this);
    }
}