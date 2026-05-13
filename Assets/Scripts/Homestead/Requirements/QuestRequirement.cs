using UnityEngine;
using RPG.Quests;

namespace RPG.Homestead
{
    /// <summary>
    /// Requires specific quest to be completed.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest Requirement", menuName = "Homestead/Requirements/Quest", order = 2)]
    public class QuestRequirement : BuildingRequirement
    {
        [SerializeField] private Quest requiredQuest;
        [SerializeField] private string specificObjective; // Optional: require specific objective completion
        
        public override bool Validate(bool skipResourceChecks = false)
        {
            if (requiredQuest == null)
            {
                Debug.LogError("QuestRequirement: Required quest is null");
                return false;
            }
            
            var questList = GameObject.FindGameObjectWithTag("Player")?.GetComponent<QuestList>();
            if (questList == null)
            {
                Debug.LogError("QuestRequirement: Player quest list not found");
                return false;
            }
            
            var status = questList.GetQuestStatus(requiredQuest);
            if (status == null) return false;
            
            // Check specific objective if specified
            if (!string.IsNullOrEmpty(specificObjective))
            {
                return status.IsObjectiveComplete(specificObjective);
            }
            
            // Otherwise check if entire quest is complete
            return status.IsComplete();
        }
        
        public override string GetDescription()
        {
            if (requiredQuest == null) return "Invalid quest requirement";
            
            if (!string.IsNullOrEmpty(specificObjective))
            {
                return $"Complete: {requiredQuest.GetTitle()} - {specificObjective}";
            }
            
            return $"Complete quest: {requiredQuest.GetTitle()}";
        }
        
        public override string GetMissingInfo()
        {
            if (requiredQuest == null) return "Invalid quest";
            
            var questList = GameObject.FindGameObjectWithTag("Player")?.GetComponent<QuestList>();
            if (questList == null) return "Quest list not found";
            
            var status = questList.GetQuestStatus(requiredQuest);
            if (status == null) return $"Quest '{requiredQuest.GetTitle()}' not started";
            
            if (!string.IsNullOrEmpty(specificObjective))
            {
                if (status.IsObjectiveComplete(specificObjective))
                {
                    return "Objective complete";
                }
                return $"Objective '{specificObjective}' not complete";
            }
            
            if (status.IsComplete())
            {
                return "Quest complete";
            }
            
            return $"Quest '{requiredQuest.GetTitle()}' not complete";
        }
    }
}
