using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires another building to be constructed first.
    /// </summary>
    [CreateAssetMenu(fileName = "New Prerequisite Requirement", menuName = "Homestead/Requirements/Prerequisite Building", order = 4)]
    public class PrerequisiteBuildingRequirement : BuildingRequirement
    {
        [SerializeField] private BuildingData prerequisiteBuilding;
        [SerializeField] private int minimumLevel = 1;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (prerequisiteBuilding == null)
            {
                Debug.LogError("PrerequisiteBuildingRequirement: Prerequisite building is null");
                return false;
            }
            
            if (EstateManager.Instance == null)
            {
                Debug.LogError("PrerequisiteBuildingRequirement: EstateManager not found");
                return false;
            }
            
            return EstateManager.Instance.HasBuildingAtLevel(prerequisiteBuilding.BuildingId, minimumLevel);
        }
        
        public override string GetDescription()
        {
            if (prerequisiteBuilding == null) return "Invalid prerequisite";
            
            if (minimumLevel > 1)
            {
                return $"Requires: {prerequisiteBuilding.DisplayName} (Level {minimumLevel}+)";
            }
            
            return $"Requires: {prerequisiteBuilding.DisplayName}";
        }
        
        public override string GetMissingInfo()
        {
            if (prerequisiteBuilding == null) return "Invalid prerequisite";
            
            if (EstateManager.Instance == null) return "Estate manager not found";
            
            if (!EstateManager.Instance.HasBuilding(prerequisiteBuilding.BuildingId))
            {
                return $"{prerequisiteBuilding.DisplayName} not built";
            }
            
            var buildings = EstateManager.Instance.GetBuildingsOfType(prerequisiteBuilding.BuildingId);
            int maxLevel = 0;
            foreach (var building in buildings)
            {
                if (building.currentLevel > maxLevel)
                {
                    maxLevel = building.currentLevel;
                }
            }
            
            if (maxLevel < minimumLevel)
            {
                return $"{prerequisiteBuilding.DisplayName} needs to be level {minimumLevel} (current: {maxLevel})";
            }
            
            return "Prerequisite met";
        }
    }
}
