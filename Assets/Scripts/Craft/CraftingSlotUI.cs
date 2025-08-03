using UnityEngine;
using UnityEngine.UI;
using RPG.Inventories;
using GameDevTV.Inventories;
using GameDevTV.UI.Inventories;

namespace RPG.UI.Crafting
{
    public class CraftingSlotUI : MonoBehaviour, IItemHolder
    {
        [SerializeField] InventoryItemIcon icon = null;
        [SerializeField] Text countText = null; // Добавляем ссылку на текст количества
        [SerializeField] Image backgroundImage = null; // Добавляем ссылку на фон

        InventoryItem item;

        public void Setup(InventoryItem item, int number)
        {
            // Store the item parameter value in the local item variable.
            this.item = item;

            // Set both the item image and the amount number on the UI.
            icon.SetItem(item, number);
        }

        /// <summary>
        /// Улучшенная настройка слота с дополнительным контролем отображения.
        /// </summary>
        public void Setup(InventoryItem item, int number, int availableAmount)
        {
            this.item = item;
            icon.SetItem(item, number);

            // Обновляем текст количества, если он есть
            if (countText != null)
            {
                countText.text = $"{availableAmount}/{number}";
                countText.color = availableAmount >= number ? Color.white : Color.red;
            }
        }

        /// <summary>
        /// Устанавливает цвет фона слота.
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
        }

        /// <summary>
        /// Устанавливает состояние слота (достаточно ресурсов или нет).
        /// </summary>
        public void SetResourceState(bool hasEnoughResources)
        {
            if (backgroundImage != null)
            {
                Color targetColor = hasEnoughResources ?
                    new Color(0.2f, 0.8f, 0.2f, 0.3f) : // Зеленый
                    new Color(0.8f, 0.2f, 0.2f, 0.3f);  // Красный

                backgroundImage.color = targetColor;
            }

            // Можно добавить эффект прозрачности для иконки
            if (icon != null)
            {
                var iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.color = hasEnoughResources ?
                        Color.white :
                        new Color(1f, 1f, 1f, 0.5f);
                }
            }
        }

        public InventoryItem GetItem()
        {
            // Return the set value in the item variable for the tooltip system
            return item;
        }
    }
}