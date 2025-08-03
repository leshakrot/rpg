using System.Collections;
using UnityEngine;
using RPG.Control;
using RPG.Movement;
using RPG.UI.Crafting;

namespace RPG.Crafting
{
    public class CraftingStation : MonoBehaviour, IRaycastable
    {
        [Header("Настройки Станции")]
        [SerializeField] private string stationName = "Верстак";
        [SerializeField] private float craftingDistance = 2.5f;

        [Header("Рецепты")]
        [Tooltip("Рецепты, доступные на этой станции")]
        [SerializeField] private CraftingRecipe craftingRecipe = null;

        // Переменная для отслеживания активной корутины
        private Coroutine activeInteractionCoroutine = null;

        public string StationName => stationName;
        public CraftingRecipe CraftingRecipe => craftingRecipe;

        public CursorType GetCursorType()
        {
            return CursorType.Harvesting;
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            // Если процесс взаимодействия уже запущен, ничего не делаем
            if (activeInteractionCoroutine != null) return true;

            if (Input.GetMouseButtonDown(0))
            {
                // Запускаем корутину для обработки взаимодействия
                activeInteractionCoroutine = StartCoroutine(MoveToStationAndInteract(callingController));
            }
            return true;
        }

        /// <summary>
        /// Корутина, которая управляет перемещением к станции и открытием UI.
        /// </summary>
        private IEnumerator MoveToStationAndInteract(PlayerController callingController)
        {
            Mover mover = callingController.GetComponent<Mover>();

            // Цикл будет работать, пока игрок не окажется на нужном расстоянии
            while (Vector3.Distance(transform.position, callingController.transform.position) > craftingDistance)
            {
                // Даем команду двигаться к цели
                mover.StartMoveAction(transform.position, 1f);

                // Ждем один кадр и проверяем расстояние снова
                yield return null;
            }

            // Игрок на месте, останавливаем его и открываем UI
            mover.Cancel();

            // Проверяем, есть ли рецепты для отображения
            if (craftingRecipe != null)
            {
                CraftingUIManager.Instance.Show(this);
            }
            else
            {
                Debug.LogWarning($"На станции {stationName} не назначены рецепты!");
            }

            // Сбрасываем корутину
            activeInteractionCoroutine = null;
        }

        /// <summary>
        /// Проверяет, находится ли игрок в радиусе взаимодействия со станцией.
        /// </summary>
        public bool IsPlayerInRange(PlayerController player)
        {
            return Vector3.Distance(transform.position, player.transform.position) <= craftingDistance;
        }
    }
}