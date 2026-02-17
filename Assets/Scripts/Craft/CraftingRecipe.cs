using UnityEngine;
using RPG.Inventories;
using GameDevTV.Inventories;

namespace RPG.Crafting
{
    [CreateAssetMenu(menuName = "Crafting/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [SerializeField] Recipes[] recipes;

        [System.Serializable]
        public class Recipes
        {
            public InventoryItem item;
            public Ingredients[] ingredients;

            [Tooltip("Время крафта в секундах. 0 = мгновенный крафт.")]
            [Min(0f)]
            public float craftingTime = 1.5f;
        }

        [System.Serializable]
        public class Ingredients
        {
            public InventoryItem item;
            public int number;
        }

        public Recipes[] GetCraftingRecipes()
        {
            return recipes;
        }
    }
}
