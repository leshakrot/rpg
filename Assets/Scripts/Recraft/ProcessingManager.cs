using UnityEngine;
using RPG.Control;
using GameDevTV.Inventories;
using System.Collections;
using RPG.UI.RequirementText;
using RPG.Harvesting; // <-- ВАЖНО: Добавьте этот using для доступа к HarvestBar

namespace RPG.Processing
{
    public class ProcessingManager : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Перетащите сюда объект HarvestBarHUD с вашего Canvas")]
        [SerializeField] private HarvestBar processingBar = null;

        private static ProcessingManager _instance;
        public static ProcessingManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void StartProcessing(ProcessingRecipe recipe, PlayerController player)
        {
            var inventory = player.GetComponent<Inventory>();
            if (inventory == null) return;

            int itemsInInventory = GetTotalItemCount(inventory, recipe.Input.item);
            if (itemsInInventory < recipe.Input.quantity)
            {
                RequirementTextManager.Show($"Нужно: {recipe.Input.item.GetDisplayName()} ({recipe.Input.quantity})");
                return;
            }

            StartCoroutine(ProcessCoroutine(recipe, inventory));
        }

        private IEnumerator ProcessCoroutine(ProcessingRecipe recipe, Inventory inventory)
        {
            // Забираем ресурсы
            RemoveItems(inventory, recipe.Input.item, recipe.Input.quantity);

            // Показываем полосу прогресса
            if (processingBar != null)
            {
                string processName = $"Создание: {recipe.Output.item.GetDisplayName()}";
                // ИЗМЕНЕНИЕ: Вызываем новый метод
                processingBar.StartProcessing(processName, recipe.TimeToProcess);
            }

            // Ждем (теперь это просто задержка, сам бар управляется своей корутиной)
            yield return new WaitForSeconds(recipe.TimeToProcess);

            // Выдаем готовый продукт
            bool addedSuccessfully = inventory.AddToFirstEmptySlot(recipe.Output.item, recipe.Output.quantity);
            if (!addedSuccessfully)
            {
                Debug.LogWarning($"Не удалось добавить {recipe.Output.item.GetDisplayName()} в инвентарь. Нет места!");
            }

            // ВАЖНО: Мы больше не вызываем здесь Stop(), т.к. корутина в HarvestBar сама завершится и скроет бар.
        }

        // ... (вспомогательные методы GetTotalItemCount и RemoveItems остаются без изменений) ...
        private int GetTotalItemCount(Inventory inventory, InventoryItem item)
        {
            int total = 0;
            for (int i = 0; i < inventory.GetSize(); i++)
            {
                if (inventory.GetItemInSlot(i) == item)
                {
                    total += inventory.GetNumberInSlot(i);
                }
            }
            return total;
        }

        private void RemoveItems(Inventory inventory, InventoryItem item, int quantity)
        {
            int quantityToRemove = quantity;
            for (int i = 0; i < inventory.GetSize(); i++)
            {
                if (inventory.GetItemInSlot(i) == item)
                {
                    int numberInSlot = inventory.GetNumberInSlot(i);
                    if (numberInSlot >= quantityToRemove)
                    {
                        inventory.RemoveFromSlot(i, quantityToRemove);
                        quantityToRemove = 0;
                        break;
                    }
                    else
                    {
                        inventory.RemoveFromSlot(i, numberInSlot);
                        quantityToRemove -= numberInSlot;
                    }
                }
            }
        }
    }
}