using RPG.Control;
using System;
using System.Collections;
using UnityEngine;

namespace RPG.Combat
{
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private Weapon _weapon;
        [SerializeField] private float _respawnTime = 5f;

        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.TryGetComponent(out PlayerController player))
            {
                player.GetComponent<Fighter>().EquipWeapon(_weapon);
                StartCoroutine(HideForSeconds(_respawnTime));
            }
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
    }
}
