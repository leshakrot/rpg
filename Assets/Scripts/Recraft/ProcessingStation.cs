using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Control;
using RPG.UI;
using RPG.Movement; // <-- ДОБАВЬТЕ ЭТОТ USING

namespace RPG.Processing
{
    public class ProcessingStation : MonoBehaviour, IRaycastable
    {
        [Header("Настройки Станции")]
        [SerializeField] private string stationName = "Станция переработки";
        [SerializeField] private float processingDistance = 2.5f;

        [Header("Рецепты")]
        [Tooltip("Список рецептов, доступных на этой станции")]
        [SerializeField] private List<ProcessingRecipe> availableRecipes = new List<ProcessingRecipe>();

        // Переменная для отслеживания активной корутины, чтобы избежать двойных кликов
        private Coroutine activeInteractionCoroutine = null;

        public string StationName => stationName;
        public List<ProcessingRecipe> AvailableRecipes => availableRecipes;

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
                // Запускаем корутину, которая обработает всю логику
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
            while (Vector3.Distance(transform.position, callingController.transform.position) > processingDistance)
            {
                // Даем команду двигаться к цели. Mover сам остановится на нужном расстоянии.
                mover.StartMoveAction(transform.position, 1f);

                // Ждем один кадр и проверяем расстояние снова
                yield return null;
            }

            // Игрок на месте, останавливаем его на всякий случай и открываем UI
            mover.Cancel();
            ProcessingUI.Instance.Show(this);

            // Сбрасываем корутину, чтобы можно было снова взаимодействовать
            activeInteractionCoroutine = null;
        }
    }
}