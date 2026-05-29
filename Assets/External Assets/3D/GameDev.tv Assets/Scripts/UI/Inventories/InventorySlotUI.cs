using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameDevTV.Inventories;
using GameDevTV.Core.UI.Dragging;

namespace GameDevTV.UI.Inventories
{
    public class InventorySlotUI : MonoBehaviour, IItemHolder, IDragContainer<InventoryItem>, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler
    {
        // CONFIG DATA
        [SerializeField] InventoryItemIcon icon = null;

        // STATE
        int index;
        InventoryItem item;
        Inventory inventory;
        
        // Для отслеживания перетаскивания
        private bool isDragging = false;
        private Vector2 pointerDownPosition;
        private const float dragThreshold = 5f; // Минимальное расстояние для определения драга

        // PUBLIC

        public void Setup(Inventory inventory, int index)
        {
            this.inventory = inventory;
            this.index = index;
            icon.SetItem(inventory.GetItemInSlot(index), inventory.GetNumberInSlot(index));
        }

        public int MaxAcceptable(InventoryItem item)
        {
            var existingItem = inventory.GetItemInSlot(index);

            // Слот занят другим предметом — своп обработает фреймворк
            if (existingItem != null && !object.ReferenceEquals(existingItem, item))
            {
                return 0;
            }

            // Слот занят тем же предметом — принимаем только если стекируемый
            if (existingItem != null && object.ReferenceEquals(existingItem, item))
            {
                return item.IsStackable() ? int.MaxValue : 0;
            }

            // Слот пустой
            return int.MaxValue;
        }

        public void AddItems(InventoryItem item, int number)
        {
            inventory.AddItemToSlot(index, item, number);
        }

        public InventoryItem GetItem()
        {
            return inventory.GetItemInSlot(index);
        }

        public int GetNumber()
        {
            return inventory.GetNumberInSlot(index);
        }

        public void RemoveItems(int number)
        {
            inventory.RemoveFromSlot(index, number);
        }
        
        // PRIVATE
        
        // Обработка нажатия мыши
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isDragging = false;
                pointerDownPosition = eventData.position;
            }
        }
        
        // Обработка начала перетаскивания
        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
        }
        
        // Обработка отпускания мыши
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Проверяем, было ли перетаскивание
                float distance = Vector2.Distance(pointerDownPosition, eventData.position);
                
                // Если мышь не сдвинулась больше порога и не было драга - используем предмет
                if (distance < dragThreshold && !isDragging)
                {
                    UseItem();
                }
                
                isDragging = false;
            }
        }
        
        // Метод для использования предмета
        private void UseItem()
        {
            InventoryItem currentItem = GetItem();
            
            // Проверяем, что предмет существует и является ActionItem
            if (currentItem != null && currentItem is ActionItem actionItem)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    // Используем предмет
                    bool wasUsed = actionItem.Use(player);
                    
                    // Если предмет был использован и он расходуемый - удаляем один экземпляр
                    if (wasUsed && actionItem.isConsumable())
                    {
                        RemoveItems(1);
                    }
                }
            }
        }
    }
}
