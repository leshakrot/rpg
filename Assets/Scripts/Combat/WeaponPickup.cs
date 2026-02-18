using RPG.Attributes;
using RPG.Control;
using System.Collections;
using UnityEngine;

namespace RPG.Combat
{
    public class WeaponPickup : InteractableObject
    {
        [SerializeField] private WeaponConfig _weapon;
        [SerializeField] private float _healthToRestore = 0;
        [SerializeField] private float _respawnTime = 5f;

        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.tag == "Player")
            {
                Pickup(other.gameObject);
            }
        }

        private void Pickup(GameObject subject)
        {
            if(_weapon != null)
            {
	            subject.GetComponent<PlayerFighter>().EquipWeapon(_weapon);
            }        
            
            if(_healthToRestore > 0)
            {
                subject.GetComponent<Health>().Heal(_healthToRestore);
            }
            StartCoroutine(HideForSeconds(_respawnTime));
        }

        private IEnumerator HideForSeconds(float seconds)
        {
            ShowPickup(false);
            yield return new WaitForSeconds(seconds);
            ShowPickup(true);
        }

        private void ShowPickup(bool shouldShow)
        {
            GetComponent<Collider>().enabled = shouldShow;
            foreach(Transform child in transform)
            {
                child.gameObject.SetActive(shouldShow);
            }
        }

        protected override void OnInteract(PlayerController callingController)
        {
            Pickup(callingController.gameObject);
        }

        public override CursorType GetCursorType()
        {
            return CursorType.Pickup;
        }
    }
}
