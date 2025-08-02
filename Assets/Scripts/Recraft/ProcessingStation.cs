using System.Collections.Generic;
using UnityEngine;
using RPG.Control;
using RPG.UI; // <-- ДОБАВЬТЕ ЭТОТ USING

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

        public string StationName => stationName;
        public List<ProcessingRecipe> AvailableRecipes => availableRecipes;

        public CursorType GetCursorType()
        {
            return CursorType.Harvesting;
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Проверяем расстояние. Если игрок далеко, он сначала подойдет.
                if (Vector3.Distance(transform.position, callingController.transform.position) > processingDistance)
                {
                    callingController.GetComponent<Movement.Mover>().StartMoveAction(transform.position, 1f);
                    // Можно добавить корутину, которая откроет UI после подхода.
                }
                else
                {
                    // Игрок уже рядом, открываем UI.
                    ProcessingUI.Instance.Show(this); // <-- ИЗМЕНЕНИЕ
                }
            }
            return true;
        }
    }
}