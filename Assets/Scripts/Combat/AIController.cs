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

        [Range(0,1)]
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
        private bool _isReturningToSpawn = false;

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
            navMeshAgent.Warp(_guardPosition.value);
            _timeSinceLastSawPlayer = Mathf.Infinity;
            _timeSinceArrivedAtWaypoint = Mathf.Infinity;
            _timeSinceAggrevated = Mathf.Infinity;
            _currentWaypointIndex = 0;
            _currentState = State.Patrol;
            _isReturningToSpawn = false;
            _suspicionTimer = 0f;
            
            var fighter = GetComponent<Fighter>();
            if (fighter != null)
            {
                fighter.Cancel();
            }
            
            var mover = GetComponent<Mover>();
            if (mover != null)
            {
                mover.Cancel();
            }
        }

        public SpawnPoint GetSpawnPoint()
        {
            return _spawnPoint;
        }

        private Vector3 GetGuardPosition()
        {
            return transform.position;
        }

        private void Start()
        {
        }

        private void Update()
        {
            if (_health.IsDead()) return;

            UpdateTimers();

            bool canAttackPlayer = _fighter.CanAttack(_player);
            bool isAggrevated = IsAggrevated();
            bool tooFarFromSpawn = IsTooFarFromSpawn();

            switch (_currentState)
            {
                case State.Chase:
                    if (tooFarFromSpawn)
                    {
                        _currentState = State.Suspicion;
                        _suspicionTimer = 0f;
                    }
                    else if (isAggrevated && canAttackPlayer)
                    {
                        AttackBehaviour();
                        _timeSinceLastSawPlayer = 0f;
                    }
                    else
                    {
                        _currentState = State.Suspicion;
                        _suspicionTimer = 0f;
                    }
                    break;

                case State.Suspicion:
                    SuspicionBehaviour();
                    _suspicionTimer += Time.deltaTime;
                    if (_suspicionTimer > _suspicionDuration)
                    {
                        _currentState = State.ReturnToSpawn;
                    }
                    break;

                case State.ReturnToSpawn:
                    if (isAggrevated && canAttackPlayer && !tooFarFromSpawn)
                    {
                        _currentState = State.Chase;
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
                    if (isAggrevated && canAttackPlayer && !tooFarFromSpawn)
                    {
                        _currentState = State.Chase;
                    }
                    break;
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

            if(_patrolPath != null)
            {
                if (AtWaypoint())
                {
                    _timeSinceArrivedAtWaypoint = 0;
                    CycleWaypoint();
                }
                nextPosition = GetCurrentWaypoint();
            }

            if(_timeSinceArrivedAtWaypoint > _waypointDwellTime)
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

        private void AttackBehaviour()
        {
            _timeSinceLastSawPlayer = 0;
            _fighter.Attack(_player);

            AggrevateNearbyEnemies();
        }

        private void AggrevateNearbyEnemies()
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, _shoutDistance, Vector3.up, 0);

            foreach(RaycastHit hit in hits)
            {
                AIController ai = hit.collider.GetComponent<AIController>();
                if (ai == null) continue;

                ai.Aggrevate();
            }
        }

        private bool IsAggrevated()
        {
            float distanceToPlayer = Vector3.Distance(_player.transform.position, transform.position);
            return distanceToPlayer < _chaseDistance || _timeSinceAggrevated < _agroCooldownTime;
        }

        private bool IsTooFarFromSpawn()
        {
            if (_spawnPoint == null) return false;
            return Vector3.Distance(transform.position, _spawnPoint.transform.position) > _maxChaseDistanceFromSpawn;
        }

        public void SetSpawnPoint(SpawnPoint point)
        {
            _spawnPoint = point;
            _guardPosition = new LazyValue<Vector3>(() => _spawnPoint.transform.position);
            _guardPosition.ForceInit();

            // Динамически подхватываем PatrolPath из SpawnPoint, если задан
            GameObject patrolPathObj = point.GetPatrolPathObject();
            if (patrolPathObj != null)
            {
                PatrolPath path = patrolPathObj.GetComponent<PatrolPath>();
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
        }
    }
}
