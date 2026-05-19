using UnityEngine;
using UnityEditor;
using RPG.Control;
using System.Linq;

namespace RPG.Editor
{
    /// <summary>
    /// Валидатор для проверки корректности настройки системы подсветки.
    /// </summary>
    public static class HighlightSystemValidator
    {
        [MenuItem("Tools/Validate/Check Highlight System")]
        public static void ValidateHighlightSystem()
        {
            int totalInteractables = 0;
            int withHighlight = 0;
            int withoutHighlight = 0;
            int withoutRenderer = 0;
            int withoutEmissionSupport = 0;
            int disabledHighlight = 0;

            MonoBehaviour[] allObjects = Object.FindObjectsOfType<MonoBehaviour>(true); // включая неактивные

            foreach (MonoBehaviour obj in allObjects)
            {
                if (obj is IRaycastable)
                {
                    totalInteractables++;
                    GameObject go = obj.gameObject;

                    // Проверяем наличие InteractableHighlight
                    InteractableHighlight highlight = go.GetComponent<InteractableHighlight>();
                    if (highlight != null)
                    {
                        withHighlight++;

                        if (!highlight.enabled)
                        {
                            disabledHighlight++;
                            Debug.LogWarning($"{go.name}: InteractableHighlight is DISABLED!", go);
                        }

                        // Проверяем наличие Renderer
                        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                        if (renderers.Length == 0)
                        {
                            withoutRenderer++;
                            Debug.LogWarning($"{go.name}: Has InteractableHighlight but no Renderer found!", go);
                        }
                        else
                        {
                            // Проверяем поддержку Emission
                            bool hasEmissionSupport = false;
                            int materialCount = 0;
                            int emissionSupportCount = 0;

                            foreach (Renderer renderer in renderers)
                            {
                                foreach (Material mat in renderer.sharedMaterials)
                                {
                                    if (mat != null)
                                    {
                                        materialCount++;
                                        if (mat.HasProperty("_EmissionColor"))
                                        {
                                            hasEmissionSupport = true;
                                            emissionSupportCount++;
                                        }
                                    }
                                }
                            }

                            if (!hasEmissionSupport)
                            {
                                withoutEmissionSupport++;
                                Debug.LogWarning($"{go.name}: NO materials support Emission (_EmissionColor property)", go);
                            }
                            else if (emissionSupportCount < materialCount)
                            {
                                Debug.LogWarning($"{go.name}: Only {emissionSupportCount}/{materialCount} materials support Emission", go);
                            }
                        }
                    }
                    else
                    {
                        withoutHighlight++;
                        Debug.Log($"{go.name}: IRaycastable ({obj.GetType().Name}) without InteractableHighlight", go);
                    }
                }
            }

            string report = $"=== HIGHLIGHT SYSTEM VALIDATION ===\n\n" +
                           $"Total Interactables (IRaycastable): {totalInteractables}\n" +
                           $"With InteractableHighlight: {withHighlight}\n" +
                           $"  - Disabled: {disabledHighlight}\n" +
                           $"Without InteractableHighlight: {withoutHighlight}\n" +
                           $"Without Renderer: {withoutRenderer}\n" +
                           $"Without Emission Support: {withoutEmissionSupport}\n\n";

            if (withoutHighlight > 0)
            {
                report += $"⚠ {withoutHighlight} objects need InteractableHighlight.\n" +
                         "Use: Tools > Setup > Add Highlight to All Interactables\n\n";
            }

            if (disabledHighlight > 0)
            {
                report += $"⚠ {disabledHighlight} objects have DISABLED InteractableHighlight (check console)\n\n";
            }

            if (withoutRenderer > 0)
            {
                report += $"⚠ {withoutRenderer} objects have no Renderer (check console for details)\n\n";
            }

            if (withoutEmissionSupport > 0)
            {
                report += $"⚠ {withoutEmissionSupport} objects have materials without Emission support\n" +
                         "Use: Tools > Debug > Fix Material Emission Settings\n" +
                         "Or change shader to Standard/URP Lit/HDRP Lit\n\n";
            }

            if (withoutHighlight == 0 && withoutRenderer == 0 && withoutEmissionSupport == 0 && disabledHighlight == 0)
            {
                report += "✓ All systems configured correctly!";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Highlight System Validation", report, "OK");
        }

        [MenuItem("Tools/Validate/List All Interactable Types")]
        public static void ListInteractableTypes()
        {
            MonoBehaviour[] allObjects = Object.FindObjectsOfType<MonoBehaviour>();
            var typeGroups = allObjects
                .Where(obj => obj is IRaycastable)
                .GroupBy(obj => obj.GetType().Name)
                .OrderBy(g => g.Key);

            string report = "=== INTERACTABLE TYPES IN SCENE ===\n\n";

            foreach (var group in typeGroups)
            {
                report += $"{group.Key}: {group.Count()}\n";
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Interactable Types", report, "OK");
        }
    }
}
