using UnityEngine;
using RPG.Movement;
using RPG.Combat;
using RPG.Attributes;
using System;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using RPG.Core;
using GameDevTV.Inventories;

namespace RPG.Control
{
    public class PlayerController : MonoBehaviour
    {
        private Mover _mover;
	    private PlayerFighter _fighter;
        private Health _health;
        private ActionStore _actionStore;

        [System.Serializable]
        private struct CursorMapping
        {
            public CursorType type;
            public Texture2D texture;
            public Vector2 hotspot;
        }

        [SerializeField] private CursorMapping[] _cursorMappings = null;
        [SerializeField] private float _maxNavmeshProjectionDistance = 1f;
        [SerializeField] private float _raycastRadius = 1f;
	    [SerializeField] private int _numberOfAbilities = 6;
        
	    [Header("WASD Movement")]
	    [Tooltip("Доля от максимальной скорости Mover при движении через WASD.")]
	    [SerializeField] float wasdMoveSpeedFraction = 1f; // 1f = полная скорость
	    [Tooltip("Насколько далеко впереди устанавливать цель для NavMeshAgent при движении WASD.")]
	    [SerializeField] float lookAheadDistance = 1.0f;
	    [Tooltip("Скорость поворота персонажа в сторону движения WASD.")]
	    [SerializeField] float rotationSpeed = 10f;

	    public bool _isDraggingUI = false;
        
	    private Transform cameraTransform;

        private void Awake()
        {
            _mover = GetComponent<Mover>();
	        _fighter = GetComponent<PlayerFighter>();
            _health = GetComponent<Health>();
	        _actionStore = GetComponent<ActionStore>();
            
	        if (Camera.main != null)
	        {
		        cameraTransform = Camera.main.transform;
	        }
	        else
	        {
		        Debug.LogError("PlayerController: Не найдена основная камера (Main Camera). Убедитесь, что у камеры есть тег 'MainCamera'.");
		        // Можно выключить компонент, если камера критична для управления
		        // enabled = false;
	        }
        }

        private void Update()
        {
            if (InteractWithUI()) return;
            if (_health.IsDead())
            {
                SetCursor(CursorType.None);
                return;
            }
            
	        bool didMoveWithWASD = HandleWASDMovement();

            UseAbilities();

            if (InteractWithComponent()) return;
            if (InteractWithMovement()) return;

            SetCursor(CursorType.None);
        }
        
	    private bool HandleWASDMovement()
	    {
		    // Проверяем, есть ли ссылка на камеру
		    if (cameraTransform == null) return false;

		    float horizontalInput = Input.GetAxis("Horizontal");
		    float verticalInput = Input.GetAxis("Vertical");

		    Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput);

		    // Выходим, если нет значимого ввода
		    if (inputDirection.magnitude < 0.1f)
		    {
			    return false; // Движения WASD не было
		    }

		    // --- Расчет направления относительно камеры ---
		    Vector3 cameraForward = cameraTransform.forward;
		    cameraForward.y = 0f;
		    cameraForward.Normalize();

		    Vector3 cameraRight = cameraTransform.right;
		    cameraRight.y = 0f;
		    cameraRight.Normalize();

		    Vector3 moveDirection = (cameraForward * verticalInput + cameraRight * horizontalInput).normalized;

		    // --- Расчет целевой точки для NavMeshAgent ---
		    Vector3 targetPosition = transform.position + moveDirection * lookAheadDistance;

		    // --- Передача команды Mover ---
		    _mover.StartMoveAction(targetPosition, wasdMoveSpeedFraction);

		    // --- Поворот персонажа ---
		    HandleRotation(moveDirection);

		    return true; // Движение WASD было совершено
	    }
	    
	    private void HandleRotation(Vector3 lookDirection)
	    {
		    if (lookDirection.sqrMagnitude > 0.01f)
		    {
			    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
			    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		    }
	    }

        private bool InteractWithUI()
        {
            if (Input.GetMouseButtonUp(0))
            {
                _isDraggingUI = false;
            }
            if (EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isDraggingUI = true;
                }
                SetCursor(CursorType.UI);
                return true;
            }
            if (_isDraggingUI)
            {
                return true;
            }
            return false;
        }

        private void UseAbilities()
        {
            for (int i = 0; i < _numberOfAbilities; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    _actionStore.Use(i, gameObject);
                }
            }         
        }

        private bool InteractWithComponent()
        {
            RaycastHit[] hits = RaycastAllSorted();
            foreach (RaycastHit hit in hits)
            {
                IRaycastable[] raycastables = hit.transform.GetComponents<IRaycastable>();
                foreach (IRaycastable raycastable in raycastables)
                {
                    if (raycastable.HandleRaycast(this))
                    {
                        SetCursor(raycastable.GetCursorType());
                        return true;
                    }
                }
            }
            return false;
        }

        private RaycastHit[] RaycastAllSorted()
        {
            RaycastHit[] hits = Physics.SphereCastAll(GetMouseRay(), _raycastRadius);
            float[] distances = new float[hits.Length];
            for(int i = 0; i < hits.Length; i++)
            {
                distances[i] = hits[i].distance;
            }
            Array.Sort(distances, hits);
            return hits;
        }

        private bool InteractWithMovement()
        {
            Vector3 target;
            bool hasHit = RaycastNavmesh(out target);

            if (hasHit)
            {
	            //if (!_mover.CanMoveTo(target)) return false;

                if (Input.GetMouseButton(0))
                {
	                if (_mover.CanMoveTo(target))
	                {
		                _mover.StartMoveAction(target, 1f); // Используем полную скорость для клика
	                }
	                else
	                {
		                // Можно добавить сюда звук ошибки или визуальный фидбек, если клик невозможен
	                }
                }
                SetCursor(CursorType.Movement);
                return true;
            }
            return false;
        }

        private bool RaycastNavmesh(out Vector3 target)
        {
            target = new Vector3();
            RaycastHit hit;
            bool hasHit = Physics.Raycast(GetMouseRay(), out hit);
            if (!hasHit) return false;
            NavMeshHit navMeshHit;
            bool hasCastToNavMesh = NavMesh.SamplePosition(hit.point, out navMeshHit, _maxNavmeshProjectionDistance, NavMesh.AllAreas);
            if (!hasCastToNavMesh) return false;

            target = navMeshHit.position;

            return true;
        }

        private void SetCursor(CursorType type)
        {
            CursorMapping mapping = GetCursorMapping(type);
            Cursor.SetCursor(mapping.texture, mapping.hotspot, CursorMode.Auto);
        }

        private CursorMapping GetCursorMapping(CursorType type)
        {
            foreach (CursorMapping mapping in _cursorMappings)
            {
                if(mapping.type == type)
                {
                    return mapping;
                }
            }
            return _cursorMappings[0];
        }

        public static Ray GetMouseRay()
        {
            return Camera.main.ScreenPointToRay(Input.mousePosition);
        }
    }
}
