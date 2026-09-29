using UnityEngine;
using GameDevTV.Utils;

namespace RPG.Companions
{
    /// <summary>
    /// Предикат для диалоговых нод (вешать на собаку рядом с CompanionQuestGuideChoiceProvider):
    ///   HasGuidableQuest — есть хотя бы один активный квест, к цели которого можно повести сейчас.
    /// Используй как условие видимости самой ноды "Веди меня к цели".
    /// </summary>
    public class CompanionQuestGuidePredicates : MonoBehaviour, IPredicateEvaluator
    {
        private CompanionQuestGuideChoiceProvider _provider;

        private void Awake()
        {
            _provider = GetComponent<CompanionQuestGuideChoiceProvider>();
            if (_provider == null)
                Debug.LogError("[CompanionQuestGuidePredicates] CompanionQuestGuideChoiceProvider не найден!", this);
        }

        public bool? Evaluate(string predicate, string[] parameters)
        {
            if (_provider == null) return null;

            return predicate switch
            {
                "HasGuidableQuest" => _provider.HasGuidableQuest(),
                _                  => null
            };
        }
    }
}
