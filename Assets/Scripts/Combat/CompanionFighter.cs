using System.Collections.Generic;
using UnityEngine;
using RPG.Attributes;

namespace RPG.Combat
{
    /// <summary>
    /// Fighter для компаньона. Не атакует игрока и других компаньонов —
    /// ни вручную, ни при автоатаке после убийства врага.
    /// </summary>
    public class CompanionFighter : Fighter
    {
        // Теги, которые компаньон не трогает
        private static readonly string[] FriendlyTags = { "Player", "Companion" };

        public override bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null) return false;
            if (IsFriendly(combatTarget)) return false;
            return base.CanAttack(combatTarget);
        }

        protected override IEnumerable<Health> FindAllTargetsInRange(float range)
        {
            foreach (Health h in base.FindAllTargetsInRange(range))
            {
                if (!IsFriendly(h.gameObject))
                    yield return h;
            }
        }

        private static bool IsFriendly(GameObject go)
        {
            foreach (string tag in FriendlyTags)
                if (go.CompareTag(tag)) return true;
            return false;
        }
    }
}
