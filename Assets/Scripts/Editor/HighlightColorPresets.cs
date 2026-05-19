using UnityEngine;
using UnityEditor;
using RPG.Control;
using RPG.Harvesting;
using RPG.Dialogue;
using RPG.Shops;
using RPG.Crafting;
using RPG.Combat;

namespace RPG.Editor
{
    /// <summary>
    /// Утилита для применения цветовых пресетов к разным типам интерактивных объектов.
    /// </summary>
    public static class HighlightColorPresets
    {
        private static readonly Color ResourceColor = new Color(0.4f, 1f, 0.4f); // Зеленый
        private static readonly Color NPCColor = new Color(0.4f, 0.8f, 1f); // Голубой
        private static readonly Color ShopColor = new Color(1f, 0.8f, 0.2f); // Золотой
        private static readonly Color ContainerColor = new Color(1f, 0.6f, 0.2f); // Оранжевый
        private static readonly Color EnemyColor = new Color(1f, 0.2f, 0.2f); // Красный
        private static readonly Color DefaultColor = new Color(1f, 0.8f, 0.4f); // Желто-оранжевый

        [MenuItem("Tools/Highlight Colors/Apply All Presets")]
        public static void ApplyAllPresets()
        {
            int totalUpdated = 0;
            
            totalUpdated += ApplyResourceColors();
            totalUpdated += ApplyNPCColors();
            totalUpdated += ApplyShopColors();
            totalUpdated += ApplyContainerColors();
            totalUpdated += ApplyEnemyColors();

            EditorUtility.DisplayDialog("Presets Applied", 
                $"Updated highlight colors for {totalUpdated} objects.", 
                "OK");
        }

        [MenuItem("Tools/Highlight Colors/Resources (Green)")]
        public static int ApplyResourceColors()
        {
            int count = 0;
            HarvestableSource[] resources = Object.FindObjectsOfType<HarvestableSource>();
            
            foreach (var resource in resources)
            {
                if (SetHighlightColor(resource.gameObject, ResourceColor, 1.5f))
                    count++;
            }
            
            Debug.Log($"Applied green highlight to {count} resources");
            return count;
        }

        [MenuItem("Tools/Highlight Colors/NPCs (Blue)")]
        public static int ApplyNPCColors()
        {
            int count = 0;
            AIConversant[] npcs = Object.FindObjectsOfType<AIConversant>();
            
            foreach (var npc in npcs)
            {
                if (SetHighlightColor(npc.gameObject, NPCColor, 1.2f))
                    count++;
            }
            
            Debug.Log($"Applied blue highlight to {count} NPCs");
            return count;
        }

        [MenuItem("Tools/Highlight Colors/Shops & Crafting (Gold)")]
        public static int ApplyShopColors()
        {
            int count = 0;
            
            Shop[] shops = Object.FindObjectsOfType<Shop>();
            foreach (var shop in shops)
            {
                if (SetHighlightColor(shop.gameObject, ShopColor, 1.8f))
                    count++;
            }
            
            CraftingStation[] stations = Object.FindObjectsOfType<CraftingStation>();
            foreach (var station in stations)
            {
                if (SetHighlightColor(station.gameObject, ShopColor, 1.8f))
                    count++;
            }
            
            Debug.Log($"Applied gold highlight to {count} shops/stations");
            return count;
        }

        [MenuItem("Tools/Highlight Colors/Containers (Orange)")]
        public static int ApplyContainerColors()
        {
            int count = 0;
            
            // Ищем ChestInventory через GetComponents, так как он в другом namespace
            MonoBehaviour[] allObjects = Object.FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in allObjects)
            {
                if (obj.GetType().Name == "ChestInventory")
                {
                    if (SetHighlightColor(obj.gameObject, ContainerColor, 1.5f))
                        count++;
                }
            }
            
            Debug.Log($"Applied orange highlight to {count} containers");
            return count;
        }

        [MenuItem("Tools/Highlight Colors/Enemies (Red)")]
        public static int ApplyEnemyColors()
        {
            int count = 0;
            CombatTarget[] enemies = Object.FindObjectsOfType<CombatTarget>();
            
            foreach (var enemy in enemies)
            {
                if (SetHighlightColor(enemy.gameObject, EnemyColor, 2.0f))
                    count++;
            }
            
            Debug.Log($"Applied red highlight to {count} enemies");
            return count;
        }

        [MenuItem("Tools/Highlight Colors/Reset to Default")]
        public static void ResetToDefault()
        {
            int count = 0;
            InteractableHighlight[] highlights = Object.FindObjectsOfType<InteractableHighlight>();
            
            foreach (var highlight in highlights)
            {
                if (SetHighlightColor(highlight.gameObject, DefaultColor, 1.5f))
                    count++;
            }
            
            Debug.Log($"Reset {count} objects to default color");
            EditorUtility.DisplayDialog("Reset Complete", 
                $"Reset {count} objects to default highlight color.", 
                "OK");
        }

        private static bool SetHighlightColor(GameObject go, Color color, float intensity)
        {
            InteractableHighlight highlight = go.GetComponent<InteractableHighlight>();
            if (highlight == null) return false;

            SerializedObject so = new SerializedObject(highlight);
            so.FindProperty("highlightColor").colorValue = color;
            so.FindProperty("highlightIntensity").floatValue = intensity;
            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(highlight);
            return true;
        }
    }
}
