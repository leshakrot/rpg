using UnityEngine;
using RPG.Control;

namespace RPG.Companions
{
    /// <summary>
    /// Клик по дрессировщику: игрок подходит и открывается окно прокачки (как у Shop).
    /// Вешается на того же NPC, что и AnimalTrainer.
    ///
    /// Если у NPC уже есть компонент диалога, реагирующий на клик (AIConversant и т.п.), НЕ вешай этот компонент —
    /// два IRaycastable на одном объекте будут конкурировать, сработает только первый. В этом случае вызывай
    /// AnimalTrainer.OpenWindow() из ноды диалога.
    /// </summary>
    [RequireComponent(typeof(AnimalTrainer))]
    public class AnimalTrainerInteractable : InteractableObject
    {
        [SerializeField] private AnimalTrainer trainer;
        [Tooltip("Какой курсор показывать при наведении. Если для типа нет текстуры в PlayerController — будет курсор по умолчанию.")]
        [SerializeField] private CursorType cursorType = CursorType.Shop;

        private void Awake()
        {
            if (trainer == null) trainer = GetComponent<AnimalTrainer>();
        }

        public override CursorType GetCursorType() => cursorType;

        protected override void OnInteract(PlayerController callingController)
        {
            if (trainer != null) trainer.OpenWindow();
        }
    }
}
