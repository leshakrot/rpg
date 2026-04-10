using GameDevTV.Utils;
using RPG.Attributes;
using RPG.Combat;
using RPG.Core;
using RPG.Movement;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace RPG.Control
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private float _chaseDistance = 5f;
        [SerializeField] private float _suspitionTime = 3f;
        [SerializeField] private float _agroCooldownTime = 5f;

        [SerializeField] private PatrolPath _patrolPath;
        [SerializeField] private float _waypointTolerance = 1f;
        [SerializeField] private float _waypointDwellTime = 3f;

        [Range(0, 1)]
        [SerializeField] private float _patrolSpeedFraction = 0.2f;
        [SerializeField] private float _shoutDistance = 5f;

        private ActionScheduler _actionScheduler;
        private Fighter _fighter;
        private GameObject _player;
        private Health _health;
        private Mover _mover;
        
        private Health _currentTarget; // Текущая цель (игрок или компаньон)

        private LazyValue<Vector3> _guardPosition;
        private float _timeSinceLastSawPlayer = Mathf.Infinity;
        private float _timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private float _timeSinceAggrevated = Mathf.Infinity;
        private int _currentWaypointIndex = 0;

        private SpawnPoint _spawnPoint;
        [SerializeField] private float _maxChaseDistanceFromSpawn = 15f;

        [Header("Escape")]
        [Tooltip("Distance from first contact point at which the enemy gives up chasing")]
        [SerializeField] private float _escapeDistanceFromContact = 20f;
        [Tooltip("Distance from the enemy itself at which the enemy gives up chasing")]
        [SerializeField] private float _escapeDistanceFromEnemy = 15f;

        private Vector3 _spawnOrigin;
        private bool _hasSpawnOrigin = false;

        private Vector3 _firstContactPoint;
        private bool _hasFirstContactPoint = false;

        // Флаг: игрок убежал в этом цикле преследования.
        // Пока true — враг не возобновляет Chase из Suspicion/ReturnToSpawn,
        // даже если agro cooldown ещё не истёк.
        // Сбрасывается только когда игрок снова входит в _chaseDistance с нуля
        // (т.е. _timeSinceAggrevated >= _agroCooldownTime).
        private bool _playerEscapedThisChase = false;

        private enum State
        {
            Patrol,
            Chase,
            Suspicion,
            ReturnToSpawn
        }

        private State _currentState = State.Patrol;
        private float _suspicionTimer = 0f;
        private float _suspicionDuration = 2f;

        private void Awake()
        {
            _actionScheduler = GetComponent<ActionScheduler>();
            _fighter = GetComponent<Fighter>();
            _player = GameObject.FindWithTag("Player");
            _health = GetComponent<Health>();
            _mover = GetComponent<Mover>();

            _guardPosition = new LazyValue<Vector3>(GetGuardPosition);
            _guardPosition.ForceInit();
        }

        public void Reset()
        {
            NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.Warp(transform.position);

            _timeSinceLastSawPlayer = Mathf.Infinity;
            _timeSinceArrivedAtWaypoint = Mathf.Infinity;
            _timeSinceAggrevated = Mathf.Infinity;
            _currentWaypointIndex = 0;
            _currentState = State.Patrol;
            _suspicionTimer = 0f;
            _hasFirstContactPoint = false;
            _playerEscapedThisChase = false;

            _fighter.Cancel();
            _mover.Cancel();
        }

        public SpawnPoint GetSpawnPoint()
        {
            return _spawnPoint;
        }

        private Vector3 GetGuardPosition()
        {
            return transform.position;
        }

        private void Start() { }

        private void Update()
        {
            if (_health.IsDead()) return;

            UpdateTimers();

            bool isAggrevated = IsAggrevated();
            bool tooFarFromSpawn = IsTooFarFromSpawn();
            bool playerEscaped = HasPlayerEscaped();

            // Игрок вернулся близко с нуля (agro полностью остыло и снова вошёл в радиус) —
            // сбрасываем флаг побега чтобы враг мог снова начать преследование
            if (_playerEscapedThisChase && _timeSinceAggrevated >= _agroCooldownTime)
            {
                float distToPlayer = Vector3.Distance(_player.transform.position, transform.position);
                if (distToPlayer < _chaseDistance)
                {
                    _playerEscapedThisChase = false;
                }
            }

            switch (_currentState)
            {
                case State.Chase:
                    if (tooFarFromSpawn || playerEscaped)
                    {
                        _playerEscapedThisChase = true;
                        _currentState = State.Suspicion;
                        _suspicionTimer = 0f;
                        _hasFirstContactPoint = false;
                        _fighter.Cancel();
                    }
                    else if (isAggrevated)
                    {
                        _timeSinceLastSawPlayer = 0f;
                        AggrevateNearbyEnemies();
                    }
                    else
                    {
                        _currentState = State.Suspicion;
                        _suspicionTimer = 0f;
                        _hasFirstContactPoint = false;
                        _fighter.Cancel();
                    }
                    break;

                case State.Suspicion:
                    SuspicionBehaviour();
                    _suspicionTimer += Time.deltaTime;
                    // Возобновляем Chase только если игрок НЕ убегал в этом цикле
                    if (!_playerEscapedThisChase && isAggrevated && !tooFarFromSpawn)
                    {
                        EnterChaseState();
                    }
                    else if (_suspicionTimer > _suspicionDuration)
                    {
                        _currentState = State.ReturnToSpawn;
                    }
                    break;

                case State.ReturnToSpawn:
                    // Возобновляем Chase только если игрок НЕ убегал в этом цикле
                    if (!_playerEscapedThisChase && isAggrevated && !tooFarFromSpawn)
                    {
                        EnterChaseState();
                        break;
                    }
                    ReturnToSpawnBehaviour();
                    if (AtGuardPosition())
                    {
                        _currentState = State.Patrol;
                    }
                    break;

                case State.Patrol:
                default:
                    PatrolBehaviour();
                    // Из Patrol атакуем всегда (флаг _playerEscapedThisChase уже сброшен к этому моменту)
                    if (!_playerEscapedThisChase && isAggrevated && !tooFarFromSpawn)
                    {
                        EnterChaseState();
                    }
                    break;
            }
        }

        private void EnterChaseState()
        {
            _currentState = State.Chase;
            _timeSinceLastSawPlayer = 0f;
            Aggrevate();

            if (!_hasFirstContactPoint)
            {
                _firstContactPoint = transform.position;
                _hasFirstContactPoint = true;
            }

            // Находим ближайшую цель (игрок или компаньон)
            _currentTarget = FindNearestTarget();
            if (_currentTarget != null)
            {
                _fighter.Attack(_currentTarget.gameObject);
            }
        }

        public void Aggrevate()
        {
            _timeSinceAggrevated = 0;
        }

        private void UpdateTimers()
        {
            _timeSinceLastSawPlayer += Time.deltaTime;
            _timeSinceArrivedAtWaypoint += Time.deltaTime;
            _timeSinceAggrevated += Time.deltaTime;
        }

        private void PatrolBehaviour()
        {
            Vector3 nextPosition = _guardPosition.value;

            if (_patrolPath != null)
            {
                if (AtWaypoint())
                {
                    _timeSinceArrivedAtWaypoint = 0;
                    CycleWaypoint();
                }
                nextPosition = GetCurrentWaypoint();
            }

            if (_timeSinceArrivedAtWaypoint > _waypointDwellTime)
            {
                _mover.StartMoveAction(nextPosition, _patrolSpeedFraction);
            }
        }

        private Vector3 GetCurrentWaypoint()
        {
            return _patrolPath.GetWaypoint(_currentWaypointIndex);
        }

        private void CycleWaypoint()
        {
            _currentWaypointIndex = _patrolPath.GetNextIndex(_currentWaypointIndex);
        }

        private bool AtWaypoint()
        {
            float distanceToWaypoint = Vector3.Distance(transform.position, GetCurrentWaypoint());
            return distanceToWaypoint < _waypointTolerance;
        }

        private void SuspicionBehaviour()
        {
            _actionScheduler.CancelCurrentAction();
        }

        private void AggrevateNearbyEnemies()
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, _shoutDistance, Vector3.up, 0);
            foreach (RaycastHit hit in hits)
            {
                AIController ai = hit.collider.GetComponent<AIController>();
                if (ai == null) continue;
                ai.Aggrevate();
            }
        }

        private bool IsAggrevated()
        {
            // Проверяем дистанцию до ближайшей цели
            Health nearestTarget = FindNearestTarget();
            if (nearestTarget == null) return _timeSinceAggrevated < _agroCooldownTime;
            
            float distanceToTarget = Vector3.Distance(nearestTarget.transform.position, transform.position);
            return distanceToTarget < _chaseDistance || _timeSinceAggrevated < _agroCooldownTime;
        }
        
        private Health FindNearestTarget()
        {
            Health nearest = null;
            float nearestDistance = Mathf.Infinity;
            
            // Проверяем игрока
            if (_player != null)
            {
                Health playerHealth = _player.GetComponent<Health>();
                if (playerHealth != null && !playerHealth.IsDead())
                {
                    float dist = Vector3.Distance(transform.position, _player.transform.position);
                    if (dist < nearestDistance)
                    {
                        nearest = playerHealth;
                        nearestDistance = dist;
                    }
                }
            }
            
            // Проверяем компаньонов
            var companions = FindObjectsOfType<RPG.Companions.CompanionController>();
            foreach (var companion in companions)
            {
                if (companion == null) continue;
                
                Health companionHealth = companion.GetComponent<Health>();
                if (companionHealth == null || companionHealth.IsDead()) continue;
                
                float dist = Vector3.Distance(transform.position, companion.transform.position);
                if (dist < nearestDistance)
                {
                    nearest = companionHealth;
                    nearestDistance = dist;
                }
            }
            
            return nearest;
        }

        private bool IsTooFarFromSpawn()
        {
            if (!_hasSpawnOrigin) return false;
            if (_patrolPath != null) return false;
            return Vector3.Distance(transform.position, _spawnOrigin) > _maxChaseDistanceFromSpawn;
        }

        private bool HasPlayerEscaped()
        {
            if (!_hasFirstContactPoint) return false;

            float distFromContact = Vector3.Distance(_player.transform.position, _firstContactPoint);
            if (distFromContact > _escapeDistanceFromContact) return true;

            float distFromEnemy = Vector3.Distance(_player.transform.position, transform.position);
            if (distFromEnemy > _escapeDistanceFromEnemy) return true;

            return false;
        }

        public void SetSpawnPoint(SpawnPoint point)
        {
            _spawnPoint = point;
            _spawnOrigin = point.transform.position;
            _hasSpawnOrigin = true;

            Vector3 guardPos = transform.position;
            _guardPosition = new LazyValue<Vector3>(() => guardPos);
            _guardPosition.ForceInit();

            if (point.GetPatrolPathObject() != null)
            {
                PatrolPath path = point.GetPatrolPathObject().GetComponent<PatrolPath>();
                if (path != null)
                {
                    _patrolPath = path;
                    _currentWaypointIndex = 0;
                }
            }
        }

        private void ReturnToSpawnBehaviour()
        {
            _mover.StartMoveAction(_guardPosition.value, _patrolSpeedFraction);
        }

        private bool AtGuardPosition()
        {
            return Vector3.Distance(transform.position, _guardPosition.value) < 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _chaseDistance);

            if (Application.isPlaying && _hasSpawnOrigin && _patrolPath == null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_spawnOrigin, _maxChaseDistanceFromSpawn);
            }

            if (Application.isPlaying && _hasFirstContactPoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_firstContactPoint, _escapeDistanceFromContact);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, _escapeDistanceFromEnemy);
            }
        }
    }
}
