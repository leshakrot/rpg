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
using Newtonsoft.Json;

namespace RPG.Combat
{
	public class PlayerFighter : Fighter, IAction, ISaveable
	{
        [SerializeField] private float _timeBetweenAttacks = 1f;
        [SerializeField] private float _autoAttackRange = 4f;
        [SerializeField] private bool _isAutoattacking = false;

        [Header("Chest piece mounts (torso + arms)")]
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Transform _upperArmLeftTransform;
        [SerializeField] private Transform _upperArmRightTransform;
        [SerializeField] private Transform _lowerArmLeftTransform;
        [SerializeField] private Transform _lowerArmRightTransform;

        [Header("Trousers mounts (hips + legs)")]
        [SerializeField] private Transform _trousersTransform;
        [SerializeField] private Transform _bootsLeftTransform;
        [SerializeField] private Transform _bootsRightTransform;

        [Header("Other armor mounts")]
        [SerializeField] private Transform _helmetTransform;
        [SerializeField] private Transform _capeTransform;
        [SerializeField] private Transform _shieldTransform;
        [SerializeField] private Transform _glovesLeftTransform;
        [SerializeField] private Transform _glovesRightTransform;

        [Header("Default armor")]
        [SerializeField] private BodyArmorConfig _defaultBodyArmor = null;
        [SerializeField] private TrousersArmorConfig _defaultTrousersArmor = null;
        [SerializeField] private CapeArmorConfig _defaultCapeArmor = null;
        [SerializeField] private HelmetArmorConfig _defaultHelmetArmor = null;
        [SerializeField] private GloveLeftArmorConfig _defaultGloveLeftArmor = null;
        [SerializeField] private GloveRightArmorConfig _defaultGloveRightArmor = null;

        private ActionScheduler _actionScheduler;
        private Health _target;
        private Equipment _equipment;
		private Mover _mover;
		private Animator _animator;

        private float _timeSinceLastAttack = Mathf.Infinity;

        private WeaponConfig _currentWeaponConfig;
		private BodyArmorConfig _currentBodyArmorConfig;
		private TrousersArmorConfig _currentTrousersArmorConfig;
		private CapeArmorConfig _currentCapeArmorConfig;
        private HelmetArmorConfig _currentHelmetArmorConfig;
        private GloveLeftArmorConfig _currentGloveLeftArmorConfig;
        private GloveRightArmorConfig _currentGloveRightArmorConfig;

        private LazyValue<Weapon> _currentWeapon;
		private LazyValue<CapeArmor> _currentCapeArmor;
        private LazyValue<HelmetArmor> _currentHelmetArmor;
        private LazyValue<GloveLeftArmor> _currentGloveLeftArmor;
        private LazyValue<GloveRightArmor> _currentGloveRightArmor;

        private void Awake()
        {
            _actionScheduler = GetComponent<ActionScheduler>();
	        _mover = GetComponent<Mover>();
	        _animator = GetComponent<Animator>();

            _currentWeaponConfig = defaultWeapon;
            _currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);

            _currentBodyArmorConfig = _defaultBodyArmor;
            _currentTrousersArmorConfig = _defaultTrousersArmor;

	        _currentCapeArmorConfig = _defaultCapeArmor;
	        _currentCapeArmor = new LazyValue<CapeArmor>(SetupDefaultCapeArmor);

            _currentHelmetArmorConfig = _defaultHelmetArmor;
            _currentHelmetArmor = new LazyValue<HelmetArmor>(SetupDefaultHelmetArmor);

            _currentGloveLeftArmorConfig = _defaultGloveLeftArmor;
            _currentGloveLeftArmor = new LazyValue<GloveLeftArmor>(SetupDefaultGloveLeftArmor);

            _currentGloveRightArmorConfig = _defaultGloveRightArmor;
            _currentGloveRightArmor = new LazyValue<GloveRightArmor>(SetupDefaultGloveRightArmor);

            _equipment = GetComponent<Equipment>();
            if (_equipment)
            {
                _equipment.equipmentUpdated += UpdateWeapon;
	            _equipment.equipmentUpdated += UpdateBodyArmor;
	            _equipment.equipmentUpdated += UpdateTrousersArmor;
	            _equipment.equipmentUpdated += UpdateCapeArmor;
                _equipment.equipmentUpdated += UpdateHelmetArmor;
                _equipment.equipmentUpdated += UpdateGloveLeftArmor;
                _equipment.equipmentUpdated += UpdateGloveRightArmor;
            }
        }

        private Weapon SetupDefaultWeapon()
        {
            return AttachWeapon(defaultWeapon);
        }

		private CapeArmor SetupDefaultCapeArmor()
		{
			return AttachArmor(_defaultCapeArmor);
		}

        private HelmetArmor SetupDefaultHelmetArmor()
        {
            return AttachArmor(_defaultHelmetArmor);
        }

        private GloveLeftArmor SetupDefaultGloveLeftArmor()
        {
            return AttachArmor(_defaultGloveLeftArmor);
        }

        private GloveRightArmor SetupDefaultGloveRightArmor()
        {
            return AttachArmor(_defaultGloveRightArmor);
        }

        private void Start()
        {
            _currentWeapon.ForceInit();
	        _currentCapeArmor.ForceInit();
            _currentHelmetArmor.ForceInit();
            _currentGloveLeftArmor.ForceInit();
            _currentGloveRightArmor.ForceInit();

            // Composite armor is spawned directly - one call attaches every modular part.
            EquipArmor(_currentBodyArmorConfig);
            EquipArmor(_currentTrousersArmorConfig);
        }

        public void EquipWeapon(WeaponConfig weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("Попытка экипировать несуществующее оружие! Экипируется оружие по умолчанию.");
                if (defaultWeapon != null)
                {
                    _currentWeaponConfig = defaultWeapon;
                    _currentWeapon.value = AttachWeapon(defaultWeapon);
                }
                return;
            }
            _currentWeaponConfig = weapon;
            _currentWeapon.value = AttachWeapon(weapon);
        }

        public void EquipArmor(BodyArmorConfig armor)
        {
            _currentBodyArmorConfig = armor;
            if (armor == null) return;
            armor.Spawn(BuildBodyMounts());
        }

        public void EquipArmor(TrousersArmorConfig armor)
        {
            _currentTrousersArmorConfig = armor;
            if (armor == null) return;
            armor.Spawn(BuildTrousersMounts());
        }

		public void EquipArmor(CapeArmorConfig armor)
		{
			_currentCapeArmorConfig = armor;
			_currentCapeArmor.value = AttachArmor(armor);
		}
        public void EquipArmor(HelmetArmorConfig armor)
        {
            _currentHelmetArmorConfig = armor;
            _currentHelmetArmor.value = AttachArmor(armor);
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

        private BodyArmorConfig.Mounts BuildBodyMounts()
        {
            return new BodyArmorConfig.Mounts
            {
                Torso = _bodyTransform,
                UpperArmLeft = _upperArmLeftTransform,
                UpperArmRight = _upperArmRightTransform,
                LowerArmLeft = _lowerArmLeftTransform,
                LowerArmRight = _lowerArmRightTransform,
            };
        }

        private TrousersArmorConfig.Mounts BuildTrousersMounts()
        {
            return new TrousersArmorConfig.Mounts
            {
                Hips = _trousersTransform,
                LegLeft = _bootsLeftTransform,
                LegRight = _bootsRightTransform,
            };
        }

        private void UpdateWeapon()
        {
            var weapon = _equipment.GetItemInSlot(EquipLocation.Weapon) as WeaponConfig;
            if(weapon == null)
            {
                EquipWeapon(defaultWeapon);
            }
            else
            {
                EquipWeapon(weapon);
            }
        }

        private void UpdateBodyArmor()
        {
            var armor = _equipment.GetItemInSlot(EquipLocation.Body) as BodyArmorConfig;
            EquipArmor(armor != null ? armor : _defaultBodyArmor);
        }

        private void UpdateTrousersArmor()
        {
            var armor = _equipment.GetItemInSlot(EquipLocation.Trousers) as TrousersArmorConfig;
            EquipArmor(armor != null ? armor : _defaultTrousersArmor);
        }

		private void UpdateCapeArmor()
		{
			CheckArmor(EquipLocation.Cape);
		}
        private void UpdateHelmetArmor()
        {
            CheckArmor(EquipLocation.Helmet);
        }
        private void UpdateGloveLeftArmor()
        {
            CheckArmor(EquipLocation.GlovesLeft);
        }
        private void UpdateGloveRightArmor()
        {
	        CheckArmor(EquipLocation.GlovesRight);
        }

        private void CheckArmor(EquipLocation equipLocation)
        {
            switch (equipLocation)
            {
                case EquipLocation.Cape:
	            {
		            var armor = _equipment.GetItemInSlot(equipLocation) as CapeArmorConfig;
		            if (armor == null)
		            {
			            EquipArmor(_defaultCapeArmor);
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
                case EquipLocation.GlovesLeft:
                    {
                        var armor = _equipment.GetItemInSlot(equipLocation) as GloveLeftArmorConfig;
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
                default: break;
            }
        }

        private Weapon AttachWeapon(WeaponConfig weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("AttachWeapon: weapon == null");
                return null;
            }
            return weapon.Spawn(rightHandTransform, leftHandTransform, _animator);
        }

		private CapeArmor AttachArmor(CapeArmorConfig armor)
		{
			return armor.Spawn(_capeTransform);
		}

        private HelmetArmor AttachArmor(HelmetArmorConfig armor)
        {
            return armor.Spawn(_helmetTransform);
        }

        private GloveLeftArmor AttachArmor(GloveLeftArmorConfig armor)
        {
            return armor.Spawn(_glovesLeftTransform);
        }
        private GloveRightArmor AttachArmor(GloveRightArmorConfig armor)
        {
            return armor.Spawn(_glovesRightTransform);
        }

        public Health GetTarget()
        {
            return _target;
        }

        public Transform GetHandTransform(bool isRightHand)
        {
            if (isRightHand)
            {
                return rightHandTransform;
            }
            else
            {
                return leftHandTransform;
            }
        }

        /// <summary>
        /// Скрывает текущее оружие игрока (например, во время рыбалки)
        /// </summary>
        public void HideWeapon()
        {
            if (_currentWeapon.value != null)
            {
                _currentWeapon.value.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Показывает текущее оружие игрока обратно
        /// </summary>
        public void ShowWeapon()
        {
            if (_currentWeapon.value != null)
            {
                _currentWeapon.value.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            _timeSinceLastAttack += Time.deltaTime;

            if (_target == null) return;

            if (_target.IsDead())
            {
                if (!_isAutoattacking) return;
                _target = FindNewTargetInRange(_autoAttackRange);
                if (_target == null) return;
            }

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
			Debug.Log("AttackBehaviour");
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
			Debug.Log("CanAttack");
            if(combatTarget == null) return false;
            if (!_mover.CanMoveTo(combatTarget.transform.position) && !GetIsInRange(combatTarget.transform)) return false;
            Health targetToTest = combatTarget.GetComponent<Health>();
            return targetToTest != null && !targetToTest.IsDead();
        }

        public void Attack(GameObject combatTarget)
		{
			Debug.Log("Attack");
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

            if (_currentWeaponConfig.HasProjectile()) _currentWeaponConfig.LaunchProjectile(rightHandTransform, leftHandTransform, _target, gameObject, damage);
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
			// Создаем словарь для хранения всех данных о текущем оружии и броне
			Dictionary<string, string> state = new Dictionary<string, string>
			{
				{ "Weapon", _currentWeaponConfig.name },
				{ "BodyArmor", _currentBodyArmorConfig?.name },
				{ "TrousersArmor", _currentTrousersArmorConfig?.name },
				{ "CapeArmor", _currentCapeArmorConfig?.name },
				{ "HelmetArmor", _currentHelmetArmorConfig?.name },
				{ "GloveLeftArmor", _currentGloveLeftArmorConfig?.name },
				{ "GloveRightArmor", _currentGloveRightArmorConfig?.name }
			};

			return state;
		}

        // Игрок не переключается на компаньона при автоатаке после убийства врага
        protected override IEnumerable<Health> FindAllTargetsInRange(float range)
        {
            foreach (Health h in base.FindAllTargetsInRange(range))
            {
                if (!h.gameObject.CompareTag("Companion"))
                    yield return h;
            }
        }

		public void RestoreState(object state)
		{
			// Преобразуем состояние обратно в словарь
			Dictionary<string, string> savedState = (Dictionary<string, string>)state;

			// Восстанавливаем оружие
			if (savedState.ContainsKey("Weapon"))
			{
				string weaponName = savedState["Weapon"];
				WeaponConfig weapon = UnityEngine.Resources.Load<WeaponConfig>(weaponName);
				EquipWeapon(weapon);
			}

			// Восстанавливаем броню
			if (savedState.ContainsKey("BodyArmor"))
			{
				string armorName = savedState["BodyArmor"];
				BodyArmorConfig armor = UnityEngine.Resources.Load<BodyArmorConfig>(armorName);
				EquipArmor(armor);
			}

			if (savedState.ContainsKey("TrousersArmor"))
			{
				string armorName = savedState["TrousersArmor"];
				TrousersArmorConfig armor = UnityEngine.Resources.Load<TrousersArmorConfig>(armorName);
				EquipArmor(armor);
			}

			if (savedState.ContainsKey("CapeArmor"))
			{
				string armorName = savedState["CapeArmor"];
				CapeArmorConfig armor = UnityEngine.Resources.Load<CapeArmorConfig>(armorName);
				EquipArmor(armor);
			}

			if (savedState.ContainsKey("HelmetArmor"))
			{
				string armorName = savedState["HelmetArmor"];
				HelmetArmorConfig armor = UnityEngine.Resources.Load<HelmetArmorConfig>(armorName);
				EquipArmor(armor);
			}

			if (savedState.ContainsKey("GloveLeftArmor"))
			{
				string armorName = savedState["GloveLeftArmor"];
				GloveLeftArmorConfig armor = UnityEngine.Resources.Load<GloveLeftArmorConfig>(armorName);
				EquipArmor(armor);
			}

			if (savedState.ContainsKey("GloveRightArmor"))
			{
				string armorName = savedState["GloveRightArmor"];
				GloveRightArmorConfig armor = UnityEngine.Resources.Load<GloveRightArmorConfig>(armorName);
				EquipArmor(armor);
			}
		}
    }
}
