using UnityEngine;
using RPG.Combat;
using GameDevTV.Inventories;

namespace RPG.Harvesting
{
    /// <summary>
    /// Компонент для управления визуальным отображением инструментов добычи
    /// Прикрепляется к игроку
    /// </summary>
    public class HarvestingToolVisualizer : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private Transform rightHandTransform = null;
        [SerializeField] private Transform leftHandTransform = null;

        // Приватные поля
        private GameObject currentToolInstance = null;
        private GameObject hiddenWeapon = null;
        private bool isToolActive = false;

        private void Awake()
        {
            // Если не заданы трансформы рук, попробуем найти их автоматически
            if (rightHandTransform == null || leftHandTransform == null)
            {
                var fighter = GetComponent<Fighter>();
                if (fighter != null)
                {
                    rightHandTransform = fighter.rightHandTransform;
                    leftHandTransform = fighter.leftHandTransform;
                }

                var playerFighter = GetComponent<PlayerFighter>();
                if (playerFighter != null)
                {
                    rightHandTransform = playerFighter.rightHandTransform;
                    leftHandTransform = playerFighter.leftHandTransform;
                }
            }
        }

        /// <summary>
        /// Показывает инструмент добычи в руке игрока
        /// </summary>
        /// <param name="tool">Инструмент для отображения</param>
        public void ShowHarvestingTool(HarvestingTool tool)
        {
            if (tool == null || tool.ToolPrefab == null)
            {
                Debug.LogWarning("HarvestingToolVisualizer: инструмент или его префаб не найден");
                return;
            }

            if (isToolActive)
            {
                Debug.LogWarning("HarvestingToolVisualizer: инструмент уже активен");
                return;
            }

            // Скрываем текущее оружие
            HideCurrentWeapon();

            // Создаём инструмент
            Transform parentTransform = GetToolParentTransform(tool.ToolType);
            if (parentTransform != null)
            {
                currentToolInstance = Instantiate(tool.ToolPrefab, parentTransform);
                currentToolInstance.transform.localPosition = Vector3.zero;
                currentToolInstance.transform.localRotation = Quaternion.identity;
                
                isToolActive = true;
                
                Debug.Log($"HarvestingToolVisualizer: показан инструмент {tool.name}");
            }
            else
            {
                Debug.LogError("HarvestingToolVisualizer: не найден трансформ для размещения инструмента");
            }
        }

        /// <summary>
        /// Скрывает инструмент добычи и восстанавливает оружие
        /// </summary>
        public void HideHarvestingTool()
        {
            if (!isToolActive)
            {
                return;
            }

            // Удаляем инструмент
            if (currentToolInstance != null)
            {
                DestroyImmediate(currentToolInstance);
                currentToolInstance = null;
            }

            // Восстанавливаем оружие
            ShowCurrentWeapon();

            isToolActive = false;
            
            Debug.Log("HarvestingToolVisualizer: инструмент скрыт, оружие восстановлено");
        }

        /// <summary>
        /// Определяет в какую руку поместить инструмент в зависимости от его типа
        /// </summary>
        private Transform GetToolParentTransform(HarvestingToolType toolType)
        {
            return toolType switch
            {
                HarvestingToolType.Axe => rightHandTransform,      // Топор в правую руку
                HarvestingToolType.Pickaxe => rightHandTransform,  // Кирка в правую руку
                HarvestingToolType.Sickle => rightHandTransform,   // Серп в правую руку
                HarvestingToolType.Skinning => rightHandTransform, // Нож в правую руку
                _ => rightHandTransform                             // По умолчанию в правую
            };
        }

        /// <summary>
        /// Скрывает текущее экипированное оружие
        /// </summary>
        private void HideCurrentWeapon()
        {
            // Ищем активное оружие в правой руке
            if (rightHandTransform != null && rightHandTransform.childCount > 0)
            {
                for (int i = 0; i < rightHandTransform.childCount; i++)
                {
                    GameObject child = rightHandTransform.GetChild(i).gameObject;
                    if (child.activeInHierarchy)
                    {
                        hiddenWeapon = child;
                        child.SetActive(false);
                        Debug.Log($"HarvestingToolVisualizer: скрыто оружие {child.name}");
                        break; // Скрываем только первое активное оружие
                    }
                }
            }

            // Также проверяем левую руку (для щитов, двуручного оружия и т.д.)
            if (leftHandTransform != null && leftHandTransform.childCount > 0)
            {
                for (int i = 0; i < leftHandTransform.childCount; i++)
                {
                    GameObject child = leftHandTransform.GetChild(i).gameObject;
                    if (child.activeInHierarchy)
                    {
                        // Если уже есть скрытое оружие, не перезаписываем
                        if (hiddenWeapon == null)
                        {
                            hiddenWeapon = child;
                        }
                        child.SetActive(false);
                        Debug.Log($"HarvestingToolVisualizer: скрыт предмет в левой руке {child.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Восстанавливает ранее скрытое оружие
        /// </summary>
        private void ShowCurrentWeapon()
        {
            // Восстанавливаем все скрытые предметы в правой руке
            if (rightHandTransform != null)
            {
                for (int i = 0; i < rightHandTransform.childCount; i++)
                {
                    GameObject child = rightHandTransform.GetChild(i).gameObject;
                    if (!child.activeInHierarchy && child != currentToolInstance)
                    {
                        child.SetActive(true);
                        Debug.Log($"HarvestingToolVisualizer: восстановлено оружие {child.name}");
                    }
                }
            }

            // Восстанавливаем все скрытые предметы в левой руке
            if (leftHandTransform != null)
            {
                for (int i = 0; i < leftHandTransform.childCount; i++)
                {
                    GameObject child = leftHandTransform.GetChild(i).gameObject;
                    if (!child.activeInHierarchy && child != currentToolInstance)
                    {
                        child.SetActive(true);
                        Debug.Log($"HarvestingToolVisualizer: восстановлен предмет в левой руке {child.name}");
                    }
                }
            }

            hiddenWeapon = null;
        }

        /// <summary>
        /// Проверка, активен ли сейчас инструмент добычи
        /// </summary>
        public bool IsToolActive => isToolActive;

        /// <summary>
        /// Получение текущего активного инструмента (для отладки)
        /// </summary>
        public GameObject GetCurrentToolInstance() => currentToolInstance;
    }
}