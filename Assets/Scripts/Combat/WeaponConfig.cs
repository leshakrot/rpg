using GameDevTV.Inventories;
using RPG.Attributes;
using RPG.Stats;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "RPG/ Weapons/ New Weapon", order = 0)]
    public class WeaponConfig: EquipableItem, IModifierProvider
    {
        [SerializeField] private AnimatorOverrideController _animatorOverride = null;
        [SerializeField] private Weapon _equippedPrefab;
        [SerializeField] private float _weaponDamage = 5f;
        [SerializeField] private float _percantageBonus = 0;
        [SerializeField] private float _weaponRange = 2f;
        [SerializeField] private bool _isRightHanded = true;
        [SerializeField] private Projectile _projectile;

        [Header("Combat feel")]
        [Tooltip("Шанс (0-1) нанести критический удар этим оружием.")]
        [SerializeField] [Range(0f, 1f)] private float _criticalChance = 0.1f;
        [Tooltip("Множитель урона при критическом ударе.")]
        [SerializeField] private float _criticalMultiplier = 1.75f;
        [Tooltip("Случайный разброс урона, напр. 0.15 = урон гуляет на +/-15%.")]
        [SerializeField] [Range(0f, 0.9f)] private float _damageVariance = 0.15f;

        private const string weaponName = "Weapon";

        public Weapon Spawn(Transform rightHand, Transform leftHand, Animator animator)
        {
            DestroyOldWeapon(rightHand, leftHand);

            Weapon weapon = null;
            if(_equippedPrefab != null)
            {
                Transform handTransform = GetTransform(rightHand, leftHand);
                weapon = Instantiate(_equippedPrefab, handTransform);
                weapon.gameObject.name = weaponName;
            }

            var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (_animatorOverride != null)
            {
                animator.runtimeAnimatorController = _animatorOverride;
            }
            else if (overrideController != null)
            {
                animator.runtimeAnimatorController = overrideController.runtimeAnimatorController;
            }

            return weapon;
        }

        private void DestroyOldWeapon(Transform rightHand, Transform leftHand)
        {
            Transform oldWeapon = rightHand.Find(weaponName);
            if(oldWeapon == null) 
            {
                oldWeapon = leftHand.Find(weaponName);
            }
            if (oldWeapon == null) return;

            oldWeapon.name = "Destroying";
            Destroy(oldWeapon.gameObject);
        }

        private Transform GetTransform(Transform rightHand, Transform leftHand)
        {
            Transform handTransform;
            if (_isRightHanded) handTransform = rightHand;
            else handTransform = leftHand;
            return handTransform;
        }

        public bool HasProjectile()
        {
            return _projectile != null;
        }

        public void LaunchProjectile(Transform rightHand, Transform leftHand, Health target, GameObject instigator, float calculatedDamage, bool isCriticalHit = false)
        {
            Projectile projectileInstance = Instantiate(_projectile, GetTransform(rightHand, leftHand).position, Quaternion.identity);
            projectileInstance.SetTarget(target, instigator, calculatedDamage, isCriticalHit);
        }

        public float GetDamage()
        {
            return _weaponDamage;
        }

        public float GetPercentageBonus()
        {
            return _percantageBonus;
        }

        public float GetRange()
        {
            return _weaponRange;
        }

        /// <summary>Возвращает шанс критического удара для этого оружия (0-1).</summary>
        public float GetCriticalChance()
        {
            return _criticalChance;
        }

        /// <summary>Возвращает множитель урона, применяемый при критическом ударе.</summary>
        public float GetCriticalMultiplier()
        {
            return _criticalMultiplier;
        }

        /// <summary>Возвращает величину случайного разброса урона (0-1, доля от базового урона).</summary>
        public float GetDamageVariance()
        {
            return _damageVariance;
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if(stat == Stat.Damage)
            {
                yield return _weaponDamage;
            }
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == Stat.Damage)
            {
                yield return _percantageBonus;
            }
        }
    }
}
