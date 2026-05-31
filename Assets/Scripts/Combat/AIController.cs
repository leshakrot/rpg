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

        private bool _playerEscapedThisChase = false;
        private GameObject _currentTarget = null;
        private bool _returningToSpawn = false;

        // Новые переменные для индивидуальных настроек точек патрулирования
        private float _currentWaypointDwellTime;
        private float _currentWaypointSpeed;

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

        private State _lastLoggedState = (State)(-1);
        private bool _lastLoggedEscaped = false;
        private bool _lastLoggedReturning = false;

        private void Awake()
        {
            _actionScheduler = GetComponent<ActionScheduler>();
            _fighter = GetComponent<Fighter>();
            _player = GameObject.FindWithTag("Player");
            _health = GetComponent<Health>();
            _mover = GetComponent<Mover>();

            _guardPosition = new LazyValue<Vector3>(GetGuardPosition);
            _guardPosition.ForceInit();

            // Инициализация стандартными значениями
            _currentWaypointDwellTime = _waypointDwellTime;
            _currentWaypointSpeed = _patrolSpeedFraction;
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
            _currentTarget = null;
            _returningToSpawn = false;

            // Сброс динамических параметров патруля
            _currentWaypointDwellTime = _waypointDwellTime;
            _currentWaypointSpeed = _patrolSpeedFraction;

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

            if (_playerEscapedThisChase && _timeSinceAggrevated >= _agroCooldownTime)
            {
                float distToPlayer = Vector3.Distance(_player.transform.position, transform.position);
                if (distToPlayer < _chaseDistance)
                    _playerEscapedThisChase = false;
            }

            if (_playerEscapedThisChase && _currentState == State.ReturnToSpawn)
            {
                float distToPlayer = Vector3.Distance(_player.transform.position, transform.position);
                if (distToPlayer < _chaseDistance)
                    _playerEscapedThisChase = false;
            }

            // Расширенный лог для диагностики
            float distFromContact = _hasFirstContactPoint
                ? Vector3.Distance(_player.transform.position, _firstContactPoint)
                : -1f;
            float distFromEnemy = Vector3.Distance(_player.transform.position, transform.position);

            if (_currentState != _lastLoggedState || _playerEscapedThisChase != _lastLoggedEscaped || _returningToSpawn != _lastLoggedReturning)
            {
                Debug.Log($"[AI] State={_currentState} | aggr={isAggrevated} | escaped={_playerEscapedThisChase} | playerEscapedCalc={playerEscaped} | returning={_returningToSpawn} | timeSinceAggr={_timeSinceAggrevated:F1} | distEnemy={distFromEnemy:F1} | distFromContact={distFromContact:F1} | hasContact={_hasFirstContactPoint} | suspTimer={_suspicionTimer:F1}");
                _lastLoggedState = _currentState;
                _lastLoggedEscaped = _playerEscapedThisChase;
                _lastLoggedReturning = _returningToSpawn;
            }

            switch (_currentState)
            {
                case State.Chase:
                    if (tooFarFromSpawn || playerEscaped)
                    {
                        _playerEscapedThisChase = true;
                        EnterSuspicionState();
                    }
                    else if (isAggrevated)
                    {
                        _timeSinceLastSawPlayer = 0f;
                        AggrevateNearbyEnemies();

                        GameObject nearestHostile = GetNearestHostile();
                        if (nearestHostile != _currentTarget)
                        {
                            _currentTarget = nearestHostile;
                            _fighter.Attack(_currentTarget);
                        }
                    }
                    else
                    {
                        EnterSuspicionState();
                    }
                    break;

                case State.Suspicion:
                    _suspicionTimer += Time.deltaTime;
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
                    if (!_playerEscapedThisChase && isAggrevated && !tooFarFromSpawn)
                    {
                        _returningToSpawn = false;
                        EnterChaseState();
                        break;
                    }
                    if (!_returningToSpawn)
                    {
                        _returningToSpawn = true;
                        _mover.StartMoveAction(_guardPosition.value, _patrolSpeedFraction);
                    }
                    if (AtGuardPosition())
                    {
                        _returningToSpawn = false;
                        _hasFirstContactPoint = false;
                        _currentState = State.Patrol;
                    }
                    break;

                case State.Patrol:
                default:
                    PatrolBehaviour();
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
            _suspicionTimer = 0f;
            _returningToSpawn = false;
            Aggrevate();

            if (!_hasFirstContactPoint)
            {
                _firstContactPoint = _player.transform.position;
                _hasFirstContactPoint = true;
            }

            _currentTarget = null;
            GameObject nearestHostile = GetNearestHostile();
            _currentTarget = nearestHostile;
            _fighter.Attack(_currentTarget);
        }

        private void EnterSuspicionState()
        {
            _currentState = State.Suspicion;
            _suspicionTimer = 0f;
            _currentTarget = null;
            _fighter.Cancel();
            _actionScheduler.CancelCurrentAction();
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

                    // Считываем параметры конкретного вейпоинта (или берем дефолтные)
                    _currentWaypointDwellTime = _patrolPath.GetWaypointDwellTime(_currentWaypointIndex, _waypointDwellTime);
                    float? speedOverride = _patrolPath.GetWaypointSpeedOverride(_currentWaypointIndex);
                    _currentWaypointSpeed = speedOverride.HasValue ? speedOverride.Value : _patrolSpeedFraction;

                    CycleWaypoint();
                }
                nextPosition = GetCurrentWaypoint();
            }
            else
            {
                // На случай, если NPC просто стоит на месте без пути
                _currentWaypointDwellTime = _waypointDwellTime;
                _currentWaypointSpeed = _patrolSpeedFraction;
            }

            // Используем динамические параметры времени и скорости для движения
            if (_timeSinceArrivedAtWaypoint > _currentWaypointDwellTime)
            {
                _mover.StartMoveAction(nextPosition, _currentWaypointSpeed);
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
            if (_timeSinceAggrevated < _agroCooldownTime) return true;

            float distanceToPlayer = Vector3.Distance(_player.transform.position, transform.position);
            if (distanceToPlayer < _chaseDistance) return true;

            Collider[] hits = Physics.OverlapSphere(transform.position, _chaseDistance);
            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Companion")) continue;
                Health h = hit.GetComponent<Health>();
                if (h != null && !h.IsDead()) return true;
            }

            return false;
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

            Collider[] hits = Physics.OverlapSphere(transform.position, _chaseDistance);
            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Companion")) continue;
                Health h = hit.GetComponent<Health>();
                if (h != null && !h.IsDead()) return false;
            }

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

        private bool AtGuardPosition()
        {
            return Vector3.Distance(transform.position, _guardPosition.value) < 0.5f;
        }

        private GameObject GetNearestHostile()
        {
            float distToPlayer = Vector3.Distance(_player.transform.position, transform.position);

            GameObject nearestCompanion = null;
            float nearestCompanionDist = Mathf.Infinity;

            Collider[] hits = Physics.OverlapSphere(transform.position, _chaseDistance);
            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Companion")) continue;
                Health h = hit.GetComponent<Health>();
                if (h == null || h.IsDead()) continue;

                float d = Vector3.Distance(hit.transform.position, transform.position);
                if (d < nearestCompanionDist)
                {
                    nearestCompanionDist = d;
                    nearestCompanion = hit.gameObject;
                }
            }

            if (nearestCompanion != null && nearestCompanionDist < distToPlayer)
                return nearestCompanion;

            return _player;
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