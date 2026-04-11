using System.Collections.Generic;
using UnityEngine;

namespace GameDevTV.Utils
{
    [System.Serializable]
    public class Condition
    {
        [SerializeField] private Disjunction[] _and;

        public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
        {
            if (_and == null || _and.Length == 0) return true;
            foreach(Disjunction dis in _and)
            {
                if (!dis.Check(evaluators))
                {
                    return false;
                }
            }
            return true;
        }

        [System.Serializable]
        private class Disjunction
        {
            [SerializeField] private Predicate[] _or;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                foreach(Predicate pred in _or)
                {
                    if (pred.Check(evaluators))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [System.Serializable]
        private class Predicate
        {
            [SerializeField] private string _predicate;
            [SerializeField] private string[] _parameters;
            [SerializeField] private bool _negate = false;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                bool wasHandled = false; // ‘лаг: отреагировал ли хоть кто-то на предикат

                foreach (var evaluator in evaluators)
                {
                    bool? result = evaluator.Evaluate(_predicate, _parameters);
                    if (result == null)
                    {
                        continue;
                    }

                    wasHandled = true;
                    if (result == _negate) return false;
                }

                // ¬озвращаем true, только если предикат был кем-то обработан и проверка пройдена
                return wasHandled;
            }
        }        
    }
}
