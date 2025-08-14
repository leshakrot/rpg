using UnityEngine;
using UnityEditor;
using RPG.Quests;
using System.Linq;

namespace RPG.Quests.Editor
{
    /// <summary>
    /// Утилита для массового обновления существующих квестов для поддержки тайных наград
    /// </summary>
    public class QuestSecretRewardMigrator : EditorWindow
    {
        [MenuItem("Window/RPG Tools/Quest Secret Reward Migrator")]
        public static void ShowWindow()
        {
            QuestSecretRewardMigrator window = GetWindow<QuestSecretRewardMigrator>("Quest Migrator");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("Quest Secret Reward Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Этот инструмент поможет вам настроить тайные награды для существующих квестов. " +
                "Все существующие награды останутся видимыми по умолчанию.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Найти все квесты в проекте", GUILayout.Height(30)))
            {
                ShowAllQuests();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Пример: Сделать случайные награды тайными", GUILayout.Height(30)))
            {
                MakeRandomRewardsSecret();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            GUILayout.Label("Инструкции:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Откройте Quest Editor (Window → RPG Tools → Quest Editor)\n" +
                "2. Выберите квест для редактирования\n" +
                "3. В секции Rewards отметьте галочкой 'Тайная награда' для скрытых наград\n" +
                "4. Сохраните изменения",
                MessageType.None);
        }

        private void ShowAllQuests()
        {
            string[] guids = AssetDatabase.FindAssets("t:Quest");
            Debug.Log($"Найдено квестов в проекте: {guids.Length}");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Quest quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
                if (quest != null)
                {
                    var rewards = quest.GetRewards().ToList();
                    var secretRewards = quest.GetSecretRewards().ToList();
                    
                    Debug.Log($"Квест: {quest.name} | Всего наград: {rewards.Count} | Тайных: {secretRewards.Count}");
                }
            }
        }

        private void MakeRandomRewardsSecret()
        {
            string[] guids = AssetDatabase.FindAssets("t:Quest");
            int modifiedQuests = 0;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Quest quest = AssetDatabase.LoadAssetAtPath<Quest>(path);
                if (quest != null)
                {
                    SerializedObject serializedQuest = new SerializedObject(quest);
                    SerializedProperty rewardsProperty = serializedQuest.FindProperty("_rewards");
                    
                    bool modified = false;
                    for (int i = 0; i < rewardsProperty.arraySize; i++)
                    {
                        SerializedProperty reward = rewardsProperty.GetArrayElementAtIndex(i);
                        SerializedProperty isSecretProperty = reward.FindPropertyRelative("isSecret");
                        
                        // Делаем каждую вторую награду тайной (простой пример)
                        if (i % 2 == 1 && !isSecretProperty.boolValue)
                        {
                            isSecretProperty.boolValue = true;
                            modified = true;
                        }
                    }
                    
                    if (modified)
                    {
                        serializedQuest.ApplyModifiedProperties();
                        EditorUtility.SetDirty(quest);
                        modifiedQuests++;
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Обновлено квестов с тайными наградами: {modifiedQuests}");
        }
    }
}