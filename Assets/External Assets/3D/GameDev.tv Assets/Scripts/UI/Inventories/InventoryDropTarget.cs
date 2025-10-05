using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameDevTV.Core.UI.Dragging;
using GameDevTV.Inventories;

namespace GameDevTV.UI.Inventories
{
    /// <summary>
    /// Handles spawning pickups when item dropped into the world.
    /// 
    /// Must be placed on the root canvas where items can be dragged. Will be
    /// called if dropped over empty space. 
    /// </summary>
    public class InventoryDropTarget : MonoBehaviour, IDragDestination<InventoryItem>
    {
        public void AddItems(InventoryItem item, int number)
        {
            // Проверяем, можно ли выбросить предмет
            if (item != null && !item.CanBeDropped())
            {
                return; // Предмет вернется обратно в исходный слот
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            player.GetComponent<ItemDropper>().DropItem(item, number);
        }

        public int MaxAcceptable(InventoryItem item)
        {
            // Если предмет нельзя выбросить, не принимаем его
            if (item != null && !item.CanBeDropped())
            {
                return 0;
            }
            return int.MaxValue;
        }
    }
}