using UnityEngine;
using RPG.Stats;

namespace RPG.UI.ExperienceText
{
    public class ExperienceTextTester : MonoBehaviour
    {
        [SerializeField] private KeyCode testKey = KeyCode.X;
        [SerializeField] private float testExperienceAmount = 50f;
        
        private Experience playerExperience;
        
        private void Start()
        {
            // Находим компонент Experience у игрока
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerExperience = player.GetComponent<Experience>();
                if (playerExperience == null)
                {
                    Debug.LogError("ExperienceTextTester: Компонент Experience не найден у игрока!");
                }
            }
            else
            {
                Debug.LogError("ExperienceTextTester: Игрок не найден!");
            }
        }
        
        private void Update()
        {
            // Тестируем всплывашку опыта при нажатии клавиши
            if (Input.GetKeyDown(testKey) && playerExperience != null)
            {
                playerExperience.GainExperience(testExperienceAmount);
                Debug.Log($"Тест: Добавлено {testExperienceAmount} опыта");
            }
        }
    }
} 