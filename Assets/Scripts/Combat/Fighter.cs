using GameDevTV.Utils;
using RPG.Attributes;
using RPG.Core;
using RPG.Movement;
using RPG.Saving;
using RPG.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPG.Combat
{
    public class Fighter : MonoBehaviour, IAction, ISaveable, IModifierProvider
    {
        [SerializeField] private float _timeBetweenAttacks = 1f;

        [SerializeField] private Transform _rightHandTransform;
        [SerializeField] private Transform _leftHandTransform;
        [SerializeField] private Weapon _defaultWeapon = null;

        private ActionScheduler _actionScheduler;
        private Health _target;
        private Mover _mover;
        private Animator _animator;
        private float _timeSinceLastAttack = Mathf.Infinity;
        private LazyValue<Weapon> _currentWeapon;

        private void Awake()
        {
            _actionScheduler = GetComponent<ActionScheduler>();
            _mover = GetComponent<Mover>();
            _animator = GetComponent<Animator>();

            _currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);
        }

        private Weapon SetupDefaultWeapon()
        {
            AttachWeapon(_defaultWeapon);
            return _defaultWeapon;
        }

        private void Start()
        {
            _currentWeapon.ForceInit();      
        }

        public void EquipWeapon(Weapon weapon)
        {
            _currentWeapon.value = weapon;
            AttachWeapon(weapon);
        }

        private void AttachWeapon(Weapon weapon)
        {
            weapon.Spawn(_rightHandTransform, _leftHandTransform, _animator);
        }

        public Health GetTarget()
        {
            return _target;
        }

        private void Update()
        {
            _timeSinceLastAttack += Time.deltaTime; 

            if (_target == null) return;

            if (_target.IsDead()) return;

            if (!GetIsInRange())
            {
                _mover.MoveTo(_target.transform.position, 1f);
            }
            else
            {
                _mover.Cancel();
                AttackBehaviour();
            }
        }

        private void AttackBehaviour()
        {
            transform.LookAt(_target.transform);
            if(_timeSinceLastAttack > _timeBetweenAttacks)
            {
                TriggerAttack();
                _timeSinceLastAttack = 0;
            }
        }

        private void TriggerAttack()
        {
            _animator.ResetTrigger("stopAttack");
            _animator.SetTrigger("attack");
        }

        private bool GetIsInRange()
        {
            return Vector3.Distance(transform.position, _target.transform.position) < _currentWeapon.value.GetRange();
        }

        public bool CanAttack(GameObject combatTarget)
        {
            if(combatTarget == null) return false;
            Health targetToTest = combatTarget.GetComponent<Health>();
            return targetToTest != null && !targetToTest.IsDead();
        }

        public void Attack(GameObject combatTarget)
        {
            _actionScheduler.StartAction(this);
            _target = combatTarget.GetComponent<Health>();
        }

        public void Cancel()
        {
            StopAttack();
            _target = null;
            _mover.Cancel();
        }

        private void StopAttack()
        {
            _animator.ResetTrigger("attack");
            _animator.SetTrigger("stopAttack");
        }

        public IEnumerable<float> GetAdditiveModifiers(Stat stat)
        {
            if(stat == Stat.Damage)
            {
                yield return _currentWeapon.value.GetDamage();
            }
        }

        public IEnumerable<float> GetPercentageModifiers(Stat stat)
        {
            if (stat == Stat.Damage)
            {
                yield return _currentWeapon.value.GetPercentageBonus();
            }
        }

        public void Hit()
        {
            if (_target == null) return;

            float damage = GetComponent<BaseStats>().GetStat(Stat.Damage);
            if (_currentWeapon.value.HasProjectile()) _currentWeapon.value.LaunchProjectile(_rightHandTransform, _leftHandTransform, _target, gameObject, damage);
            else
            {             
                _target.TakeDamage(gameObject, damage);
            }
        }

        public void Shoot()
        {
            Hit();
        }

        public object CaptureState()
        {
            return _currentWeapon.value.name;          
        }

        public void RestoreState(object state)
        {
            string weaponName = (string)state;
            Weapon weapon = Resources.Load<Weapon>(weaponName);
            EquipWeapon(weapon);
        }
    }
}
