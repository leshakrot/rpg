using UnityEngine;
using GameDevTV.Inventories;
using System.Collections.Generic;
using System.Linq;

namespace RPG.Harvesting
{
    /// <summary>
    /// Утилита для проверки инструментов добычи в инвентаре игрока
    /// </summary>
    public static class HarvestingToolChecker
    {
        /// <summary>
        /// Найти лучший инструмент для добычи определённого типа ресурса
        /// </summary>
        /// <param name="inventory">Инвентарь игрока</param>
        /// <param name="requiredToolType">Требуемый тип инструмента</param>
        /// <param name="resourceTier">Уровень ресурса</param>
        /// <returns>Лучший подходящий инструмент или null</returns>
        public static HarvestingTool FindBestTool(Inventory inventory, HarvestingToolType requiredToolType, int resourceTier)
        {
            if (inventory == null)
            {
                Debug.LogWarning("HarvestingToolChecker: Инвентарь не найден");
                return null;
            }
            
            // Собираем все инструменты из инвентаря
            var availableTools = GetAllHarvestingTools(inventory);
            
            // Фильтруем подходящие инструменты
            var compatibleTools = availableTools.Where(tool => 
                tool.IsCompatibleWith(requiredToolType) && 
                tool.CanHarvestResourceTier(resourceTier)
            ).ToList();
            
            if (!compatibleTools.Any())
            {
                Debug.Log($"HarvestingToolChecker: Не найдено подходящих инструментов для {requiredToolType} T{resourceTier}");
                return null;
            }
            
            // Выбираем лучший инструмент (по уровню, затем по скорости)
            var bestTool = compatibleTools.OrderByDescending(tool => tool.ToolTier)
                                         .ThenByDescending(tool => tool.SpeedMultiplier)
                                         .First();
            
            Debug.Log($"HarvestingToolChecker: Найден лучший инструмент - {bestTool.GetDisplayName()} T{bestTool.ToolTier}");
            return bestTool;
        }
        
        /// <summary>
        /// Проверить, может ли игрок добывать ресурс (есть ли подходящий инструмент или ресурс не требует инструмента)
        /// </summary>
        public static bool CanHarvestResource(Inventory inventory, HarvestingToolType requiredToolType, int resourceTier)
        {
            // Если ресурс не требует инструмента (уровень 1, кусты/камни)
            if (requiredToolType == HarvestingToolType.None)
                return true;
                
            return FindBestTool(inventory, requiredToolType, resourceTier) != null;
        }
        
        /// <summary>
        /// Получить множитель скорости добычи от лучшего инструмента
        /// </summary>
        public static float GetHarvestSpeedMultiplier(Inventory inventory, HarvestingToolType requiredToolType, int resourceTier, int playerLevel)
        {
            var bestTool = FindBestTool(inventory, requiredToolType, resourceTier);
            
            if (bestTool == null)
            {
                // Если нет инструмента, но ресурс можно собирать руками (уровень 1)
                if (requiredToolType == HarvestingToolType.None && resourceTier == 1)
                {
                    return 0.5f; // Руками в 2 раза медленнее
                }
                return 0f; // Нельзя добывать
            }
            
            return bestTool.GetHarvestSpeedMultiplier(playerLevel);
        }
        
        /// <summary>
        /// Получить все инструменты добычи из инвентаря
        /// </summary>
        private static List<HarvestingTool> GetAllHarvestingTools(Inventory inventory)
        {
            var tools = new List<HarvestingTool>();
            
            for (int i = 0; i < inventory.GetSize(); i++)
            {
                var item = inventory.GetItemInSlot(i);
                if (item is HarvestingTool harvestingTool)
                {
                    tools.Add(harvestingTool);
                }
            }
            
            return tools;
        }
        
        /// <summary>
        /// Получить информацию о всех инструментах игрока для UI
        /// </summary>
        public static string GetToolsInfo(Inventory inventory)
        {
            var tools = GetAllHarvestingTools(inventory);
            
            if (!tools.Any())
                return "Инструменты добычи не найдены";
                
            var toolGroups = tools.GroupBy(t => t.ToolType)
                                 .OrderBy(g => g.Key);
            
            var info = "Доступные инструменты:\n";
            foreach (var group in toolGroups)
            {
                var bestInGroup = group.OrderByDescending(t => t.ToolTier).First();
                info += $"• {bestInGroup.GetToolDescription()}\n";
            }
            
            return info.TrimEnd('\n');
        }
        
        /// <summary>
        /// Проверить, доступен ли определённый уровень ресурса для игрока
        /// </summary>
        public static bool IsResourceTierAvailable(Inventory inventory, HarvestingToolType toolType, int resourceTier)
        {
            if (toolType == HarvestingToolType.None && resourceTier == 1)
                return true; // Кусты/камни 1 уровня доступны всем
                
            var tool = FindBestTool(inventory, toolType, resourceTier);
            return tool != null && tool.CanHarvestResourceTier(resourceTier);
        }
    }
} 