using UnityEngine;
using GameDevTV.Utils;

namespace RPG.Companions
{
    /// <summary>
    /// Предикаты для условий в диалоговых нодах.
    /// Лежит на том же объекте, что CompanionRecruiter.
    /// </summary>
    public class CompanionPredicates : MonoBehaviour, IPredicateEvaluator
    {
        private CompanionRecruiter _recruiter;

        private void Awake()
        {
            _recruiter = GetComponent<CompanionRecruiter>();
            if (_recruiter == null)
                Debug.LogError("[CompanionPredicates] CompanionRecruiter не найден!", this);
        }

        /// <summary>
        /// Поддерживаемые предикаты:
        ///   CompanionHired        — нанят (активен или на базе)
        ///   CompanionNotHired     — не нанят вообще
        ///   CompanionActive       — нанят и следует за игроком
        ///   CompanionOnBase       — нанят, но отправлен на базу
        ///   CanAffordCompanion    — хватает денег на найм
        ///   CannotAffordCompanion — не хватает денег
        /// </summary>
        public bool? Evaluate(string predicate, string[] parameters)
        {
            if (_recruiter == null) return null;

            return predicate switch
            {
                "CompanionHired"        =>  _recruiter.CheckCompanionHired(),
                "CompanionNotHired"     => !_recruiter.CheckCompanionHired(),
                "CompanionActive"       =>  _recruiter.CheckCompanionActive(),
                "CompanionOnBase"       =>  _recruiter.CheckCompanionHired() && !_recruiter.CheckCompanionActive(),
                "CanAffordCompanion"    =>  _recruiter.CheckCanAfford(),
                "CannotAffordCompanion" => !_recruiter.CheckCanAfford(),
                _                       => null
            };
        }
    }
}
