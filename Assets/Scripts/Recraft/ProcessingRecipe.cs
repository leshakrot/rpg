using UnityEngine;
using GameDevTV.Inventories;

[CreateAssetMenu(fileName = "New Processing Recipe", menuName = "RPG/Processing/Recipe")]
public class ProcessingRecipe : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public InventoryItem item;
        public int quantity;
    }

    [Header("Рецепт")]
    [Tooltip("Что требуется для создания")]
    [SerializeField] private Ingredient input;

    [Tooltip("Что получается в результате")]
    [SerializeField] private Ingredient output;

    [Tooltip("Время на переработку (в секундах)")]
    [SerializeField] private float timeToProcess = 3f;

    // Свойства для доступа к данным
    public Ingredient Input => input;
    public Ingredient Output => output;
    public float TimeToProcess => timeToProcess;
}