using UnityEngine;
using RPG.Quests;
using RPG.Attributes;

namespace RPG.Combat
{
    public class DynamicSpawnHelper : MonoBehaviour
    {
        public void SetupQuestProgress(Component component, QuestList questList, Quest questReference, string objectiveReference, int amount)
        {
            QuestProgress questProgress = component as QuestProgress;
            if (questProgress == null)
            {
                Debug.LogWarning("[DynamicSpawnHelper] Компонент не является QuestProgress");
                return;
            }
            
            var questListField = typeof(QuestProgress).GetField("questList", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var questRefField = typeof(QuestProgress).GetField("questReference", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var objectiveRefField = typeof(QuestProgress).GetField("objectiveReference", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var amountField = typeof(QuestProgress).GetField("amount", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (questListField != null) questListField.SetValue(questProgress, questList);
            if (questRefField != null) questRefField.SetValue(questProgress, questReference);
            if (objectiveRefField != null) objectiveRefField.SetValue(questProgress, objectiveReference);
            if (amountField != null) amountField.SetValue(questProgress, amount);
        }
        
        public void ConnectQuestProgressToHealth(Component questProgressComponent)
        {
            QuestProgress questProgress = questProgressComponent as QuestProgress;
            if (questProgress == null)
            {
                Debug.LogWarning("[DynamicSpawnHelper] Компонент не является QuestProgress");
                return;
            }
            
            Health health = questProgress.GetComponent<Health>();
            if (health == null)
            {
                Debug.LogWarning("[DynamicSpawnHelper] На объекте нет компонента Health");
                return;
            }
            
            health.onDie.AddListener(questProgress.AddProgress);
        }
        
        public void CheckQuestCondition(SpawnConditionResult result, QuestList questList, Quest quest, string objectiveRef)
        {
            if (questList == null || quest == null)
            {
                result.canSpawn = true;
                return;
            }
            
            if (questList.HasQuest(quest))
            {
                QuestStatus status = questList.GetQuestStatus(quest);
                
                if (string.IsNullOrEmpty(objectiveRef))
                {
                    result.canSpawn = !status.IsComplete();
                }
                else
                {
                    result.canSpawn = !status.IsObjectiveComplete(objectiveRef);
                }
            }
            else
            {
                result.canSpawn = false;
            }
        }
        
        public void CheckQuestNotStarted(SpawnConditionResult result, QuestList questList, Quest quest)
        {
            if (questList == null || quest == null)
            {
                result.canSpawn = true;
                return;
            }
            
            result.canSpawn = !questList.HasQuest(quest);
        }
        
        public void CheckQuestActive(SpawnConditionResult result, QuestList questList, Quest quest)
        {
            if (questList == null || quest == null)
            {
                result.canSpawn = false;
                return;
            }
            
            result.canSpawn = questList.HasQuest(quest);
        }
        
        public void InvertCondition(SpawnConditionResult result)
        {
            result.canSpawn = !result.canSpawn;
        }
        
        public void AlwaysSpawn(SpawnConditionResult result)
        {
            result.canSpawn = true;
        }
        
        public void NeverSpawn(SpawnConditionResult result)
        {
            result.canSpawn = false;
        }
    }
}
