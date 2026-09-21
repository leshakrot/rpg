using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RPG.Attributes;
using RPG.Core;
using RPG.Movement;
using RPG.Combat;
using RPG.Control;

namespace RPG.Combat
{
    public class RagdollController : MonoBehaviour
    {
        [Header("Настройки ragdoll")]
        [Tooltip("Задержка перед включением ragdoll после смерти. Обычно 0 достаточно.")]
        [SerializeField] private float ragdollStartDelay = 0f;

        [Tooltip("Минимальное время, которое ragdoll должен физически работать.")]
        [SerializeField] private float minimumRagdollTime = 1.5f;

        [Tooltip("Скорость Rigidbody, ниже которой тело считается почти неподвижным.")]
        [SerializeField] private float settleVelocityThreshold = 0.15f;

        [Tooltip("Угловая скорость Rigidbody, ниже которой тело считается почти неподвижным.")]
        [SerializeField] private float settleAngularVelocityThreshold = 0.15f;

        [Tooltip("Сколько секунд все части должны оставаться почти неподвижными перед отключением физики.")]
        [SerializeField] private float settleTime = 0.5f;

        [Tooltip("Максимальное время активного ragdoll. Защита от бесконечной физики.")]
        [SerializeField] private float maximumRagdollTime = 4f;

        [Tooltip("После остановки тела отключить его ragdoll-коллайдеры. Существенно дешевле для слабых устройств.")]
        [SerializeField] private bool disableCollidersWhenSettled = true;

        [Header("Импульс при смерти")]
        [Tooltip("Добавлять небольшой случайный физический импульс при смерти.")]
        [SerializeField] private bool useDeathImpulse = true;

        [Tooltip("Минимальная сила импульса.")]
        [SerializeField] private float minDeathImpulse = 1.5f;

        [Tooltip("Максимальная сила импульса.")]
        [SerializeField] private float maxDeathImpulse = 3.5f;

        [Tooltip("Минимальная случайная составляющая вверх.")]
        [SerializeField] private float minUpwardImpulse = 0.15f;

        [Tooltip("Максимальная случайная составляющая вверх.")]
        [SerializeField] private float maxUpwardImpulse = 0.75f;

        [Tooltip("Максимальный случайный разброс направления в градусах.")]
        [Range(0f, 90f)]
        [SerializeField] private float impulseSpreadAngle = 25f;

        [Tooltip("Сколько частей ragdoll получают импульс. Обычно 1 достаточно.")]
        [Range(1, 3)]
        [SerializeField] private int impulseBodyPartCount = 1;

        [Tooltip("Случайное вращение тела после смерти. Создаёт больше вариантов падения.")]
        [SerializeField] private bool useDeathTorque = true;

        [Tooltip("Минимальная сила случайного вращения.")]
        [SerializeField] private float minDeathTorque = 0.5f;

        [Tooltip("Максимальная сила случайного вращения.")]
        [SerializeField] private float maxDeathTorque = 2f;

        [Header("Дополнительно")]
        [Tooltip("Отключать ли Animator при переходе в ragdoll.")]
        [SerializeField] private bool disableAnimator = true;

        [Tooltip("Отключать ли NavMeshAgent.")]
        [SerializeField] private bool disableNavMeshAgent = true;

        [Tooltip("Отключать ли Mover.")]
        [SerializeField] private bool disableMover = true;

        [Tooltip("Отключать ли Fighter.")]
        [SerializeField] private bool disableFighter = true;

        [Tooltip("Отключать ли AIController.")]
        [SerializeField] private bool disableAIController = true;

        [Tooltip("Отключать ли основной Collider на корне.")]
        [SerializeField] private bool disableRootCollider = true;

        private Health health;
        private Animator animator;
        private NavMeshAgent navMeshAgent;
        private Mover mover;
        private Fighter fighter;
        private AIController aiController;
        private ActionScheduler actionScheduler;
        private Collider rootCollider;

        private Rigidbody[] ragdollRigidbodies;
        private Collider[] ragdollColliders;

        private GameObject lastDamageInstigator;

        private bool isRagdollActive;
        private bool isInitialized;
        private Coroutine ragdollCoroutine;

        private void Awake()
        {
            CacheComponents();
            CacheRagdollParts();
            DisableRagdoll();
            SubscribeToHealth();
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealth();

            if (ragdollCoroutine != null)
            {
                StopCoroutine(ragdollCoroutine);
                ragdollCoroutine = null;
            }
        }

        private void CacheComponents()
        {
            health = GetComponent<Health>();
            animator = GetComponent<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            mover = GetComponent<Mover>();
            fighter = GetComponent<Fighter>();
            aiController = GetComponent<AIController>();
            actionScheduler = GetComponent<ActionScheduler>();
            rootCollider = GetComponent<Collider>();
        }

        private void CacheRagdollParts()
        {
            Rigidbody[] allRigidbodies = GetComponentsInChildren<Rigidbody>(true);

            List<Rigidbody> validRigidbodies = new List<Rigidbody>();
            List<Collider> validColliders = new List<Collider>();

            foreach (Rigidbody rigidbody in allRigidbodies)
            {
                if (rigidbody == null)
                {
                    continue;
                }

                if (rigidbody.gameObject == gameObject)
                {
                    continue;
                }

                validRigidbodies.Add(rigidbody);

                Collider[] colliders = rigidbody.GetComponents<Collider>();

                foreach (Collider collider in colliders)
                {
                    if (collider != null)
                    {
                        validColliders.Add(collider);
                    }
                }
            }

            ragdollRigidbodies = validRigidbodies.ToArray();
            ragdollColliders = validColliders.ToArray();
            isInitialized = ragdollRigidbodies.Length > 0;

            if (!isInitialized)
            {
                Debug.LogWarning(
                    $"[RagdollController] На объекте '{gameObject.name}' не найдено Rigidbody для ragdoll."
                );
            }
        }

        private void SubscribeToHealth()
        {
            if (health == null)
            {
                return;
            }

            health.onDie.AddListener(OnDeath);
            health.onTakeDamage += OnTakeDamage;
        }

        private void UnsubscribeFromHealth()
        {
            if (health == null)
            {
                return;
            }

            health.onDie.RemoveListener(OnDeath);
            health.onTakeDamage -= OnTakeDamage;
        }

        private void OnTakeDamage(GameObject instigator)
        {
            lastDamageInstigator = instigator;
        }

        private void OnDeath()
        {
            if (isRagdollActive || !isInitialized)
            {
                return;
            }

            if (ragdollCoroutine != null)
            {
                StopCoroutine(ragdollCoroutine);
            }

            ragdollCoroutine = StartCoroutine(ActivateRagdollAfterDeath());
        }

        private IEnumerator ActivateRagdollAfterDeath()
        {
            // Health.onDie вызывается до завершения UpdateState(), поэтому ждём кадр.
            yield return null;

            if (ragdollStartDelay > 0f)
            {
                yield return new WaitForSeconds(ragdollStartDelay);
            }

            if (this == null || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            EnableRagdoll();

            if (useDeathImpulse)
            {
                ApplyDeathImpulse();
            }

            float elapsed = 0f;
            float settledElapsed = 0f;

            while (elapsed < maximumRagdollTime)
            {
                elapsed += Time.deltaTime;

                if (elapsed >= minimumRagdollTime)
                {
                    if (IsRagdollSettled())
                    {
                        settledElapsed += Time.deltaTime;

                        if (settledElapsed >= settleTime)
                        {
                            DisableRagdollPhysics();
                            yield break;
                        }
                    }
                    else
                    {
                        settledElapsed = 0f;
                    }
                }

                yield return null;
            }

            DisableRagdollPhysics();
        }

        private void EnableRagdoll()
        {
            if (isRagdollActive)
            {
                return;
            }

            isRagdollActive = true;

            if (actionScheduler != null)
            {
                actionScheduler.CancelCurrentAction();
            }

            if (navMeshAgent != null && disableNavMeshAgent)
            {
                if (navMeshAgent.enabled)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.enabled = false;
                }
            }

            if (mover != null && disableMover)
            {
                mover.enabled = false;
            }

            if (fighter != null && disableFighter)
            {
                fighter.enabled = false;
            }

            if (aiController != null && disableAIController)
            {
                aiController.enabled = false;
            }

            if (rootCollider != null && disableRootCollider)
            {
                rootCollider.enabled = false;
            }

            if (animator != null && disableAnimator)
            {
                animator.enabled = false;
            }

            Physics.SyncTransforms();

            foreach (Collider collider in ragdollColliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            foreach (Rigidbody rigidbody in ragdollRigidbodies)
            {
                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.isKinematic = false;
                rigidbody.useGravity = true;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            Physics.SyncTransforms();
        }

        private void ApplyDeathImpulse()
        {
            if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
            {
                return;
            }

            int partsToAffect = Mathf.Clamp(
                impulseBodyPartCount,
                1,
                ragdollRigidbodies.Length
            );

            List<Rigidbody> availableParts = new List<Rigidbody>(ragdollRigidbodies);

            for (int i = 0; i < partsToAffect; i++)
            {
                if (availableParts.Count == 0)
                {
                    break;
                }

                int randomIndex = Random.Range(0, availableParts.Count);
                Rigidbody rigidbody = availableParts[randomIndex];
                availableParts.RemoveAt(randomIndex);

                if (rigidbody == null)
                {
                    continue;
                }

                Vector3 direction = GetDeathImpulseDirection();

                float impulse = Random.Range(
                    minDeathImpulse,
                    Mathf.Max(minDeathImpulse, maxDeathImpulse)
                );

                float upward = Random.Range(
                    minUpwardImpulse,
                    Mathf.Max(minUpwardImpulse, maxUpwardImpulse)
                );

                direction += Vector3.up * upward;
                direction.Normalize();

                rigidbody.AddForce(
                    direction * impulse,
                    ForceMode.Impulse
                );

                if (useDeathTorque)
                {
                    Vector3 randomTorque = Random.onUnitSphere;

                    float torque = Random.Range(
                        minDeathTorque,
                        Mathf.Max(minDeathTorque, maxDeathTorque)
                    );

                    rigidbody.AddTorque(
                        randomTorque * torque,
                        ForceMode.Impulse
                    );
                }
            }
        }

        private Vector3 GetDeathImpulseDirection()
        {
            Vector3 direction;

            if (lastDamageInstigator != null)
            {
                direction = transform.position - lastDamageInstigator.transform.position;

                direction.y = 0f;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = -transform.forward;
                }
            }
            else
            {
                direction = Random.insideUnitSphere;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector3.forward;
                }
            }

            direction.Normalize();

            // Небольшой случайный поворот направления.
            float randomAngle = Random.Range(
                -impulseSpreadAngle,
                impulseSpreadAngle
            );

            direction = Quaternion.Euler(0f, randomAngle, 0f) * direction;

            return direction;
        }

        private void DisableRagdoll()
        {
            if (ragdollRigidbodies == null)
            {
                return;
            }

            foreach (Rigidbody rigidbody in ragdollRigidbodies)
            {
                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }

            if (ragdollColliders != null)
            {
                foreach (Collider collider in ragdollColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
            }
        }

        private bool IsRagdollSettled()
        {
            if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
            {
                return true;
            }

            foreach (Rigidbody rigidbody in ragdollRigidbodies)
            {
                if (rigidbody == null)
                {
                    continue;
                }

                if (rigidbody.IsSleeping())
                {
                    continue;
                }

                if (rigidbody.linearVelocity.sqrMagnitude >
                    settleVelocityThreshold * settleVelocityThreshold)
                {
                    return false;
                }

                if (rigidbody.angularVelocity.sqrMagnitude >
                    settleAngularVelocityThreshold * settleAngularVelocityThreshold)
                {
                    return false;
                }
            }

            return true;
        }

        private void DisableRagdollPhysics()
        {
            if (!isRagdollActive)
            {
                return;
            }

            foreach (Rigidbody rigidbody in ragdollRigidbodies)
            {
                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }

            if (disableCollidersWhenSettled)
            {
                foreach (Collider collider in ragdollColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                }
            }
        }

        public bool IsRagdollActive()
        {
            return isRagdollActive;
        }

        public bool HasRagdollParts()
        {
            return isInitialized;
        }

        public int GetRagdollRigidbodyCount()
        {
            return ragdollRigidbodies != null ? ragdollRigidbodies.Length : 0;
        }

        public int GetRagdollColliderCount()
        {
            return ragdollColliders != null ? ragdollColliders.Length : 0;
        }
    }
}
