using System.Collections.Generic;

namespace RPG.Dialogue
{
    /// <summary>
    /// Регистрируется через PlayerConversant.RegisterDynamicChoiceProvider() и позволяет
    /// подменить статичные children конкретной ноды на динамически сгенерированный список
    /// (например: "выбери один из активных квестов"). Без зарегистрированных провайдеров
    /// поведение диалога не меняется.
    /// </summary>
    public interface IDynamicChoiceProvider
    {
        /// <summary>
        /// true, если для currentNode нужно подменить его статичные player-children на choices.
        /// Вызывается PlayerConversant каждый раз, когда запрашивается список ответов игрока.
        /// </summary>
        bool TryGetChoices(DialogueNode currentNode, out IEnumerable<DialogueNode> choices);

        /// <summary>
        /// Вызывается для каждого зарегистрированного провайдера сразу после того, как игрок
        /// выбрал любую ноду (не только динамическую). Верните true, если chosenNode — это узел,
        /// сгенерированный этим провайдером, и вы обработали выбор (например, запомнили его).
        /// </summary>
        bool TryHandleSelection(DialogueNode chosenNode);
    }
}
