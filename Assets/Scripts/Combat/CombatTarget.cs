using RPG.Attributes;
using RPG.Control;
using UnityEngine;

namespace RPG.Combat
{
    [RequireComponent(typeof(Health))]
    public class CombatTarget : MonoBehaviour, IRaycastable
    {
        public CursorType GetCursorType()
        {
            return CursorType.Combat;
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            if(!enabled) return false;
	        if (!callingController.GetComponent<PlayerFighter>().CanAttack(gameObject))
            {
                return false;
            }


            if (Input.GetMouseButton(0))
            {
	            callingController.GetComponent<PlayerFighter>().Attack(gameObject);
            }
            return true;
        }
    }
}
