#if UNITY_EDITOR
using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Harvesting
{
    public class CreateExampleTools : MonoBehaviour
    {
        [ContextMenu("Создать примеры топоров")]
        public void CreateAxes()
        {
            // T1 - Деревянный топор
            var woodenAxe = CreateTool(
                "Деревянный топор",
                "WoodenAxe",
                HarvestingToolType.Axe,
                1, // tier
                1.0f, // speed multiplier
                1 // max resource tier
            );
            
            // T2 - Каменный топор
            var stoneAxe = CreateTool(
                "Каменный топор",
                "StoneAxe",
                HarvestingToolType.Axe,
                2, // tier
                1.2f, // speed multiplier
                2 // max resource tier
            );
            
            // T3 - Железный топор
            var ironAxe = CreateTool(
                "Железный топор",
                "IronAxe",
                HarvestingToolType.Axe,
                3, // tier
                1.4f, // speed multiplier
                3 // max resource tier
            );
            
            // T4 - Стальной топор
            var steelAxe = CreateTool(
                "Стальной топор",
                "SteelAxe",
                HarvestingToolType.Axe,
                4, // tier
                1.6f, // speed multiplier
                4 // max resource tier
            );
            
            // T5 - Мистический топор
            var mysticAxe = CreateTool(
                "Мистический топор",
                "MysticAxe",
                HarvestingToolType.Axe,
                5, // tier
                1.8f, // speed multiplier
                5 // max resource tier
            );
            
            Debug.Log("Созданы топоры T1-T5");
        }
        
        [ContextMenu("Создать примеры кирок")]
        public void CreatePickaxes()
        {
            // T1 - Деревянная кирка
            var woodenPickaxe = CreateTool(
                "Деревянная кирка",
                "WoodenPickaxe",
                HarvestingToolType.Pickaxe,
                1, // tier
                1.0f, // speed multiplier
                1 // max resource tier
            );
            
            // T2 - Каменная кирка
            var stonePickaxe = CreateTool(
                "Каменная кирка",
                "StonePickaxe",
                HarvestingToolType.Pickaxe,
                2, // tier
                1.2f, // speed multiplier
                2 // max resource tier
            );
            
            // T3 - Железная кирка
            var ironPickaxe = CreateTool(
                "Железная кирка",
                "IronPickaxe",
                HarvestingToolType.Pickaxe,
                3, // tier
                1.4f, // speed multiplier
                3 // max resource tier
            );
            
            Debug.Log("Созданы кирки T1-T3");
        }
        
        [ContextMenu("Создать все типы инструментов")]
        public void CreateAllTools()
        {
            CreateAxes();
            CreatePickaxes();
            
            // Серпы
            var woodenSickle = CreateTool(
                "Деревянный серп",
                "WoodenSickle",
                HarvestingToolType.Sickle,
                1, // tier
                1.0f, // speed multiplier
                1 // max resource tier
            );
            
            var ironSickle = CreateTool(
                "Железный серп",
                "IronSickle",
                HarvestingToolType.Sickle,
                2, // tier
                1.3f, // speed multiplier
                2 // max resource tier
            );
            
            // Ножи для снятия шкур
            var skinningKnife = CreateTool(
                "Нож для снятия шкур",
                "SkinningKnife",
                HarvestingToolType.Skinning,
                1, // tier
                1.0f, // speed multiplier
                1 // max resource tier
            );
            
            Debug.Log("Созданы все типы инструментов");
        }
        
        private HarvestingTool CreateTool(string name, string fileName, HarvestingToolType toolType, int tier, float speedMultiplier, int maxResourceTier)
        {
            var tool = ScriptableObject.CreateInstance<HarvestingTool>();
            
            // Устанавливаем базовые параметры через reflection
            var toolTypeField = typeof(HarvestingTool).GetField("toolType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tierField = typeof(HarvestingTool).GetField("toolTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedField = typeof(HarvestingTool).GetField("speedMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxTierField = typeof(HarvestingTool).GetField("maxResourceTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var durabilityField = typeof(HarvestingTool).GetField("durability", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var efficiencyField = typeof(HarvestingTool).GetField("efficiency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (toolTypeField != null) toolTypeField.SetValue(tool, toolType);
            if (tierField != null) tierField.SetValue(tool, tier);
            if (speedField != null) speedField.SetValue(tool, speedMultiplier);
            if (maxTierField != null) maxTierField.SetValue(tool, maxResourceTier);
            if (durabilityField != null) durabilityField.SetValue(tool, 100f);
            if (efficiencyField != null) efficiencyField.SetValue(tool, 1.0f);
            
            // Устанавливаем базовые параметры InventoryItem
            var displayNameField = typeof(InventoryItem).GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var descriptionField = typeof(InventoryItem).GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var iconField = typeof(InventoryItem).GetField("icon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var priceField = typeof(InventoryItem).GetField("price", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isStackableField = typeof(InventoryItem).GetField("isStackable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (displayNameField != null) displayNameField.SetValue(tool, name);
            if (descriptionField != null) descriptionField.SetValue(tool, tool.GetToolDescription());
            if (priceField != null) priceField.SetValue(tool, tier * 10); // Цена зависит от tier
            if (isStackableField != null) isStackableField.SetValue(tool, false); // Инструменты не стакаются
            
            // Сохраняем в Assets
            string path = $"Assets/Internal Assets/Weapons/{fileName}.asset";
            UnityEditor.AssetDatabase.CreateAsset(tool, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"Создан инструмент: {name} (T{tier}) в {path}");
            return tool;
        }
    }
}
#endif 