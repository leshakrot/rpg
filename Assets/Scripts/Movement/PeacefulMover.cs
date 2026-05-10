using UnityEngine;
using UnityEngine.AI;

namespace RPG.Movement
{
    /// <summary>
    /// Упрощённый Mover для мирных декоративных NPC.
    /// Без Health, ActionScheduler, BaseStats, сохранений.
    /// Скорость задаётся напрямую через speedFraction от фиксированного максимума.
    /// </summary>
    public class PeacefulMover : MonoBehaviour
    {
        [SerializeField] private float _maxSpeed = 3.5f;

        private NavMeshAgent _navMeshAgent;
        private Animator _animator;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            UpdateAnimator();
        }

        public void StartMoveAction(Vector3 destination, float speedFraction)
        {
            MoveTo(destination, speedFraction);
        }

        public void MoveTo(Vector3 destination, float speedFraction)
        {
            if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.destination = destination;
                _navMeshAgent.speed = _maxSpeed * Mathf.Clamp01(speedFraction);
                _navMeshAgent.isStopped = false;
            }
        }

        public void Cancel()
        {
            if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = true;
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;
            Vector3 localVelocity = transform.InverseTransformDirection(_navMeshAgent.velocity);
            _animator.SetFloat("forwardSpeed", localVelocity.z);
        }
    }
}
