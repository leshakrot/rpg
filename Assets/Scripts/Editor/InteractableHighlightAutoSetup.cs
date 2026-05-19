using UnityEngine;
using UnityEditor;
using RPG.Control;

namespace RPG.Editor
{
    /// <summary>
    /// Автоматически добавляет InteractableHighlight при добавлении компонентов IRaycastable.
    /// </summary>
    [InitializeOnLoad]
    public static class InteractableHighlightAutoSetup
    {
        static InteractableHighlightAutoSetup()
        {
            ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        private static void OnComponentAdded(Component component)
        {
            // Проверяем, реализует ли компонент IRaycastable
            if (component is IRaycastable)
            {
                GameObject go = component.gameObject;
                
                // Проверяем, нет ли уже InteractableHighlight
                if (go.GetComponent<InteractableHighlight>() == null)
                {
                    // Добавляем с задержкой, чтобы избежать проблем с порядком инициализации
                    EditorApplication.delayCall += () =>
                    {
                        if (go != null && go.GetComponent<InteractableHighlight>() == null)
                        {
                            Undo.AddComponent<InteractableHighlight>(go);
                            Debug.Log($"Auto-added InteractableHighlight to {go.name}");
                        }
                    };
                }
            }
        }
    }
}
