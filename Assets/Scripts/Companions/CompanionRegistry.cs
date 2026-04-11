using System.Collections.Generic;
using UnityEngine;

namespace RPG.Companions
{
    /// <summary>
    /// Единый реестр всех компаньонов в проекте.
    /// Создаётся один раз: Assets → Create → Companions → Companion Registry.
    /// Назначается в Inspector на любой постоянный объект (например, SavingWrapper или GameManager).
    ///
    /// Расширяй CompanionEntry по мере необходимости — иконки, озвучка, диалоги и т.д.
    /// </summary>
    [CreateAssetMenu(fileName = "CompanionRegistry", menuName = "Companions/Companion Registry", order = 1)]
    public class CompanionRegistry : ScriptableObject
    {
        [System.Serializable]
        public class CompanionEntry
        {
            [Tooltip("Данные компаньона — параметры поведения и найма")]
            public CompanionData data;

            [Tooltip("Префаб компаньона — используется и как NPC, и как боец")]
            public GameObject prefab;

            // Сюда можно добавлять новые поля не трогая остальную систему:
            // public Sprite portrait;
            // public AudioClip greetingSound;
            // public DialogueObject introDialogue;
        }

        [SerializeField] private List<CompanionEntry> companions = new();

        public CompanionEntry GetEntry(string companionID)
        {
            foreach (var entry in companions)
                if (entry.data != null && entry.data.CompanionID == companionID)
                    return entry;
            return null;
        }

        public GameObject    GetPrefab(string companionID) => GetEntry(companionID)?.prefab;
        public CompanionData GetData(string companionID)   => GetEntry(companionID)?.data;

        public IReadOnlyList<CompanionEntry> GetAll() => companions;
    }
}
