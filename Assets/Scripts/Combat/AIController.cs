using RPG.Combat;
using RPG.Core;
using RPG.Movement;
using UnityEngine;

namespace RPG.Control
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private float _chaseDistance = 5f;
        [SerializeField] private float _suspitionTime = 3f;

        private ActionScheduler _actionScheduler;

        private Fighter _fighter;
        private GameObject _player;
        private Health _health;
        private Mover _mover;

        private Vector3 _guardLocation;
        private float _timeSinceLastSawPlayer = Mathf.Infinity;

        private void Start()
        {
            _actionScheduler = GetComponent<ActionScheduler>();

            _fighter = GetComponent<Fighter>();
            _player = GameObject.FindWithTag("Player");
            _health = GetComponent<Health>();
            _mover = GetComponent<Mover>();

            _guardLocation = transform.position;
        }

        private void Update()
        {
            if (_health.IsDead()) return;

            if (InAttackRangeOfPlayer() && _fighter.CanAttack(_player))
            {
                _timeSinceLastSawPlayer = 0;
                AttackBehaviour();
            }
            else if (_timeSinceLastSawPlayer < _suspitionTime)
            {
                SuspicionBehaviour();
            }
            else
            {
                GuardBehaviour();
            }

            _timeSinceLastSawPlayer += Time.deltaTime;
        }

        private void GuardBehaviour()
        {
            _mover.StartMoveAction(_guardLocation);
        }

        private void SuspicionBehaviour()
        {
            _actionScheduler.CancelCurrentAction();
        }

        private void AttackBehaviour()
        {
            _fighter.Attack(_player);
        }

        private bool InAttackRangeOfPlayer()
        {
            float distanceToPlayer = Vector3.Distance(_player.transform.position, transform.position);
            return distanceToPlayer < _chaseDistance;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _chaseDistance);
        }
    }
}
