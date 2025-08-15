using RPG.Attributes;
using RPG.Combat;
using RPG.Control;
using UnityEngine;

namespace RPG.Dialogue
{
    public class AIConversant : MonoBehaviour, IRaycastable
    {
        [SerializeField] private Dialogue _dialogue = null;
        [SerializeField] private string _conversantName;
        
        [Header("Trigger Dialogue Settings")]
        [SerializeField] private bool _isTrigger = false;
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private bool _showTriggerVisualization = true;
        [SerializeField] private Color _triggerColor = new Color(0, 1, 0, 0.3f);
        
        private bool _hasTriggered = false;

        public CursorType GetCursorType()
        {
            return CursorType.Dialogue;
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            if(_dialogue == null || _isTrigger)
            {
                return false;
            }

            if (TryGetComponent(out Health health))
            {
                if(health.IsDead()) return false;
            }

            if (Input.GetMouseButtonDown(0))
            {
                callingController.GetComponent<PlayerConversant>().StartDialogue(this, _dialogue);
            }
            return true;
        }

        public string GetName()
        {
            return _conversantName;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isTrigger || _dialogue == null)
                return;

            if (_triggerOnce && _hasTriggered)
                return;

            if (!other.CompareTag("Player"))
                return;

            if (TryGetComponent(out Health health))
            {
                if (health.IsDead()) return;
            }

            PlayerConversant playerConversant = other.GetComponent<PlayerConversant>();
            if (playerConversant != null)
            {
                if (!playerConversant.IsActive())
                {
                    playerConversant.StartDialogue(this, _dialogue);
                    _hasTriggered = true;
                }
            }
        }

        public void ResetTrigger()
        {
            _hasTriggered = false;
        }

        public bool IsTrigger()
        {
            return _isTrigger;
        }

        private void OnValidate()
        {
            if (_isTrigger)
            {
                Collider collider = GetComponent<Collider>();
                if (collider != null && !collider.isTrigger)
                {
                    collider.isTrigger = true;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!_isTrigger || !_showTriggerVisualization)
                return;

            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null)
                return;

            Gizmos.color = _triggerColor;
            
            if (triggerCollider is BoxCollider boxCollider)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (triggerCollider is SphereCollider sphereCollider)
            {
                Gizmos.DrawSphere(transform.position + sphereCollider.center, 
                    sphereCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z));
            }
            else if (triggerCollider is CapsuleCollider capsuleCollider)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                
                Vector3 size = new Vector3(
                    capsuleCollider.radius * 2,
                    capsuleCollider.height,
                    capsuleCollider.radius * 2
                );
                
                Gizmos.DrawCube(capsuleCollider.center, size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
