using GameDevTV.Utils;
using RPG.Attributes;
using RPG.Core;
using RPG.Movement;
using GameDevTV.Saving;
using RPG.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevTV.Inventories;

namespace RPG.Combat
{
    public class Fighter : MonoBehaviour, IAction, ISaveable
    {
        [SerializeField] private float _timeBetweenAttacks = 1f;

        [SerializeField] private Transform _rightHandTransform;
        [SerializeField] private Transform _leftHandTransform;

        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Transform _helmetTransform;
        [SerializeField] private Transform _bootsLeftTransform;
        [SerializeField] private Transform _bootsRightTransform;
        [SerializeField] private Transform _necklaceTransform;
        [SerializeField] private Transform _shieldTransform;
        [SerializeField] private Transform _glovesLeftTransform;
        [SerializeField] private Transform _glovesRightTransform;
        [SerializeField] private Transform _trousersTransform;
        [SerializeField] private Transform _upperArmLeftTransform;
        [SerializeField] private Transform _upperArmRightTransform;
        [SerializeField] private Transform _lowerArmLeftTransform;
        [SerializeField] private Transform _lowerArmRightTransform;

        [SerializeField] private WeaponConfig _defaultWeapon = null;
        [SerializeField] private ArmorConfig _defaultArmor = null;

        private ActionScheduler _actionScheduler;
        private Health _target;
        private Equipment _equipment;
        private Mover _mover;
        private Animator _animator;
        private float _timeSinceLastAttack = Mathf.Infinity;
        private WeaponConfig _currentWeaponConfig;
        private ArmorConfig _currentArmorConfig;
        private LazyValue<Weapon> _currentWeapon;
        private LazyValue<Armor> _currentArmor;

        private void Awake()
        {
            _actionScheduler = GetComponent<ActionScheduler>();
            _mover = GetComponent<Mover>();
            _animator = GetComponent<Animator>();

            _currentWeaponConfig = _defaultWeapon;
            _currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);

            _currentArmorConfig = _defaultArmor;
            _currentArmor = new LazyValue<Armor>(SetupDefaultArmor);

            _equipment = GetComponent<Equipment>();
            if (_equipment)
            {
                _equipment.equipmentUpdated += UpdateWeapon;
                _equipment.equipmentUpdated += UpdateArmor;
            }
        }

        private Weapon SetupDefaultWeapon()
        {    
            return AttachWeapon(_defaultWeapon);
        }

        private Armor SetupDefaultArmor()
        {
            return AttachArmor(_defaultArmor);
        }

        private void Start()
        {
            _currentWeapon.ForceInit();  
            _currentArmor.ForceInit();
        }

        public void EquipWeapon(WeaponConfig weapon)
        {
            _currentWeaponConfig = weapon;
            _currentWeapon.value = AttachWeapon(weapon);
        }

        public void EquipArmor(ArmorConfig armor)
        {
            _currentArmorConfig = armor;
            _currentArmor.value = AttachArmor(armor);
        }

        private void UpdateWeapon()
        {
            var weapon = _equipment.GetItemInSlot(EquipLocation.Weapon) as WeaponConfig;
            if(weapon == null)
            {
                EquipWeapon(_defaultWeapon);
            }
            else
            {
                EquipWeapon(weapon);
            }
        }
        
        private void UpdateArmor()
        {  
            CheckArmor(EquipLocation.Body);
            CheckArmor(EquipLocation.UpperArm);
            CheckArmor(EquipLocation.LowerArm);
            CheckArmor(EquipLocation.Helmet);
            CheckArmor(EquipLocation.Boots);
            CheckArmor(EquipLocation.Necklace);
            CheckArmor(EquipLocation.Shield);
            CheckArmor(EquipLocation.Gloves);
            CheckArmor(EquipLocation.Trousers);
        }

        private void CheckArmor(EquipLocation equipLocation)
        {
            var armor = _equipment.GetItemInSlot(equipLocation) as ArmorConfig;
            if (armor == null)
            {
                EquipArmor(_defaultArmor);
            }
            else
            {
                EquipArmor(armor);
            }
        }

        private Weapon AttachWeapon(WeaponConfig weapon)
        {
            return weapon.Spawn(_rightHandTransform, _leftHandTransform, _animator);
        }

        private Armor AttachArmor(ArmorConfig armor)
        {
            switch (armor.GetEquipLocation())
            {
                case EquipLocation.Body:
                    {
                        return armor.Spawn(_bodyTransform, _bodyTransform, _bodyTransform);
                    }
                case EquipLocation.UpperArm:
                    {
                        return armor.Spawn(_bodyTransform, _upperArmLeftTransform, _upperArmRightTransform);
                    }
                case EquipLocation.LowerArm:
                    {
                        return armor.Spawn(_bodyTransform, _lowerArmLeftTransform, _lowerArmRightTransform);
                    }
                case EquipLocation.Helmet:
                    {
                        return armor.Spawn(_helmetTransform, _bodyTransform, _bodyTransform);
                    }
                case EquipLocation.Boots:
                    {
                        return armor.Spawn(_bodyTransform, _bootsLeftTransform, _bootsRightTransform);
                    }
                case EquipLocation.Necklace:
                    {
                        return armor.Spawn(_necklaceTransform, _bodyTransform, _bodyTransform);
                    }
                case EquipLocation.Gloves:
                    {
                        return armor.Spawn(_bodyTransform, _glovesLeftTransform, _glovesRightTransform);
                    }
                case EquipLocation.Trousers:
                    {
                        return armor.Spawn(_trousersTransform, _bodyTransform, _bodyTransform);
                    }
                default: break;
            }
            return null;
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

            if (!GetIsInRange(_target.transform))
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

        private bool GetIsInRange(Transform targetTransform)
        {
            return Vector3.Distance(transform.position, targetTransform.position) < _currentWeaponConfig.GetRange();
        }

        public bool CanAttack(GameObject combatTarget)
        {
            if(combatTarget == null) return false;
            if (!_mover.CanMoveTo(combatTarget.transform.position) && !GetIsInRange(combatTarget.transform)) return false;
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

        public void Hit()
        {
            if (_target == null) return;

            float damage = GetComponent<BaseStats>().GetStat(Stat.Damage);

            if(_currentWeapon.value != null)
            {
                _currentWeapon.value.OnHit();
            }

            if (_currentWeaponConfig.HasProjectile()) _currentWeaponConfig.LaunchProjectile(_rightHandTransform, _leftHandTransform, _target, gameObject, damage);
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
            List<object> state = new List<object>();
            state.Add(_currentWeaponConfig.name);
            state.Add(_currentArmorConfig.name);
            return state;
        }

        public void RestoreState(object state)
        {
            string weaponName = (string)state;
            WeaponConfig weapon = Resources.Load<WeaponConfig>(weaponName);
            EquipWeapon(weapon);
            string armorName = (string)state;
            ArmorConfig armor = Resources.Load<ArmorConfig>(armorName);
            EquipArmor(armor);
        }
    }
}
