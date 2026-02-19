using System.Collections;
using UnityEngine;
using RPG.Movement;
using RPG.Core;

namespace RPG.Control
{
    public abstract class InteractableObject : MonoBehaviour, IRaycastable, IAction
    {
        [Header("Настройки взаимодействия")]
        [SerializeField] protected float interactionDistance = 2.5f;
        
        protected Coroutine activeInteractionCoroutine = null;
        protected PlayerController currentController = null;
        private bool hasInteracted = false;

        protected virtual void OnEnable()
        {
            InteractableRegistry.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (InteractableRegistry.Instance != null)
            {
                InteractableRegistry.Instance.Unregister(this);
            }
        }

        public abstract CursorType GetCursorType();

        public virtual bool HandleRaycast(PlayerController callingController)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Mover mover = callingController.GetComponent<Mover>();
                if (!mover.CanMoveTo(transform.position))
                {
                    return false;
                }

                if (activeInteractionCoroutine != null)
                {
                    StopCoroutine(activeInteractionCoroutine);
                    activeInteractionCoroutine = null;
                }
                
                hasInteracted = false;
                currentController = callingController;
                ActionScheduler actionScheduler = callingController.GetComponent<ActionScheduler>();
                actionScheduler.StartAction(this);
                activeInteractionCoroutine = StartCoroutine(MoveToObjectAndInteract(callingController));
            }
            return true;
        }

        private IEnumerator MoveToObjectAndInteract(PlayerController callingController)
        {
            Mover mover = callingController.GetComponent<Mover>();
            
            while (Vector3.Distance(transform.position, callingController.transform.position) > interactionDistance)
            {
                mover.MoveTo(transform.position, 1f);
                yield return null;
            }

            if (!hasInteracted)
            {
                hasInteracted = true;
                mover.Cancel();
                OnInteract(callingController);
            }
            
            activeInteractionCoroutine = null;
        }

        public void Cancel()
        {
            if (activeInteractionCoroutine != null)
            {
                StopCoroutine(activeInteractionCoroutine);
                activeInteractionCoroutine = null;
            }
            
            if (currentController != null)
            {
                Mover mover = currentController.GetComponent<Mover>();
                if (mover != null)
                {
                    mover.Cancel();
                }
                currentController = null;
            }
            
            hasInteracted = false;
        }

        protected abstract void OnInteract(PlayerController callingController);

        public bool IsPlayerInRange(PlayerController player)
        {
            return Vector3.Distance(transform.position, player.transform.position) <= interactionDistance;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
