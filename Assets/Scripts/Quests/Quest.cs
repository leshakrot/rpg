using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "RPG/Quest", order = 0)]
    public class Quest : ScriptableObject
    {
        [SerializeField] private string[] _objectives;

        public string GetTitle()
        {
            return name;
        }

        public int GetObjectiveCount()
        {
            return _objectives.Length;
        }

        public IEnumerable<string> GetObjectives()
        {
            return _objectives;
        }
    }
}
