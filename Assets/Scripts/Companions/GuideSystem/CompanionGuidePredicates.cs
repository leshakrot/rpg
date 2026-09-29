using UnityEngine;
using GameDevTV.Utils;

namespace RPG.Companions
{
    /// <summary>
    /// Предикаты для диалоговых нод (вешать на собаку рядом с CompanionPredicates).
    ///   CompanionGuiding       — собака сейчас ведёт игрока
    ///   CompanionNotGuiding    — не ведёт
    ///   CanGuideToQuestTarget  — у квеста есть цель и до неё есть маршрут
    /// </summary>
    public class CompanionGuidePredicates : MonoBehaviour, IPredicateEvaluator
    {
        private CompanionGuide _guide;

        private void Awake()
        {
            _guide = GetComponent<CompanionGuide>();
            if (_guide == null)
                Debug.LogError("[CompanionGuidePredicates] CompanionGuide не найден!", this);
        }

        public bool? Evaluate(string predicate, string[] parameters)
        {
            if (_guide == null) return null;

            return predicate switch
            {
                "CompanionGuiding"      =>  _guide.IsGuiding,
                "CompanionNotGuiding"   => !_guide.IsGuiding,
                "CanGuideToQuestTarget" =>  _guide.CanGuideToCurrentTarget,
                _                       => null
            };
        }
    }
}
