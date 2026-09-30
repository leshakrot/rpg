using UnityEngine;
using GameDevTV.Utils;

namespace RPG.Examples
{
    /// <summary>
    /// Пример класса с предикатами для демонстрации работы Dialogue Predicate Viewer.
    /// 
    /// Доступные предикаты:
    ///   ExamplePredicate — пример простого предиката
    ///   AnotherExample — еще один пример
    ///   TestCondition — тестовое условие
    /// </summary>
    public class ExamplePredicates : MonoBehaviour, IPredicateEvaluator
    {
        public bool? Evaluate(string predicate, string[] parameters)
        {
            switch (predicate)
            {
                case "ExamplePredicate":
                    return true; // Всегда возвращает true
                
                case "AnotherExample":
                    return false; // Всегда возвращает false
                
                case "TestCondition":
                    // Пример с параметрами
                    if (parameters.Length > 0)
                    {
                        return parameters[0] == "test";
                    }
                    return null;
                
                default:
                    return null;
            }
        }
    }
}
