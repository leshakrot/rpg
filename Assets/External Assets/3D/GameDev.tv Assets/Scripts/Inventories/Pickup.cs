using UnityEngine;

namespace GameDevTV.Inventories
{
    /// <summary>
    /// To be placed at the root of a Pickup prefab. Contains the data about the
    /// pickup such as the type of item and the number.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        // STATE
        InventoryItem item;
        int number = 1;

        // CACHED REFERENCE
        Inventory inventory;
        ActionStore actionStore;

        // LIFECYCLE METHODS

        private void Awake()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            inventory = player.GetComponent<Inventory>();
            actionStore = player.GetComponent<ActionStore>();
        }

        // PUBLIC

        /// <summary>
        /// Set the vital data after creating the prefab.
        /// </summary>
        /// <param name="item">The type of item this prefab represents.</param>
        /// <param name="number">The number of items represented.</param>
        public void Setup(InventoryItem item, int number)
        {
            this.item = item;
            if (!item.IsStackable())
            {
                number = 1;
            }
            this.number = number;
        }

        public InventoryItem GetItem()
        {
            return item;
        }

        public int GetNumber()
        {
            return number;
        }

	    public void PickupItem()
	    {
		    Debug.Log($"[Pickup] Поднятие предмета: {item?.name}, количество: {number}");
		    
		    // Проверяем, является ли предмет ActionItem и есть ли он уже в ActionStore
		    bool addedToActionSlot = false;
		    if (item is ActionItem)
		    {
		        // Проходимся по всем слотам ActionStore
		        for (int i = 0; i < 8; i++) // Предполагаем, что ActionStore имеет 8 слотов
		        {
		            ActionItem actionItem = actionStore.GetAction(i);
		            
		            // Если нашли такой же предмет в ActionStore
		            if (object.ReferenceEquals(actionItem, item))
		            {
		                Debug.Log($"[Pickup] Добавление предмета {item.name} в ActionSlot {i}");
		                actionStore.AddAction(item, i, number);
		                addedToActionSlot = true;
		                break;
		            }
		        }
		    }
		    
		    // Если предмет не был добавлен в ActionSlot, добавляем его в инвентарь
		    if (!addedToActionSlot)
		    {
		        bool foundSlot = inventory.AddToFirstEmptySlot(item, number);
		        if (!foundSlot)
		        {
		            return; // не уничтожаем объект, если нет места в инвентаре
		        }
		    }
		    
		    // Уничтожаем объект после успешного подбора
		    Destroy(gameObject);
	    }

        public bool CanBePickedUp()
        {
            if (item is ActionItem actionItem)
            {
                // Если предмет уже есть в ActionStore — место есть
                for (int i = 0; i < 6; i++)
                {
                    if (object.ReferenceEquals(actionStore.GetAction(i), item))
                        return true;
                }
            }
            return inventory.HasSpaceFor(item);
        }
    }
}