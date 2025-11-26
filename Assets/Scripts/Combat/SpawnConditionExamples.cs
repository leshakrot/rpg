using UnityEngine;

namespace RPG.Combat
{
    public class SpawnConditionExamples : MonoBehaviour
    {
        [SerializeField] private bool exampleBoolCondition = true;
        [SerializeField] private int requiredPlayerLevel = 10;
        
        public void SimpleCondition(SpawnConditionResult result)
        {
            result.canSpawn = exampleBoolCondition;
        }
        
        public void CheckPlayerLevel(SpawnConditionResult result, int minLevel)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var stats = player.GetComponent<RPG.Stats.BaseStats>();
                if (stats != null)
                {
                    result.canSpawn = stats.GetLevel() >= minLevel;
                    return;
                }
            }
            result.canSpawn = false;
        }
        
        public void CheckTimeOfDay(SpawnConditionResult result, float minHour, float maxHour)
        {
            float currentHour = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 60f;
            result.canSpawn = currentHour >= minHour && currentHour <= maxHour;
        }
        
        public void RandomChance(SpawnConditionResult result, float chance)
        {
            result.canSpawn = Random.value <= chance;
        }
        
        public void CombineConditionsAND(SpawnConditionResult result1, SpawnConditionResult result2)
        {
            result1.canSpawn = result1.canSpawn && result2.canSpawn;
        }
        
        public void CombineConditionsOR(SpawnConditionResult result1, SpawnConditionResult result2)
        {
            result1.canSpawn = result1.canSpawn || result2.canSpawn;
        }
    }
}
