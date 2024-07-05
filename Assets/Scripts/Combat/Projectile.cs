using RPG.Attributes;
using UnityEngine;

namespace RPG.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed;
        [SerializeField] private bool _isHoming;
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private float _maxLifeTime = 10f;
        [SerializeField] private float _lifeAfterImpact = 2f;
        [SerializeField] private GameObject[] _destroyOnHit;

        private Health _target;
        private GameObject _instigator = null;
        private float _damage;

        private void Start()
        {
            transform.LookAt(GetAimLocation());
        }

        private void Update()
        {
            if (_target == null) return;

            if(_isHoming && !_target.IsDead()) transform.LookAt(GetAimLocation());
            transform.Translate(Vector3.forward * _speed * Time.deltaTime);
        }

        public void SetTarget(Health target, GameObject instigator, float damage)
        {
            _target = target;
            _damage = damage;
            _instigator = instigator;

            Destroy(gameObject, _maxLifeTime);
        }

        private Vector3 GetAimLocation()
        {
            CapsuleCollider targetCapsule = _target.GetComponent<CapsuleCollider>();
            if (targetCapsule == null) return _target.transform.position;
            return _target.transform.position + Vector3.up * targetCapsule.height / 2;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.GetComponent<Health>() != _target) return;
            if (_target.IsDead()) return;
            _target.TakeDamage(_instigator, _damage);

            _speed = 0;

            if(_hitEffect != null) Instantiate(_hitEffect, GetAimLocation(), transform.rotation);

            foreach(GameObject toDestroy in _destroyOnHit)
            {
                Destroy(toDestroy);
            }

            Destroy(gameObject, _lifeAfterImpact);
        }
    }
}
