using System.Collections;
using System.Collections.Generic;
using GameDevTV.Core.UI.Dragging;
using GameDevTV.Inventories;
using RPG.Abilities;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameDevTV.UI.Inventories
{
    /// <summary>
    /// The UI slot for the player action bar.
    /// </summary>
    public class ActionSlotUI : MonoBehaviour, IItemHolder, IDragContainer<GameDevTV.Inventories.InventoryItem>, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler
    {
        // CONFIG DATA
        [SerializeField] InventoryItemIcon icon = null;
        [SerializeField] int index = 0;
        [SerializeField] Image cooldownOverlay = null;

        // CACHE
        ActionStore store;
        CooldownStore cooldownStore;
        GameObject player;
        
        // Для отслеживания перетаскивания
        private bool isDragging = false;
        private Vector2 pointerDownPosition;
        private const float dragThreshold = 5f; // Минимальное расстояние для определения драга

        // LIFECYCLE METHODS
        private void Awake()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            store = player.GetComponent<ActionStore>();
            cooldownStore = player.GetComponent<CooldownStore>();
            store.storeUpdated += UpdateIcon;
            
            Debug.Log($"ActionSlotUI on {gameObject.name} - IDragSource check: {GetComponent<IDragSource<InventoryItem>>() != null}");
        }

        private void Update()
        {
            cooldownOverlay.fillAmount = cooldownStore.GetFractionRemaining(GetItem());
        }

        // PUBLIC

        public void AddItems(InventoryItem item, int number)
        {
            // Проверяем, можно ли переместить предмет в ActionSlot
            if (item != null && !item.CanBeDropped())
            {
                return; // Предмет вернется обратно в исходный слот
            }

            store.AddAction(item, index, number);
        }

        public InventoryItem GetItem()
        {
            return store.GetAction(index);
        }

        public int GetNumber()
        {
            return store.GetNumber(index);
        }

        public int MaxAcceptable(InventoryItem item)
        {
            // Если предмет нельзя выбросить, не принимаем его в ActionSlot
            if (item != null && !item.CanBeDropped())
            {
                return 0;
            }
            return store.MaxAcceptable(item, index);
        }

        public void RemoveItems(int number)
        {
            store.RemoveItems(index, number);
        }

        // Метод для использования предмета при клике
        public void UseItem()
        {
            if (store != null && player != null)
            {
                store.Use(index, player);
            }
        }

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

        // PRIVATE

        void UpdateIcon()
        {
            icon.SetItem(GetItem(), GetNumber());
        }
    }
}