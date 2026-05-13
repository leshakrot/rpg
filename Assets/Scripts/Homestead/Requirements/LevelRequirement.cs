using UnityEngine;
using RPG.Stats;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires player to be at specific level.
    /// </summary>
    [CreateAssetMenu(fileName = "New Level Requirement", menuName = "Homestead/Requirements/Level", order = 3)]
    public class LevelRequirement : BuildingRequirement
    {
        [SerializeField] private int requiredLevel;
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            var baseStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<BaseStats>();
            if (baseStats == null)
            {
                Debug.LogError("LevelRequirement: Player BaseStats not found");
                return false;
            }
            
            return baseStats.GetLevel() >= requiredLevel;
        }
        
        public override string GetDescription()
        {
            return $"Player Level {requiredLevel}";
        }
        
        public override string GetMissingInfo()
        {
            var baseStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<BaseStats>();
            if (baseStats == null) return "Player stats not found";
            
            int currentLevel = baseStats.GetLevel();
            if (currentLevel >= requiredLevel)
            {
                return "Level requirement met";
            }
            
            return $"Need level {requiredLevel} (current: {currentLevel})";
        }
    }
}
