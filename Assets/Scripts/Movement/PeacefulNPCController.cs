using GameDevTV.Utils;
using RPG.Movement;
using UnityEngine;

namespace RPG.Control
{
    /// <summary>
    /// Контроллер для мирных декоративных NPC.
    /// Только патрулирование по вейпоинтам, без боевой логики, без смерти.
    /// Требует: PeacefulMover, NavMeshAgent.
    /// </summary>
    public class PeacefulNPCController : MonoBehaviour
    {
        [SerializeField] private PatrolPath _patrolPath;
        [SerializeField] private float _waypointTolerance = 1f;
        [SerializeField] private float _waypointDwellTime = 3f;

        [Header("Speed")]
        [SerializeField] private bool _randomSpeed = false;

        [Tooltip("Используется когда Random Speed выключен")]
        [Range(0, 1)]
        [SerializeField] private float _moveSpeedFraction = 0.2f;

        [Tooltip("Минимальная скорость при Random Speed")]
        [Range(0, 1)]
        [SerializeField] private float _minSpeedFraction = 0.1f;

        [Tooltip("Максимальная скорость при Random Speed")]
        [Range(0, 1)]
        [SerializeField] private float _maxSpeedFraction = 0.5f;

        private PeacefulMover _mover;
        private LazyValue<Vector3> _originPosition;

        private int _currentWaypointIndex = 0;
        private float _timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private float _currentSpeedFraction;

        private void Awake()
        {
            _mover = GetComponent<PeacefulMover>();
            _originPosition = new LazyValue<Vector3>(() => transform.position);
            _originPosition.ForceInit();
            _currentSpeedFraction = GetSpeedFraction();
        }

        private void Update()
        {
            PatrolBehaviour();
        }

        private void PatrolBehaviour()
        {
            Vector3 nextPosition = _originPosition.value;

            if (_patrolPath != null)
            {
                if (AtWaypoint())
                {
                    _timeSinceArrivedAtWaypoint = 0f;
                    CycleWaypoint();
                    // Новая скорость роллится при смене вейпоинта
                    _currentSpeedFraction = GetSpeedFraction();
                }
                nextPosition = GetCurrentWaypoint();
            }

            _timeSinceArrivedAtWaypoint += Time.deltaTime;

            if (_timeSinceArrivedAtWaypoint > _waypointDwellTime)
            {
                _mover.StartMoveAction(nextPosition, _currentSpeedFraction);
            }
        }

        private float GetSpeedFraction()
        {
            if (_randomSpeed)
                return Random.Range(_minSpeedFraction, _maxSpeedFraction);

            return _moveSpeedFraction;
        }

        private bool AtWaypoint()
        {
            return Vector3.Distance(transform.position, GetCurrentWaypoint()) < _waypointTolerance;
        }

        private Vector3 GetCurrentWaypoint()
        {
            return _patrolPath.GetWaypoint(_currentWaypointIndex);
        }

        private void CycleWaypoint()
        {
            _currentWaypointIndex = _patrolPath.GetNextIndex(_currentWaypointIndex);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _waypointTolerance);
        }
    }
}
