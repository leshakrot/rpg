using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using RPG.Combat;
using RPG.Attributes;
using RPG.Core;
using RPG.UI.KnockoutCountdownText;

namespace RPG.Companions
{
    /// <summary>
    /// Контроллер поведения компаньона — следование и бой.
    /// Сам по себе ничего не делает до вызова Activate().
    /// Лежит на том же объекте, что и CompanionRecruiter.
    /// </summary>
    [RequireComponent(typeof(Fighter))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CompanionController : MonoBehaviour
    {
        [Header("Следование")]
        [SerializeField] private float followDistance    = 3f;
        [SerializeField] private float followStopDistance = 2f;
        [Tooltip("Множитель скорости следования относительно актуальной скорости игрока. " +
                 "Значение больше 1 позволяет компаньону нагонять игрока, если он немного отстал.")]
        [SerializeField] private float followCatchUpSpeedMultiplier = 1.15f;

        [Header("Бой")]
        [SerializeField] private float combatRange = 10f;

        [Header("Нокаут и возрождение")]
        [Tooltip("Время в секундах, через которое компаньон возрождается после нокаута.")]
        [SerializeField] private float knockoutRespawnDuration = 15f;
        [Tooltip("Процент максимального здоровья, восстанавливаемый компаньону при возрождении.")]
        [Range(1f, 100f)]
        [SerializeField] private float reviveHealthPercentage = 50f;
        [Tooltip("Спавнер всплывающей подсказки с обратным отсчётом до возрождения. " +
                 "Показывается над компаньоном каждую секунду, пока он в нокауте.")]
        [SerializeField] private KnockoutCountdownSpawner knockoutCountdownSpawner;

        // ── состояние ────────────────────────────────────────────────────
        private enum State { Inactive, Following, Combat, Knockout }
        private State _state = State.Inactive;

        private Fighter      _fighter;
        private Health       _health;
        private NavMeshAgent _agent;
        private Transform    _player;
        private NavMeshAgent _playerAgent;
        private Health       _target;
        private string       _companionID;
        private Coroutine    _knockoutRoutine;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _fighter = GetComponent<Fighter>();
            _health  = GetComponent<Health>();
            _agent   = GetComponent<NavMeshAgent>();

            // До найма NPC стоит на месте — агент выключен
            _agent.enabled = false;
        }

        private void Update()
        {
            if (_state == State.Inactive) return;
            if (_player == null)          return;

            if (_health.IsDead())
            {
                HandleDeath();
                return;
            }

            switch (_state)
            {
                case State.Following: TickFollow(); break;
                case State.Combat:    TickCombat(); break;
                // В нокауте компаньон ничего не делает — идёт таймер в KnockoutRoutine
            }
        }

        private void OnDestroy()
        {
            UnregisterSelf();
        }

        // ── публичный API ─────────────────────────────────────────────────

        /// <summary>Активирует режим компаньона. Вызывается из CompanionRecruiter.</summary>
        public void Activate(CompanionData data)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                Debug.LogError("[CompanionController] Activate: игрок не найден!");
                return;
            }

            _player      = playerObj.transform;
            _playerAgent = playerObj.GetComponent<NavMeshAgent>();
            _companionID = data.CompanionID;

            // Переопределяем параметры из SO если нужно
            followDistance = data.FollowDistance;
            combatRange    = data.CombatRange;

            // "Companion" — враги его видят и атакуют, но не путают с "Player"
            gameObject.tag = "Companion";

            _agent.enabled   = true;
            _agent.isStopped = false;

            _health.onDie.AddListener(HandleDeath);

            _state = State.Following;

            CompanionManager.Instance.RegisterActiveCompanion(_companionID, this);
        }

        /// <summary>Деактивирует режим компаньона. NPC возвращается к обычному состоянию.</summary>
        public void Deactivate()
        {
            _health.onDie.RemoveListener(HandleDeath);

            StopKnockoutRoutine();

            _fighter.Cancel();

            _agent.isStopped = true;
            _agent.enabled   = false;

            _target      = null;
            _player      = null;
            _playerAgent = null;

            // Возвращаем тег NPC-разговорника (поменяй под свой проект)
            gameObject.tag = "Untagged";

            _state = State.Inactive;

            UnregisterSelf();
        }

        // ── тик следования ────────────────────────────────────────────────

        private void TickFollow()
        {
            SyncFollowSpeedWithPlayer();

            Health enemy = GetBestEnemy();
            if (enemy != null)
            {
                _target = enemy;
                _state  = State.Combat;
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > followDistance)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }
            else
            {
                _agent.isStopped = true;
            }
        }

        /// <summary>
        /// Подстраивает скорость следования компаньона под актуальную скорость игрока
        /// (спринт, замедления, бонусы и т.д.), чтобы компаньон не отставал вне боя.
        /// </summary>
        private void SyncFollowSpeedWithPlayer()
        {
            if (_playerAgent == null) return;
            if (_playerAgent.speed <= 0f) return;

            _agent.speed = _playerAgent.speed * followCatchUpSpeedMultiplier;
        }

        // ── тик боя ──────────────────────────────────────────────────────

        private void TickCombat()
        {
            // Текущая цель умерла — ищем следующую
            if (_target == null || _target.IsDead())
            {
                _target = GetBestEnemy();
                if (_target == null)
                {
                    _fighter.Cancel();
                    _state = State.Following;
                    return;
                }
            }

            // Цель слишком далеко от игрока — прекращаем погоню
            float distTargetToPlayer = Vector3.Distance(_target.transform.position, _player.position);
            if (distTargetToPlayer > combatRange * 1.5f)
            {
                _fighter.Cancel();
                _target = null;
                _state  = State.Following;
                return;
            }

            _fighter.Attack(_target.gameObject);
        }

        // ── вспомогательное ──────────────────────────────────────────────

        private Health GetBestEnemy()
        {
            // Приоритет: то, что атакует сам игрок
            Fighter playerFighter = _player.GetComponent<Fighter>();
            if (playerFighter != null)
            {
                Health playerTarget = playerFighter.GetTarget();
                if (playerTarget != null && !playerTarget.IsDead())
                {
                    float d = Vector3.Distance(transform.position, playerTarget.transform.position);
                    if (d <= combatRange) return playerTarget;
                }
            }

            return FindNearestEnemy();
        }

        private Health FindNearestEnemy()
        {
            Health nearest     = null;
            float  nearestDist = Mathf.Infinity;

            foreach (Collider hit in Physics.OverlapSphere(transform.position, combatRange))
            {
                Health h = hit.GetComponent<Health>();
                if (h == null || h.IsDead())       continue;
                if (h.gameObject == gameObject)     continue;
                if (hit.CompareTag("Player"))       continue;
                if (hit.CompareTag("Companion"))    continue;

                bool isEnemy = hit.GetComponent<RPG.Control.AIController>() != null
                            || hit.GetComponent<CombatTarget>() != null;
                if (!isEnemy) continue;

                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < nearestDist) { nearest = h; nearestDist = d; }
            }

            return nearest;
        }

        // ── нокаут и возрождение ─────────────────────────────────────────

        /// <summary>
        /// Вызывается при получении смертельного урона. Вместо окончательной смерти
        /// компаньон переходит в нокаут и возрождается через knockoutRespawnDuration секунд.
        /// </summary>
        private void HandleDeath()
        {
            // onDie может сработать повторно, если по лежащему компаньону продолжают бить —
            // игнорируем повторные вызовы, пока идёт уже запущенный отсчёт нокаута.
            if (_state == State.Knockout) return;

            _fighter.Cancel();
            _state = State.Knockout;

            StopKnockoutRoutine();
            _knockoutRoutine = StartCoroutine(KnockoutRoutine());
        }

        /// <summary>
        /// Ежесекундно показывает над компаньоном подсказку с оставшимся временем
        /// до возрождения, затем восстанавливает часть здоровья и возвращает его в строй.
        /// </summary>
        private IEnumerator KnockoutRoutine()
        {
            float remainingSeconds = knockoutRespawnDuration;

            while (remainingSeconds > 0f)
            {
                ShowKnockoutCountdown(Mathf.CeilToInt(remainingSeconds));
                yield return new WaitForSeconds(1f);
                remainingSeconds -= 1f;
            }

            Revive();
        }

        private void ShowKnockoutCountdown(int secondsLeft)
        {
            if (knockoutCountdownSpawner == null) return;
            knockoutCountdownSpawner.Spawn($"До возрождения {secondsLeft} сек");
        }

        /// <summary>Возвращает компаньона в строй после нокаута с частью здоровья.</summary>
        private void Revive()
        {
            _health.Heal(_health.GetMaxHealthPoints() * reviveHealthPercentage / 100f);

            _knockoutRoutine = null;
            _target = null;
            _state  = State.Following;
        }

        private void StopKnockoutRoutine()
        {
            if (_knockoutRoutine == null) return;
            StopCoroutine(_knockoutRoutine);
            _knockoutRoutine = null;
        }

        private void UnregisterSelf()
        {
            if (!string.IsNullOrEmpty(_companionID))
            {
                CompanionManager.Instance.UnregisterActiveCompanion(_companionID);
                _companionID = null;
            }
        }

        // ── отладка ───────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, followDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, combatRange);
        }
    }
}
