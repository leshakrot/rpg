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
    public class ActionSlotUI : MonoBehaviour, IItemHolder, IDragContainer<GameDevTV.Inventories.InventoryItem>, IPointerClickHandler
    {
        // CONFIG DATA
        [SerializeField] InventoryItemIcon icon = null;
        [SerializeField] int index = 0;
        [SerializeField] Image cooldownOverlay = null;

        // CACHE
        ActionStore store;
        CooldownStore cooldownStore;
        GameObject player;

        // LIFECYCLE METHODS
        private void Awake()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            store = player.GetComponent<ActionStore>();
            cooldownStore = player.GetComponent<CooldownStore>();
            store.storeUpdated += UpdateIcon;
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

        // Обработка клика по иконке предмета
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                UseItem();
            }
        }

        // PRIVATE

        void UpdateIcon()
        {
            icon.SetItem(GetItem(), GetNumber());
        }
    }
}