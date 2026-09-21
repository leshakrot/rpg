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
            if (health != null)
            {
                health.onDie.AddListener(OnDeath);
            }
        }

        private void UnsubscribeFromHealth()
        {
            if (health != null)
            {
                health.onDie.RemoveListener(OnDeath);
            }
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
