using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Harvesting
{
    public class CreateExampleResources : MonoBehaviour
    {
        [ContextMenu("Создать примеры ресурсов деревьев")]
        public void CreateTreeResources()
        {
            // T1 - Березовая древесина (уровень 1)
            var birchWood = CreateResource(
                "Березовая древесина",
                "BirchWood",
                HarvestingToolType.Axe,
                1, // tier
                1, // min level
                3, // base time
                10, // amount
                ResourceCategory.Wood, // category
                10f // experience
            );
            
            // T2 - Дубовая древесина (уровень 3)
            var oakWood = CreateResource(
                "Дубовая древесина", 
                "OakWood",
                HarvestingToolType.Axe,
                2, // tier
                3, // min level
                4, // base time
                10, // amount
                ResourceCategory.Wood, // category
                20f // experience
            );
            
            // T3 - Сосновая древесина (уровень 5)
            var pineWood = CreateResource(
                "Сосновая древесина",
                "PineWood", 
                HarvestingToolType.Axe,
                3, // tier
                5, // min level
                5, // base time
                10, // amount
                ResourceCategory.Wood, // category
                30f // experience
            );
            
            // T4 - Кедровая древесина (уровень 8)
            var cedarWood = CreateResource(
                "Кедровая древесина",
                "CedarWood",
                HarvestingToolType.Axe, 
                4, // tier
                8, // min level
                6, // base time
                10, // amount
                ResourceCategory.Wood, // category
                40f // experience
            );
            
            // T5 - Мистическая древесина (уровень 15)
            var mysticWood = CreateResource(
                "Мистическая древесина",
                "MysticWood",
                HarvestingToolType.Axe,
                5, // tier
                15, // min level
                8, // base time
                10, // amount
                ResourceCategory.Wood, // category
                50f // experience
            );
            
            Debug.Log("Созданы ресурсы деревьев T1-T5");
        }
        
        [ContextMenu("Создать примеры ресурсов руды")]
        public void CreateOreResources()
        {
            // T1 - Железная руда (уровень 1)
            var ironOre = CreateResource(
                "Железная руда",
                "IronOre",
                HarvestingToolType.Pickaxe,
                1, // tier
                1, // min level
                4, // base time
                10, // amount
                ResourceCategory.Ore, // category
                10f // experience
            );
            
            // T2 - Медная руда (уровень 3)
            var copperOre = CreateResource(
                "Медная руда",
                "CopperOre", 
                HarvestingToolType.Pickaxe,
                2, // tier
                3, // min level
                5, // base time
                10, // amount
                ResourceCategory.Ore, // category
                20f // experience
            );
            
            // T3 - Серебряная руда (уровень 5)
            var silverOre = CreateResource(
                "Серебряная руда",
                "SilverOre",
                HarvestingToolType.Pickaxe,
                3, // tier
                5, // min level
                6, // base time
                10, // amount
                ResourceCategory.Ore, // category
                30f // experience
            );
            
            Debug.Log("Созданы ресурсы руды T1-T3");
        }
        
        [ContextMenu("Создать примеры ресурсов трав")]
        public void CreateHerbResources()
        {
            // T1 - Хворост (можно собирать руками)
            var kindling = CreateResource(
                "Хворост",
                "Kindling",
                HarvestingToolType.None,
                1, // tier
                1, // min level
                2, // base time
                5, // amount
                ResourceCategory.Herb, // category
                5f // experience
            );
            
            // T2 - Лечебная трава (нужен серп)
            var healingHerb = CreateResource(
                "Лечебная трава",
                "HealingHerb",
                HarvestingToolType.Sickle,
                2, // tier
                2, // min level
                3, // base time
                8, // amount
                ResourceCategory.Herb, // category
                10f // experience
            );
            
            Debug.Log("Созданы ресурсы трав T1-T2");
        }
        
        private HarvestableResource CreateResource(string name, string fileName, HarvestingToolType toolType, int tier, int minLevel, int baseTime, int amount, ResourceCategory category, float experiencePerUnit)
        {
            var resource = ScriptableObject.CreateInstance<HarvestableResource>();
            
            // Устанавливаем базовые параметры через reflection (так как поля private)
            var nameField = typeof(HarvestableResource).GetField("resourceName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var toolTypeField = typeof(HarvestableResource).GetField("requiredToolType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tierField = typeof(HarvestableResource).GetField("resourceTier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var minLevelField = typeof(HarvestableResource).GetField("minimumLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var baseTimeField = typeof(HarvestableResource).GetField("baseHarvestTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var amountField = typeof(HarvestableResource).GetField("resourceAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var categoryField = typeof(HarvestableResource).GetField("resourceCategory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var experienceField = typeof(HarvestableResource).GetField("experiencePerUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (nameField != null) nameField.SetValue(resource, name);
            if (toolTypeField != null) toolTypeField.SetValue(resource, toolType);
            if (tierField != null) tierField.SetValue(resource, tier);
            if (minLevelField != null) minLevelField.SetValue(resource, minLevel);
            if (baseTimeField != null) baseTimeField.SetValue(resource, baseTime);
            if (amountField != null) amountField.SetValue(resource, amount);
            if (categoryField != null) categoryField.SetValue(resource, category);
            if (experienceField != null) experienceField.SetValue(resource, experiencePerUnit);
            
            // Устанавливаем цвет прогресс-бара в зависимости от tier
            var colorField = typeof(HarvestableResource).GetField("progressBarColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (colorField != null)
            {
                Color barColor = tier switch
                {
                    1 => Color.green,
                    2 => Color.blue,
                    3 => Color.yellow,
                    4 => Color.magenta,
                    5 => Color.red,
                    _ => Color.white
                };
                colorField.SetValue(resource, barColor);
            }
            
            // Сохраняем в Assets
            string path = $"Assets/Internal Assets/Materials/{fileName}.asset";
            UnityEditor.AssetDatabase.CreateAsset(resource, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"Создан ресурс: {name} (T{tier}, {category}, {experiencePerUnit} exp) в {path}");
            return resource;
        }
    }
} 