using UnityEngine;
using GameDevTV.Utils;

namespace RPG.Companions
{
    /// <summary>
    /// Предикаты для условий в диалоговых нодах.
    ///
    /// Доступные предикаты:
    ///   CompanionHired        — нанят (в любом состоянии)
    ///   CompanionNotHired     — не нанят вообще
    ///   CompanionActive       — нанят и следует за игроком
    ///   CompanionWaiting      — нанят, ждёт на месте ("Подожди здесь")
    ///   CompanionOnBase       — нанят, отправлен на базу
    ///   CanAffordCompanion    — хватает денег на найм
    ///   CannotAffordCompanion — не хватает денег
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

        public bool? Evaluate(string predicate, string[] parameters)
        {
            if (_recruiter == null) return null;

            return predicate switch
            {
                "CompanionHired"        =>  _recruiter.CheckCompanionHired(),
                "CompanionNotHired"     => !_recruiter.CheckCompanionHired(),
                "CompanionActive"       =>  _recruiter.CheckCompanionActive(),
                "CompanionWaiting"      =>  _recruiter.CheckCompanionWaiting(),
                "CompanionOnBase"       =>  _recruiter.CheckCompanionOnBase(),
                "CanAffordCompanion"    =>  _recruiter.CheckCanAfford(),
                "CannotAffordCompanion" => !_recruiter.CheckCanAfford(),
                _                       => null
            };
        }
    }
}
