using UnityEngine;
using UnityEditor;
using RPG.Control;

namespace RPG.Editor
{
    /// <summary>
    /// Отладочная утилита для диагностики проблем с материалами подсветки.
    /// </summary>
    public static class HighlightMaterialDebugger
    {
        [MenuItem("Tools/Debug/Analyze Selected Object Materials")]
        public static void AnalyzeSelectedObjectMaterials()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject first.", "OK");
                return;
            }

            string report = $"=== MATERIAL ANALYSIS: {selected.name} ===\n\n";

            InteractableHighlight highlight = selected.GetComponent<InteractableHighlight>();
            if (highlight == null)
            {
                report += "⚠ No InteractableHighlight component found!\n";
            }
            else
            {
                report += "✓ InteractableHighlight component found\n\n";
            }

            IRaycastable raycastable = selected.GetComponent<IRaycastable>();
            if (raycastable == null)
            {
                report += "⚠ No IRaycastable component found!\n";
            }
            else
            {
                report += $"✓ IRaycastable component found: {raycastable.GetType().Name}\n\n";
            }

            Renderer[] renderers = selected.GetComponentsInChildren<Renderer>();
            report += $"Renderers found: {renderers.Length}\n\n";

            int materialIndex = 0;
            foreach (Renderer renderer in renderers)
            {
                report += $"--- Renderer: {renderer.gameObject.name} ({renderer.GetType().Name}) ---\n";
                
                foreach (Material mat in renderer.sharedMaterials)
                {
                    materialIndex++;
                    if (mat == null)
                    {
                        report += $"  Material {materialIndex}: NULL\n";
                        continue;
                    }

                    report += $"  Material {materialIndex}: {mat.name}\n";
                    report += $"    Shader: {mat.shader.name}\n";

                    // Проверяем _EmissionColor
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        Color emissionColor = mat.GetColor("_EmissionColor");
                        bool emissionEnabled = mat.IsKeywordEnabled("_EMISSION");
                        report += $"    ✓ _EmissionColor: {emissionColor} (Enabled: {emissionEnabled})\n";
                    }
                    else
                    {
                        report += $"    ✗ _EmissionColor: NOT SUPPORTED\n";
                    }

                    // Проверяем _EmissiveColor (URP/HDRP)
                    if (mat.HasProperty("_EmissiveColor"))
                    {
                        Color emissiveColor = mat.GetColor("_EmissiveColor");
                        report += $"    ✓ _EmissiveColor: {emissiveColor}\n";
                    }

                    // Проверяем Color properties
                    bool hasColorProperty = false;
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color baseColor = mat.GetColor("_BaseColor");
                        report += $"    ✓ _BaseColor: {baseColor}\n";
                        hasColorProperty = true;
                    }
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.GetColor("_Color");
                        report += $"    ✓ _Color: {color}\n";
                        hasColorProperty = true;
                    }
                    if (mat.HasProperty("_MainColor"))
                    {
                        Color mainColor = mat.GetColor("_MainColor");
                        report += $"    ✓ _MainColor: {mainColor}\n";
                        hasColorProperty = true;
                    }

                    if (!hasColorProperty)
                    {
                        report += $"    ✗ No Color property found\n";
                    }

                    // Рекомендация по режиму
                    bool supportsEmission = mat.HasProperty("_EmissionColor");
                    if (supportsEmission && hasColorProperty)
                    {
                        report += $"    💡 Recommended Mode: EmissionOrColorTint\n";
                    }
                    else if (supportsEmission)
                    {
                        report += $"    💡 Recommended Mode: Emission\n";
                    }
                    else if (hasColorProperty)
                    {
                        report += $"    💡 Recommended Mode: ColorTint\n";
                    }
                    else
                    {
                        report += $"    ⚠ No highlight mode supported!\n";
                    }

                    // Проверяем другие emission свойства
                    if (mat.HasProperty("_EmissionMap"))
                    {
                        report += $"    • Has _EmissionMap\n";
                    }

                    // Проверяем render queue
                    report += $"    Render Queue: {mat.renderQueue}\n";

                    // Проверяем keywords
                    string[] keywords = mat.shaderKeywords;
                    if (keywords.Length > 0)
                    {
                        report += $"    Keywords: {string.Join(", ", keywords)}\n";
                    }

                    report += "\n";
                }
            }

            Debug.Log(report);
            EditorUtility.DisplayDialog("Material Analysis", 
                $"Analysis complete for '{selected.name}'.\nCheck Console for detailed report.", 
                "OK");
        }

        [MenuItem("Tools/Debug/Test Highlight on Selected")]
        public static void TestHighlightOnSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject first.", "OK");
                return;
            }

            InteractableHighlight highlight = selected.GetComponent<InteractableHighlight>();
            if (highlight == null)
            {
                EditorUtility.DisplayDialog("No Component", 
                    "Selected object doesn't have InteractableHighlight component.", 
                    "OK");
                return;
            }

            // Включаем подсветку
            highlight.EnableHighlight();
            Debug.Log($"Highlight ENABLED on {selected.name}");

            // Через 2 секунды выключаем
            EditorApplication.delayCall += () =>
            {
                System.Threading.Thread.Sleep(2000);
                EditorApplication.delayCall += () =>
                {
                    if (highlight != null)
                    {
                        highlight.DisableHighlight();
                        Debug.Log($"Highlight DISABLED on {selected.name}");
                    }
                };
            };
        }

        [MenuItem("Tools/Debug/Fix Material Emission Settings")]
        public static void FixMaterialEmissionSettings()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject first.", "OK");
                return;
            }

            int fixedCount = 0;
            Renderer[] renderers = selected.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.HasProperty("_EmissionColor"))
                    {
                        // Включаем emission в материале
                        mat.EnableKeyword("_EMISSION");
                        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                        
                        // Устанавливаем минимальное значение emission
                        if (mat.GetColor("_EmissionColor") == Color.black)
                        {
                            mat.SetColor("_EmissionColor", new Color(0.01f, 0.01f, 0.01f));
                        }

                        EditorUtility.SetDirty(mat);
                        fixedCount++;
                    }
                }
            }

            Debug.Log($"Fixed emission settings for {fixedCount} materials on {selected.name}");
            EditorUtility.DisplayDialog("Fix Complete", 
                $"Fixed emission settings for {fixedCount} materials.", 
                "OK");
        }
    }
}
