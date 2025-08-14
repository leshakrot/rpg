using UnityEngine;
using System.Linq;

namespace RPG.Quests
{
    /// <summary>
    /// Демонстрационный скрипт для тестирования системы тайных наград
    /// Добавьте этот компонент к любому GameObject для тестирования
    /// </summary>
    public class SecretRewardDemo : MonoBehaviour
    {
        [Header("Настройки демо")]
        [SerializeField] private Quest testQuest;
        [SerializeField] private bool logOnStart = true;
        
        [Header("Кнопки в Inspector")]
        [SerializeField] private bool showQuestPreview;
        [SerializeField] private bool showQuestCompletion;
        [SerializeField] private bool analyzeAllQuests;
        
        void Start()
        {
            if (logOnStart && testQuest != null)
            {
                DemoQuestRewardSystem();
            }
        }
        
        void OnValidate()
        {
            if (showQuestPreview)
            {
                showQuestPreview = false;
                if (testQuest != null) ShowQuestPreview();
            }
            
            if (showQuestCompletion)
            {
                showQuestCompletion = false;
                if (testQuest != null) ShowQuestCompletion();
            }
            
            if (analyzeAllQuests)
            {
                analyzeAllQuests = false;
                AnalyzeAllQuestsInProject();
            }
        }
        
        [ContextMenu("Demo Quest Reward System")]
        public void DemoQuestRewardSystem()
        {
            if (testQuest == null)
            {
                Debug.LogWarning("SecretRewardDemo: Назначьте тестовый квест в поле testQuest!");
                return;
            }
            
            Debug.Log("=== ДЕМО СИСТЕМЫ ТАЙНЫХ НАГРАД ===");
            Debug.Log($"Тестируем квест: {testQuest.GetTitle()}");
            
            // Анализируем награды
            var allRewards = testQuest.GetRewards().ToList();
            var visibleRewards = testQuest.GetVisibleRewards().ToList();
            var secretRewards = testQuest.GetSecretRewards().ToList();
            
            Debug.Log($"📊 Статистика наград:");
            Debug.Log($"   Всего наград: {allRewards.Count}");
            Debug.Log($"   Видимых: {visibleRewards.Count}");
            Debug.Log($"   Тайных: {secretRewards.Count}");
            
            // Показываем видимые награды
            if (visibleRewards.Any())
            {
                Debug.Log($"👁️ Видимые награды (показываем игроку ДО завершения):");
                foreach (var reward in visibleRewards)
                {
                    string rewardText = reward.number > 1 ? $"{reward.number}x {reward.item.GetDisplayName()}" : reward.item.GetDisplayName();
                    Debug.Log($"   • {rewardText}");
                }
            }
            else
            {
                Debug.Log($"👁️ Нет видимых наград");
            }
            
            // Показываем тайные награды
            if (secretRewards.Any())
            {
                Debug.Log($"🎁 Тайные награды (сюрприз ПРИ завершении):");
                foreach (var reward in secretRewards)
                {
                    string rewardText = reward.number > 1 ? $"{reward.number}x {reward.item.GetDisplayName()}" : reward.item.GetDisplayName();
                    Debug.Log($"   • {rewardText}");
                }
                
                string hint = QuestRewardHelper.GetSecretRewardHint(testQuest);
                Debug.Log($"💡 Подсказка для UI: \"{hint}\"");
            }
            else
            {
                Debug.Log($"🎁 Нет тайных наград");
            }
            
            Debug.Log("=== КОНЕЦ ДЕМО ===");
        }
        
        [ContextMenu("Show Quest Preview")]
        public void ShowQuestPreview()
        {
            if (testQuest == null) return;
            
            Debug.Log("=== ПРЕВЬЮ КВЕСТА (что видит игрок) ===");
            
            var visibleRewards = testQuest.GetVisibleRewards().ToList();
            string previewText = $"Квест: {testQuest.GetTitle()}\n\n";
            
            if (visibleRewards.Any())
            {
                previewText += "Награды: ";
                previewText += string.Join(", ", visibleRewards.Select(r => 
                    r.number > 1 ? $"{r.number} {r.item.GetDisplayName()}" : r.item.GetDisplayName()));
            }
            else
            {
                previewText += "Нет явных наград";
            }
            
            if (QuestRewardHelper.HasSecretRewards(testQuest))
            {
                previewText += "\n\n✨ Этот квест может содержать дополнительные сюрпризы...";
            }
            
            Debug.Log(previewText);
        }
        
        [ContextMenu("Show Quest Completion")]
        public void ShowQuestCompletion()
        {
            if (testQuest == null) return;
            
            Debug.Log("=== ЗАВЕРШЕНИЕ КВЕСТА (все награды) ===");
            
            var visibleRewards = testQuest.GetVisibleRewards().ToList();
            var secretRewards = testQuest.GetSecretRewards().ToList();
            
            string completionText = $"Квест '{testQuest.GetTitle()}' выполнен!\n\n";
            
            if (visibleRewards.Any())
            {
                completionText += "Награды: ";
                completionText += string.Join(", ", visibleRewards.Select(r => 
                    r.number > 1 ? $"{r.number} {r.item.GetDisplayName()}" : r.item.GetDisplayName()));
                completionText += "\n";
            }
            
            if (secretRewards.Any())
            {
                completionText += "\n🎉 Бонусные награды: ";
                completionText += string.Join(", ", secretRewards.Select(r => 
                    r.number > 1 ? $"{r.number} {r.item.GetDisplayName()}" : r.item.GetDisplayName()));
                completionText += "\n";
            }
            
            if (!visibleRewards.Any() && !secretRewards.Any())
            {
                completionText += "Нет материальных наград, но вы получили ценный опыт!";
            }
            
            Debug.Log(completionText);
        }
        
        [ContextMenu("Analyze All Quests")]
        public void AnalyzeAllQuestsInProject()
        {
            Debug.Log("=== АНАЛИЗ ВСЕХ КВЕСТОВ В ПРОЕКТЕ ===");
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Quest");
            
            if (guids.Length == 0)
            {
                Debug.Log("Квесты не найдены в проекте.");
                return;
            }
            
            int totalQuests = 0;
            int questsWithSecrets = 0;
            int totalRewards = 0;
            int totalSecretRewards = 0;
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                Quest quest = UnityEditor.AssetDatabase.LoadAssetAtPath<Quest>(path);
                
                if (quest != null)
                {
                    totalQuests++;
                    var rewards = quest.GetRewards().ToList();
                    var secrets = quest.GetSecretRewards().ToList();
                    
                    totalRewards += rewards.Count;
                    totalSecretRewards += secrets.Count;
                    
                    if (secrets.Any())
                    {
                        questsWithSecrets++;
                        Debug.Log($"🎁 {quest.name}: {rewards.Count} наград ({secrets.Count} тайных)");
                    }
                    else
                    {
                        Debug.Log($"📋 {quest.name}: {rewards.Count} наград");
                    }
                }
            }
            
            Debug.Log($"\n📊 СТАТИСТИКА:");
            Debug.Log($"   Всего квестов: {totalQuests}");
            Debug.Log($"   Квестов с тайными наградами: {questsWithSecrets}");
            Debug.Log($"   Всего наград: {totalRewards}");
            Debug.Log($"   Тайных наград: {totalSecretRewards}");
            
            if (totalQuests > 0)
            {
                float secretQuestPercentage = (questsWithSecrets * 100f) / totalQuests;
                float secretRewardPercentage = totalRewards > 0 ? (totalSecretRewards * 100f) / totalRewards : 0;
                
                Debug.Log($"   % квестов с секретами: {secretQuestPercentage:F1}%");
                Debug.Log($"   % тайных наград: {secretRewardPercentage:F1}%");
            }
            
            Debug.Log("=== КОНЕЦ АНАЛИЗА ===");
        }
    }
}