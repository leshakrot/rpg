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