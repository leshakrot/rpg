using RPG.Saving;
using UnityEngine;

namespace RPG.Core
{
    public class Health : MonoBehaviour, ISaveable
    {
        [SerializeField] private float _healthPoints = 100f;

        private Animator _animator;
        private ActionScheduler _actionScheduler;
        private bool _isDead = false;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _actionScheduler = GetComponent<ActionScheduler>();
        }

        public void TakeDamage(float damage)
        {
            _healthPoints = Mathf.Max(_healthPoints - damage, 0);
            if(_healthPoints == 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;

            _isDead = true;
            _animator.SetTrigger("die");
            _actionScheduler.CancelCurrentAction();
            
        }

        public bool IsDead()
        {
            return _isDead;
        }

        public object CaptureState()
        {
            return _healthPoints;
        }

        public void RestoreState(object state)
        {
            _healthPoints = (float)state;

            if (_healthPoints == 0)
            {
                Die();
            }
        }
    }
}
