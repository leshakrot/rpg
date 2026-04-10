using UnityEngine;
using RPG.Dialogue;
using RPG.Control;
using RPG.Inventories;
using UnityEngine.SceneManagement;

namespace RPG.Companions
{
    /// <summary>
    /// Компонент для найма компаньона - добавляется на самого NPC-компаньона
    /// Компаньон нанимает сам себя через диалог
    /// </summary>
    public class CompanionRecruiter : MonoBehaviour
    {
        [Header("Настройки компаньона")]
        [SerializeField] private CompanionData companionData;
        
        [Header("Позиция базы")]
        [Tooltip("Если не указано, используется текущая позиция")]
        [SerializeField] private Transform basePosition;
        
        [Header("Диалоговые действия")]
        [SerializeField] private string hireActionID = "hire_companion";
        [SerializeField] private string dismissActionID = "dismiss_companion";
        [SerializeField] private string callActionID = "call_companion";

        private AIConversant _conversant;
        private DialogueTrigger _dialogueTrigger;

        private void Awake()
        {
            _conversant = GetComponent<AIConversant>();
            _dialogueTrigger = GetComponent<DialogueTrigger>();
            
            if (_dialogueTrigger == null)
            {
                _dialogueTrigger = gameObject.AddComponent<DialogueTrigger>();
            }

            // Регистрируем обработчики действий
            _dialogueTrigger.RegisterAction(hireActionID, OnHireCompanion);
            _dialogueTrigger.RegisterAction(dismissActionID, OnDismissCompanion);
            _dialogueTrigger.RegisterAction(callActionID, OnCallCompanion);
        }

        /// <summary>
        /// Нанять компаньона
        /// </summary>
        private void OnHireCompanion()
        {
            if (companionData == null)
            {
                Debug.LogError("CompanionRecruiter: CompanionData не назначен!");
                return;
            }

            // Проверяем уже нанят ли
            if (CompanionManager.Instance.IsCompanionHired(companionData.CompanionID))
            {
                Debug.Log("Компаньон уже нанят");
                return;
            }

            // Проверяем деньги
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Purse purse = player.GetComponent<Purse>();
            if (purse == null)
            {
                Debug.LogError("У игрока нет компонента Purse!");
                return;
            }

            if (purse.GetBalance() < companionData.HireCost)
            {
                Debug.Log("Недостаточно денег для найма");
                return;
            }

            // Списываем деньги
            purse.UpdateBalance(-companionData.HireCost);

            // Нанимаем компаньона
            string currentScene = SceneManager.GetActiveScene().name;
            CompanionManager.Instance.HireCompanion(
                companionData.CompanionID,
                currentScene,
                transform.position
            );

            // Спавним компаньона
            SpawnCompanion();

            Debug.Log($"Компаньон {companionData.CompanionName} нанят!");
        }

        /// <summary>
        /// Отправить компаньона на базу
        /// </summary>
        private void OnDismissCompanion()
        {
            if (companionData == null) return;

            if (!CompanionManager.Instance.IsCompanionHired(companionData.CompanionID))
            {
                Debug.Log("Компаньон не нанят");
                return;
            }

            CompanionManager.Instance.DismissCompanion(companionData.CompanionID);
            ReturnToBase();
            Debug.Log($"Компаньон {companionData.CompanionName} отправлен на базу");
        }

        /// <summary>
        /// Вызвать компаньона обратно
        /// </summary>
        private void OnCallCompanion()
        {
            if (companionData == null) return;

            if (!CompanionManager.Instance.IsCompanionHired(companionData.CompanionID))
            {
                Debug.Log("Компаньон не нанят");
                return;
            }

            CompanionManager.Instance.ActivateCompanion(companionData.CompanionID);
            SpawnCompanion();
            
            Debug.Log($"Компаньон {companionData.CompanionName} вызван!");
        }

        /// <summary>
        /// Активировать компаньона (этот же объект становится активным компаньоном)
        /// </summary>
        private void SpawnCompanion()
        {
            if (companionData == null)
            {
                Debug.LogError("CompanionRecruiter: CompanionData не назначен!");
                return;
            }

            // Получаем или добавляем контроллер
            CompanionController controller = GetComponent<CompanionController>();
            if (controller == null)
            {
                Debug.LogError("CompanionRecruiter: На компаньоне нет CompanionController!");
                return;
            }
            
            // Активируем контроллер
            controller.enabled = true;
            controller.Initialize(companionData);
            
            Debug.Log($"Компаньон {companionData.CompanionName} активирован!");
        }
        
        /// <summary>
        /// Вернуться на базу (отключить режим компаньона)
        /// </summary>
        private void ReturnToBase()
        {
            CompanionController controller = GetComponent<CompanionController>();
            if (controller != null)
            {
                controller.Deactivate();
                controller.enabled = false;
            }
            
            // Возвращаемся на базу если указана
            if (basePosition != null)
            {
                transform.position = basePosition.position;
                transform.rotation = basePosition.rotation;
            }
            
            Debug.Log($"Компаньон вернулся на базу");
        }

        /// <summary>
        /// Проверка условий для диалога (используется в DialogueNode)
        /// </summary>
        public bool CheckCompanionHired()
        {
            return companionData != null && CompanionManager.Instance.IsCompanionHired(companionData.CompanionID);
        }

        public bool CheckCompanionActive()
        {
            return companionData != null && CompanionManager.Instance.IsCompanionActive(companionData.CompanionID);
        }

        public bool CheckCanAfford()
        {
            if (companionData == null) return false;
            
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return false;

            Purse purse = player.GetComponent<Purse>();
            if (purse == null) return false;

            return purse.GetBalance() >= companionData.HireCost;
        }
    }
}
