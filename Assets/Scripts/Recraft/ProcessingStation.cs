using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Control;
using RPG.UI;
using RPG.Movement;

namespace RPG.Processing
{
    public class ProcessingStation : InteractableObject
    {
        [Header("Настройки станции")]
        [SerializeField] private string stationName = "Станция переработки";

        [Header("Рецепты")]
        [Tooltip("Список рецептов, доступных на этой станции")]
        [SerializeField] private List<ProcessingRecipe> availableRecipes = new List<ProcessingRecipe>();

        public string StationName => stationName;
        public List<ProcessingRecipe> AvailableRecipes => availableRecipes;

        public override CursorType GetCursorType()
        {
            return CursorType.Harvesting;
        }

        protected override void OnInteract(PlayerController callingController)
        {
            ProcessingUI.Instance.Show(this);
        }
    }
}