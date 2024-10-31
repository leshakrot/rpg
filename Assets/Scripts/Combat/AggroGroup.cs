using UnityEngine;

namespace RPG.Combat
{
    public class AggroGroup : MonoBehaviour
    {
        [SerializeField] private Fighter[] _fighters;
        [SerializeField] private bool _activateOnStart = false;

        private void Start()
        {
            Active(_activateOnStart);
        }

        public void Active(bool shouldActivate)
        {
            foreach(Fighter fighter in _fighters)
            {
                CombatTarget target = fighter.GetComponent<CombatTarget>();
                if(target != null)
                {
                    target.enabled = shouldActivate;
                }
                fighter.enabled = shouldActivate;
            }
        }
    }
}
