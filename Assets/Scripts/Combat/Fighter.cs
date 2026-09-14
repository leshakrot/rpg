using UnityEngine;
using RPG.Movement;
using RPG.Core;
using GameDevTV.Saving;
using RPG.Attributes;
using RPG.Stats;
using System.Collections.Generic;
using GameDevTV.Utils;
using System;
using GameDevTV.Inventories;

namespace RPG.Combat
{
    public class Fighter : MonoBehaviour, IAction
    {
        [SerializeField] float timeBetweenAttacks = 1f;
        [SerializeField] float autoAttackRange = 4f;

        [Header("Combat feel")]
        [Tooltip("Разброс интервала между атаками (0.15 = +/-15%), чтобы ритм боя не был метрономом.")]
        [SerializeField] [Range(0f, 0.5f)] float attackTimingVariance = 0.15f;
        [Tooltip("Максимальный шанс уклонения цели, вычисляемый из её защиты.")]
        [SerializeField] [Range(0f, 1f)] float maxDodgeChance = 0.3f;
        [Tooltip("Сколько последовательных ударов по одной цели усиливают урон.")]
        [SerializeField] int maxComboStacks = 5;
        [Tooltip("Прирост урона за каждый удар в комбо (0.05 = +5% за стак).")]
        [SerializeField] [Range(0f, 0.2f)] float comboDamagePerStack = 0.05f;

        private const float DodgeDefenceScale = 100f;
        private const string DodgeTriggerName = "dodge";
        private const string CriticalHitTriggerName = "criticalHit";

        public WeaponConfig defaultWeapon = null;
        public Transform rightHandTransform = null;
        public Transform leftHandTransform = null;

        Health target;
        Equipment equipment;
        float timeSinceLastAttack = Mathf.Infinity;
        WeaponConfig currentWeaponConfig;
        LazyValue<Weapon> currentWeapon;

        // Текущая длительность до следующей атаки — пересчитывается с разбросом после каждого удара
        private float nextAttackDelay;

        // Комбо: сколько раз подряд мы попали по одной и той же цели
        private int comboStacks = 0;
        private Health lastComboTarget;

        // Кэшируем компоненты — не используем GetComponent в Update каждый кадр
        private Mover _mover;
        private Animator _animator;
        private BaseStats _baseStats;

        private void Awake()
        {
            currentWeaponConfig = defaultWeapon;
            currentWeapon = new LazyValue<Weapon>(SetupDefaultWeapon);
            equipment = GetComponent<Equipment>();
            if (equipment)
            {
                equipment.equipmentUpdated += UpdateWeapon;
            }

            _mover = GetComponent<Mover>();
            _animator = GetComponent<Animator>();
            _baseStats = GetComponent<BaseStats>();

            nextAttackDelay = timeBetweenAttacks;
        }

        private Weapon SetupDefaultWeapon()
        {
            return AttachWeapon(defaultWeapon);
        }

        private void Start()
        {
            currentWeapon.ForceInit();
        }

        private void Update()
        {
            timeSinceLastAttack += Time.deltaTime;

            if (target == null) return;
            if (target.IsDead())
            {
                target = FindNewTargetInRange(autoAttackRange);
                if (target == null) return;
            }

            if (!GetIsInRange(target.transform))
            {
                _mover.MoveTo(target.transform.position, 1f);
            }
            else
            {
                _mover.Cancel();
                AttackBehaviour();
            }
        }

        public void EquipWeapon(WeaponConfig weapon)
        {
            currentWeaponConfig = weapon;
            currentWeapon.value = AttachWeapon(weapon);
        }

        private void UpdateWeapon()
        {
            var weapon = equipment.GetItemInSlot(EquipLocation.Weapon) as WeaponConfig;
            if (weapon == null)
            {
                EquipWeapon(defaultWeapon);
            }
            else
            {
                EquipWeapon(weapon);
            }
        }

        private Weapon AttachWeapon(WeaponConfig weapon)
        {
            return weapon.Spawn(rightHandTransform, leftHandTransform, _animator);
        }

        public Health GetTarget()
        {
            return target;
        }

        public Transform GetHandTransform(bool isRightHand)
        {
            return isRightHand ? rightHandTransform : leftHandTransform;
        }

        private void AttackBehaviour()
        {
            transform.LookAt(target.transform);
            if (timeSinceLastAttack > nextAttackDelay)
            {
                TriggerAttack();
                timeSinceLastAttack = 0;
                // Каждая следующая атака чуть раньше или чуть позже — бой не выглядит как метроном
                nextAttackDelay = timeBetweenAttacks * UnityEngine.Random.Range(1f - attackTimingVariance, 1f + attackTimingVariance);
            }
        }

        public Health FindNewTargetInRange(float range)
        {
            Health best = null;
            float bestDistance = Mathf.Infinity;
            foreach (var candidate in FindAllTargetsInRange(range))
            {
                float candidateDistance = Vector3.Distance(
                    transform.position, candidate.transform.position);
                if (candidateDistance < bestDistance)
                {
                    best = candidate;
                    bestDistance = candidateDistance;
                }
            }
            return best;
        }

        protected virtual IEnumerable<Health> FindAllTargetsInRange(float range)
        {
            bool iAmAnEnemy = GetComponent<RPG.Control.AIController>() != null;

            RaycastHit[] raycastHits = Physics.SphereCastAll(transform.position, range, Vector3.up);
            foreach (var hit in raycastHits)
            {
                Health health = hit.transform.GetComponent<Health>();
                if (health == null) continue;
                if (health.IsDead()) continue;
                if (health.gameObject == gameObject) continue;

                // Компаньон — союзник для всех
                if (hit.transform.CompareTag("Companion")) continue;

                // Враги не атакуют друг друга
                bool targetIsEnemy = hit.transform.GetComponent<RPG.Control.AIController>() != null;
                if (iAmAnEnemy && targetIsEnemy) continue;

                yield return health;
            }
        }

        private void TriggerAttack()
        {
            _animator.ResetTrigger("stopAttack");
            _animator.SetTrigger("attack");
        }

        // Animation Event
        public void Hit()
        {
            if (target == null) return;

            BaseStats targetBaseStats = target.GetComponent<BaseStats>();

            // Цель может уклониться — шанс растёт с её защитой, что делает статы реально важными
            if (RollDodge(targetBaseStats))
            {
                ResetCombo();
                target.GetComponent<Animator>()?.SetTrigger(DodgeTriggerName);
                return;
            }

            float damage = _baseStats.GetStat(Stat.Damage);
            if (targetBaseStats != null)
            {
                float defence = targetBaseStats.GetStat(Stat.Defence);
                damage /= 1 + defence / damage;
            }

            damage = ApplyComboBonus(damage);
            damage = ApplyDamageVarianceAndCrit(damage);

            if (currentWeapon.value != null)
            {
                currentWeapon.value.OnHit();
            }

            if (currentWeaponConfig.HasProjectile())
            {
                currentWeaponConfig.LaunchProjectile(rightHandTransform, leftHandTransform, target, gameObject, damage);
            }
            else
            {
                target.TakeDamage(gameObject, damage);
            }
        }

        /// <summary>
        /// Вычисляет шанс цели уклониться от атаки на основе её характеристики защиты.
        /// Ограничен maxDodgeChance, чтобы уклонение не делало бой непредсказуемым.
        /// </summary>
        private bool RollDodge(BaseStats targetBaseStats)
        {
            if (targetBaseStats == null) return false;
            float defence = targetBaseStats.GetStat(Stat.Defence);
            float dodgeChance = Mathf.Clamp(defence / (defence + DodgeDefenceScale), 0f, maxDodgeChance);
            return UnityEngine.Random.value < dodgeChance;
        }

        /// <summary>
        /// Усиливает урон за серию последовательных попаданий по одной цели.
        /// Комбо сбрасывается при смене цели, промахе или отмене атаки.
        /// </summary>
        private float ApplyComboBonus(float damage)
        {
            if (target != lastComboTarget)
            {
                lastComboTarget = target;
                comboStacks = 0;
            }

            float bonus = 1f + Mathf.Min(comboStacks, maxComboStacks) * comboDamagePerStack;
            comboStacks++;
            return damage * bonus;
        }

        private void ResetCombo()
        {
            comboStacks = 0;
            lastComboTarget = null;
        }

        /// <summary>
        /// Добавляет случайный разброс урона и, с шансом оружия, критический удар.
        /// </summary>
        private float ApplyDamageVarianceAndCrit(float damage)
        {
            float variance = currentWeaponConfig.GetDamageVariance();
            if (variance > 0f)
            {
                damage *= UnityEngine.Random.Range(1f - variance, 1f + variance);
            }

            if (UnityEngine.Random.value < currentWeaponConfig.GetCriticalChance())
            {
                damage *= currentWeaponConfig.GetCriticalMultiplier();
                _animator.SetTrigger(CriticalHitTriggerName);
            }

            return damage;
        }

        void Shoot()
        {
            Hit();
        }

        private bool GetIsInRange(Transform targetTransform)
        {
            return Vector3.Distance(transform.position, targetTransform.position) < currentWeaponConfig.GetRange();
        }

        /// <summary>
        /// Проверяет может ли Fighter атаковать цель.
        /// Убрана проверка CanMoveTo() — это дорогой вызов NavMesh каждый кадр,
        /// который раньше давал ложный false и мешал переходу в Chase.
        /// Fighter.Update() сам справляется с движением к цели.
        /// </summary>
        public virtual bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null) return false;
            Health targetToTest = combatTarget.GetComponent<Health>();
            return targetToTest != null && !targetToTest.IsDead();
        }

        public void Attack(GameObject combatTarget)
        {
            GetComponent<ActionScheduler>().StartAction(this);
            target = combatTarget.GetComponent<Health>();
        }

        public void Cancel()
        {
            StopAttack();
            target = null;
            ResetCombo();
            _mover.Cancel();
        }

        private void StopAttack()
        {
            _animator.ResetTrigger("attack");
            _animator.SetTrigger("stopAttack");
        }
    }
}
