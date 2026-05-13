using UnityEngine;

namespace RPG.Homestead
{
    /// <summary>
    /// Runtime representation of a constructed building.
    /// Stores current state and references.
    /// </summary>
    [System.Serializable]
    public class BuildingInstance
    {
        public BuildingData buildingData;
        public int currentLevel;
        public string slotId;
        public GameObject buildingObject; // The instantiated prefab (not serialized)
        
        /// <summary>
        /// Check if the building is at maximum level.
        /// </summary>
        public bool IsMaxLevel()
        {
            return currentLevel >= buildingData.MaxLevel;
        }
        
        /// <summary>
        /// Check if the building can be upgraded.
        /// </summary>
        public bool CanUpgrade()
        {
            return !IsMaxLevel();
        }
        
        /// <summary>
        /// Get the display name with current level.
        /// </summary>
        public string GetDisplayName()
        {
            return $"{buildingData.DisplayName} (Level {currentLevel})";
        }
    }
}
