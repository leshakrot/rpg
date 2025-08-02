using UnityEngine;
using UnityEngine.UI;
using RPG.UI;
using TMPro;
using RPG.Processing;

public class RecipeUIEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private Image recipeIcon;

    private ProcessingRecipe recipe;
    private ProcessingUI parentUI;

    /// <summary>
    /// Настраивает кнопку с данными из рецепта.
    /// </summary>
    public void Setup(ProcessingRecipe recipe, ProcessingUI parentUI)
    {
        this.recipe = recipe;
        this.parentUI = parentUI;

        if (recipeNameText != null)
        {
            // Берем название из выходного предмета
            recipeNameText.text = recipe.Output.item.GetDisplayName();
        }

        if (recipeIcon != null)
        {
            // Берем иконку из выходного предмета
            recipeIcon.sprite = recipe.Output.item.GetIcon();
        }
    }

    /// <summary>
    /// Этот метод будет вызываться при нажатии на кнопку.
    /// </summary>
    public void OnClick()
    {
        parentUI.SelectRecipe(recipe);
    }
}