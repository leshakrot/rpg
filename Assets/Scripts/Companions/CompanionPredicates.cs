using UnityEngine;
using GameDevTV.Utils;
using RPG.Inventories;

namespace RPG.Companions
{
    /// <summary>
    /// Предикаты для проверки условий в диалогах компаньонов
    /// Добавляется на NPC-рекрутера вместе с CompanionRecruiter
    /// </summary>
    public class CompanionPredicates : MonoBehaviour, IPredicateEvaluator
    {
        private CompanionRecruiter _recruiter;

        private void Awake()
        {
            _recruiter = GetComponent<CompanionRecruiter>();
        }

        public bool? Evaluate(string predicate, string[] parameters)
        {
            if (_recruiter == null) return null;

            switch (predicate)
            {
                case "CompanionHired":
                    return _recruiter.CheckCompanionHired();
                    
                case "CompanionActive":
                    return _recruiter.CheckCompanionActive();
                    
                case "CompanionNotHired":
                    return !_recruiter.CheckCompanionHired();
                    
                case "CompanionOnBase":
                    return _recruiter.CheckCompanionHired() && !_recruiter.CheckCompanionActive();
                    
                case "CanAffordCompanion":
                    return _recruiter.CheckCanAfford();
                    
                case "CannotAffordCompanion":
                    return !_recruiter.CheckCanAfford();
            }

            return null;
        }
    }
}
