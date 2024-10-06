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
        [SerializeField] private BodyArmorConfig _defaultBodyArmor = null;
        [SerializeField] private UpperArmLeftArmorConfig _defaultUpperArmLeftArmor = null;
        [SerializeField] private UpperArmRightArmorConfig _defaultUpperArmRightArmor = null;
        [SerializeField] private LowerArmLeftArmorConfig _defaultLowerArmLeftArmor = null;
        [SerializeField] private LowerArmRightArmorConfig _defaultLowerArmRightArmor = null;
        [SerializeField] private HelmetArmorConfig _defaultHelmetArmor = null;
        [SerializeField] private BootLeftArmorConfig _defaultBootLeftArmor = null;
        [SerializeField] private BootRightArmorConfig _defaultBootRightArmor = null;
        [SerializeField] private GloveLeftArmorConfig _defaultGloveLeftArmor = null;
        [SerializeField] private GloveRightArmorConfig _defaultGloveRightArmor = null;
        [SerializeField] private TrousersArmorConfig _defaultTrousersArmor = null;

        private ActionScheduler _actionScheduler;
        private Health _target;
        private Equipment _equipment;
        private Mover _mover;
        private Animator _animator;
        private float _timeSinceLastAttack = Mathf.Infinity;

        private WeaponConfig _currentWeaponConfig;
        private BodyArmorConfig _currentBodyArmorConfig;
        private UpperArmLeftArmorConfig _currentUpperArmLeftArmorConfig;
        private UpperArmRightArmorConfig _currentUpperArmRightArmorConfig;
        private LowerArmLeftArmorConfig _currentLowerArmLeftArmorConfig;
        private LowerArmRightArmorConfig _currentLowerArmRightArmorConfig;
        private HelmetArmorConfig _currentHelmetArmorConfig;
        private BootLeftArmorConfig _currentBootLeftArmorConfig;
        private BootRightArmorConfig _currentBootRightArmorConfig;
        private GloveLeftArmorConfig _currentGloveLeftArmorConfig;
        private GloveRightArmorConfig _currentGloveRightArmorConfig;
        private TrousersArmorConfig _currentTrousersArmorConfig;

        private LazyValue<Weapon> _currentWeapon;
        private LazyValue<BodyArmor> _currentBodyArmor;
        private LazyValue<UpperArmLeftArmor> _currentUpperArmLeftArmor;
        private LazyValue<UpperArmRightArmor> _currentUpperArmRightArmor;
        private LazyValue<LowerArmLeftArmor> _currentLowerArmLeftArmor;
        private LazyValue<LowerArmRightArmor> _currentLowerArmRightArmor;
        private LazyValue<HelmetArmor> _currentHelmetArmor;
        private LazyValue<BootLeftArmor> _currentBootLeftArmor;
        private LazyValue<BootRightArmor> _currentBootRightArmor;
        private LazyValue<GloveLeftArmor> _currentGloveLeftArmor;
        private LazyValue<GloveRightArmor> _currentGloveRightArmor;
        private LazyValue<TrousersArmor> _currentTrousersArmor;

        private void Awake()
        {
            _actionScheduler = GetComponent<ActionScheduler>();
            _mover = GetComponent<Mover>();
            _animator = GetComponent<Animator>();

            _currentWeaponConfig = _defaultWeapon;
            _currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);

            _currentBodyArmorConfig = _defaultBodyArmor;
            _currentBodyArmor = new LazyValue<BodyArmor>(SetupDefaultBodyArmor);

            _currentUpperArmLeftArmorConfig = _defaultUpperArmLeftArmor;
            _currentUpperArmLeftArmor = new LazyValue<UpperArmLeftArmor>(SetupDefaultUpperArmLeftArmor);

            _currentUpperArmRightArmorConfig = _defaultUpperArmRightArmor;
            _currentUpperArmRightArmor = new LazyValue<UpperArmRightArmor>(SetupDefaultUpperArmRightArmor);

            _currentLowerArmLeftArmorConfig = _defaultLowerArmLeftArmor;
            _currentLowerArmLeftArmor = new LazyValue<LowerArmLeftArmor>(SetupDefaultLowerArmLeftArmor);

            _currentLowerArmRightArmorConfig = _defaultLowerArmRightArmor;
            _currentLowerArmRightArmor = new LazyValue<LowerArmRightArmor>(SetupDefaultLowerArmRightArmor);

            _currentHelmetArmorConfig = _defaultHelmetArmor;
            _currentHelmetArmor = new LazyValue<HelmetArmor>(SetupDefaultHelmetArmor);

            _currentBootLeftArmorConfig = _defaultBootLeftArmor;
            _currentBootLeftArmor = new LazyValue<BootLeftArmor>(SetupDefaultBootLeftArmor);

            _currentBootRightArmorConfig = _defaultBootRightArmor;
            _currentBootRightArmor = new LazyValue<BootRightArmor>(SetupDefaultBootRightArmor);

            _currentGloveLeftArmorConfig = _defaultGloveLeftArmor;
            _currentGloveLeftArmor = new LazyValue<GloveLeftArmor>(SetupDefaultGloveLeftArmor);

            _currentGloveRightArmorConfig = _defaultGloveRightArmor;
            _currentGloveRightArmor = new LazyValue<GloveRightArmor>(SetupDefaultGloveRightArmor);

            _currentTrousersArmorConfig = _defaultTrousersArmor;
            _currentTrousersArmor = new LazyValue<TrousersArmor>(SetupDefaultTrousersArmor);

            _equipment = GetComponent<Equipment>();
            if (_equipment)
            {
                _equipment.equipmentUpdated += UpdateWeapon;
                _equipment.equipmentUpdated += UpdateBodyArmor;
                _equipment.equipmentUpdated += UpdateUpperArmLeftArmor;
                _equipment.equipmentUpdated += UpdateUpperArmRightArmor;
                _equipment.equipmentUpdated += UpdateLowerArmLeftArmor;
                _equipment.equipmentUpdated += UpdateLowerArmRightArmor;
                _equipment.equipmentUpdated += UpdateHelmetArmor;
                _equipment.equipmentUpdated += UpdateBootLeftArmor;
                _equipment.equipmentUpdated += UpdateBootRightArmor;
                _equipment.equipmentUpdated += UpdateGloveLeftArmor;
                _equipment.equipmentUpdated += UpdateGloveRightArmor;
                _equipment.equipmentUpdated += UpdateTrousersArmor;
            }
        }

        private Weapon SetupDefaultWeapon()
        {    
            return AttachWeapon(_defaultWeapon);
        }

        private BodyArmor SetupDefaultBodyArmor()
        {
            return AttachArmor(_defaultBodyArmor);
        }

        private UpperArmLeftArmor SetupDefaultUpperArmLeftArmor()
        {
            return AttachArmor(_defaultUpperArmLeftArmor);
        }

        private UpperArmRightArmor SetupDefaultUpperArmRightArmor()
        {
            return AttachArmor(_defaultUpperArmRightArmor);
        }

        private LowerArmLeftArmor SetupDefaultLowerArmLeftArmor()
        {
            return AttachArmor(_defaultLowerArmLeftArmor);
        }

        private LowerArmRightArmor SetupDefaultLowerArmRightArmor()
        {
            return AttachArmor(_defaultLowerArmRightArmor);
        }

        private HelmetArmor SetupDefaultHelmetArmor()
        {
            return AttachArmor(_defaultHelmetArmor);
        }

        private BootLeftArmor SetupDefaultBootLeftArmor()
        {
            return AttachArmor(_defaultBootLeftArmor);
        }

        private BootRightArmor SetupDefaultBootRightArmor()
        {
            return AttachArmor(_defaultBootRightArmor);
        }

        private GloveLeftArmor SetupDefaultGloveLeftArmor()
        {
            return AttachArmor(_defaultGloveLeftArmor);
        }

        private GloveRightArmor SetupDefaultGloveRightArmor()
        {
            return AttachArmor(_defaultGloveRightArmor);
        }

        private TrousersArmor SetupDefaultTrousersArmor()
        {
            return AttachArmor(_defaultTrousersArmor);
        }

        private void Start()
        {
            _currentWeapon.ForceInit();  
            _currentBodyArmor.ForceInit();
            _currentUpperArmLeftArmor.ForceInit();
            _currentUpperArmRightArmor.ForceInit();
            _currentLowerArmLeftArmor.ForceInit();
            _currentLowerArmRightArmor.ForceInit();
            _currentHelmetArmor.ForceInit();
            _currentBootLeftArmor.ForceInit();
            _currentBootRightArmor.ForceInit();
            _currentGloveLeftArmor.ForceInit();
            _currentGloveRightArmor.ForceInit();
            _currentTrousersArmor.ForceInit();
        }

        public void EquipWeapon(WeaponConfig weapon)
        {
            _currentWeaponConfig = weapon;
            _currentWeapon.value = AttachWeapon(weapon);
        }

        public void EquipArmor(BodyArmorConfig armor)
        {
            _currentBodyArmorConfig = armor;
            _currentBodyArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(UpperArmLeftArmorConfig armor)
        {
            _currentUpperArmLeftArmorConfig = armor;
            _currentUpperArmLeftArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(UpperArmRightArmorConfig armor)
        {
            _currentUpperArmRightArmorConfig = armor;
            _currentUpperArmRightArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(LowerArmLeftArmorConfig armor)
        {
            _currentLowerArmLeftArmorConfig = armor;
            _currentLowerArmLeftArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(LowerArmRightArmorConfig armor)
        {
            _currentLowerArmRightArmorConfig = armor;
            _currentLowerArmRightArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(HelmetArmorConfig armor)
        {
            _currentHelmetArmorConfig = armor;
            _currentHelmetArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(BootLeftArmorConfig armor)
        {
            _currentBootLeftArmorConfig = armor;
            _currentBootLeftArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(BootRightArmorConfig armor)
        {
            _currentBootRightArmorConfig = armor;
            _currentBootRightArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(GloveLeftArmorConfig armor)
        {
            _currentGloveLeftArmorConfig = armor;
            _currentGloveLeftArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(GloveRightArmorConfig armor)
        {
            _currentGloveRightArmorConfig = armor;
            _currentGloveRightArmor.value = AttachArmor(armor);
        }
        public void EquipArmor(TrousersArmorConfig armor)
        {
            _currentTrousersArmorConfig = armor;
            _currentTrousersArmor.value = AttachArmor(armor);
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

        private void UpdateBodyArmor()
        {
            CheckArmor(EquipLocation.Body);
        }
        private void UpdateUpperArmLeftArmor()
        {
            CheckArmor(EquipLocation.UpperArmLeft);
        }
        private void UpdateUpperArmRightArmor()
        {
            CheckArmor(EquipLocation.UpperArmRight);
        }
        private void UpdateLowerArmLeftArmor()
        {
            CheckArmor(EquipLocation.LowerArmLeft);
        }
        private void UpdateLowerArmRightArmor()
        {
            CheckArmor(EquipLocation.LowerArmRight);
        }
        private void UpdateHelmetArmor()
        {
            CheckArmor(EquipLocation.Helmet);
        }
        private void UpdateBootLeftArmor()
        {
            CheckArmor(EquipLocation.BootsLeft);
        }
        private void UpdateBootRightArmor()
        {
            CheckArmor(EquipLocation.BootsRight);
        }
        private void UpdateGloveLeftArmor()
        {
            CheckArmor(EquipLocation.GlovesLeft);
        }
        private void UpdateGloveRightArmor()
        {
            CheckArmor(EquipLocation.GlovesLeft);
        }
        private void UpdateTrousersArmor()
        {
            CheckArmor(EquipLocation.Trousers);
        }

        //private void UpdateShield()
        //{            
        //    CheckArmor(EquipLocation.Shield);                  
        //}

        //private void UpdateNecklace()
        //{
        //    CheckArmor(EquipLocation.Necklace);
        //}

        private void CheckArmor(EquipLocation equipLocation)
        {
            switch (equipLocation)
            {
                case EquipLocation.Body:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as BodyArmorConfig;
                        _defaultBodyArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultBodyArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.UpperArmLeft:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as UpperArmLeftArmorConfig;
                        _defaultUpperArmLeftArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultUpperArmLeftArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.UpperArmRight:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as UpperArmRightArmorConfig;
                        _defaultUpperArmRightArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultUpperArmRightArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.LowerArmLeft:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as LowerArmLeftArmorConfig;
                        _defaultLowerArmLeftArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultLowerArmLeftArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;

                    }
                case EquipLocation.LowerArmRight:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as LowerArmRightArmorConfig;
                        _defaultLowerArmRightArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultLowerArmRightArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;

                    }
                case EquipLocation.Helmet:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as HelmetArmorConfig;
                        _defaultHelmetArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultHelmetArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.BootsLeft:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as BootLeftArmorConfig;
                        _defaultBootLeftArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultBootLeftArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.BootsRight:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as BootRightArmorConfig;
                        _defaultBootRightArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultBootRightArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.GlovesLeft:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as GloveLeftArmorConfig;
                        _defaultGloveLeftArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultGloveLeftArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.GlovesRight:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as GloveRightArmorConfig;
                        _defaultGloveRightArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultGloveRightArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                case EquipLocation.Trousers:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as TrousersArmorConfig;
                        _defaultTrousersArmor.SetupEquipLocation(equipLocation);
                        if (armor == null)
                        {
                            EquipArmor(_defaultTrousersArmor);
                        }
                        else
                        {
                            EquipArmor(armor);
                        }
                        break;
                    }
                default: break;
            }
        }

        private Weapon AttachWeapon(WeaponConfig weapon)
        {
            return weapon.Spawn(_rightHandTransform, _leftHandTransform, _animator);
        }

        private BodyArmor AttachArmor(BodyArmorConfig armor)
        {
            return armor.Spawn(_bodyTransform);
        }

        private UpperArmLeftArmor AttachArmor(UpperArmLeftArmorConfig armor)
        {
            return armor.Spawn(_upperArmLeftTransform);           
        }

        private UpperArmRightArmor AttachArmor(UpperArmRightArmorConfig armor)
        {
            return armor.Spawn(_upperArmRightTransform);
        }

        private LowerArmLeftArmor AttachArmor(LowerArmLeftArmorConfig armor)
        {
            return armor.Spawn(_lowerArmLeftTransform);
        }
        private LowerArmRightArmor AttachArmor(LowerArmRightArmorConfig armor)
        {
            return armor.Spawn(_lowerArmRightTransform);
        }

        private HelmetArmor AttachArmor(HelmetArmorConfig armor)
        {
            return armor.Spawn(_helmetTransform);
        }

        private BootLeftArmor AttachArmor(BootLeftArmorConfig armor)
        {
            return armor.Spawn(_bootsLeftTransform);
        }
        private BootRightArmor AttachArmor(BootRightArmorConfig armor)
        {
            return armor.Spawn(_bootsRightTransform);
        }

        private GloveLeftArmor AttachArmor(GloveLeftArmorConfig armor)
        {
            return armor.Spawn(_glovesLeftTransform);
        }
        private GloveRightArmor AttachArmor(GloveRightArmorConfig armor)
        {
            return armor.Spawn(_glovesRightTransform);
        }

        private TrousersArmor AttachArmor(TrousersArmorConfig armor)
        {
            return armor.Spawn(_trousersTransform);
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
            state.Add(_currentBodyArmorConfig.name);
            return state;
        }

        public void RestoreState(object state)
        {
            string weaponName = (string)state;
            WeaponConfig weapon = Resources.Load<WeaponConfig>(weaponName);
            EquipWeapon(weapon);
            string armorName = (string)state;
            BodyArmorConfig armor = Resources.Load<BodyArmorConfig>(armorName);
            EquipArmor(armor);
        }
    }
}
