using UnityEngine;
using UnityEditor;
using RPG.Control;
using RPG.Harvesting;

namespace RPG.Editor
{
    /// <summary>
    /// Editor-утилита для автоматического добавления InteractableHighlight ко всем интерактивным объектам.
    /// </summary>
    public static class InteractableHighlightSetup
    {
        [MenuItem("Tools/Setup/Add Highlight to All Interactables")]
        public static void AddHighlightToAllInteractables()
        {
            int addedCount = 0;
            int skippedCount = 0;

            // Находим все объекты с IRaycastable в сцене
            MonoBehaviour[] allObjects = Object.FindObjectsOfType<MonoBehaviour>();

            foreach (MonoBehaviour obj in allObjects)
            {
                if (obj is IRaycastable)
                {
                    GameObject go = obj.gameObject;
                    
                    // Проверяем, есть ли уже компонент
                    if (go.GetComponent<InteractableHighlight>() == null)
                    {
                        Undo.AddComponent<InteractableHighlight>(go);
                        addedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
            }

            Debug.Log($"InteractableHighlight Setup Complete: Added to {addedCount} objects, skipped {skippedCount} objects (already had component).");
            
            if (addedCount > 0)
            {
                EditorUtility.DisplayDialog("Setup Complete", 
                    $"Added InteractableHighlight to {addedCount} objects.\n{skippedCount} objects already had the component.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Setup Complete", 
                    "No new objects needed the InteractableHighlight component.", 
                    "OK");
            }
        }

        [MenuItem("Tools/Setup/Remove Highlight from All Interactables")]
        public static void RemoveHighlightFromAllInteractables()
        {
            int removedCount = 0;

            InteractableHighlight[] highlights = Object.FindObjectsOfType<InteractableHighlight>();

            foreach (InteractableHighlight highlight in highlights)
            {
                Undo.DestroyObjectImmediate(highlight);
                removedCount++;
            }

            Debug.Log($"Removed InteractableHighlight from {removedCount} objects.");
            
            EditorUtility.DisplayDialog("Cleanup Complete", 
                $"Removed InteractableHighlight from {removedCount} objects.", 
                "OK");
        }
    }
}
