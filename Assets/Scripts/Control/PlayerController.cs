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
        [SerializeField] private ParticleSystem _movementTargetIndicator;
        [SerializeField] private ParticleSystem _movementTargetIndicatorNoTarget;

        [Header("WASD Movement")]
        [Tooltip("Доля от максимальной скорости Mover при движении через WASD.")]
        [SerializeField] float wasdMoveSpeedFraction = 1f; // 1f = полная скорость
        [Tooltip("Насколько далеко впереди устанавливать цель для NavMeshAgent при движении WASD.")]
        [SerializeField] float lookAheadDistance = 1.0f;
        [Tooltip("Скорость поворота персонажа в сторону движения WASD.")]
        [SerializeField] float rotationSpeed = 10f;

        [Header("Mobile Controls")]
        [Tooltip("Минимальное время удержания пальца для определения тапа (сек).")]
        [SerializeField] float tapThresholdTime = 0.3f;
        [Tooltip("Максимальное расстояние перемещения пальца для определения тапа (пиксели).")]
        [SerializeField] float tapThresholdDistance = 15f;

        public bool _isDraggingUI = false;

        private Transform cameraTransform;

        // Переменные для обработки мобильного ввода
        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private int _currentTouchId = -1;
        private bool _isTouchActive = false;
        private bool _isTouchProcessed = false;

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

            // Обрабатываем WASD движение на десктопе
            bool didMoveWithWASD = HandleWASDMovement();

            // Обрабатываем способности (нажатие 1-6)
            UseAbilities();

            // Обрабатываем взаимодействие с компонентами
            if (InteractWithComponent()) return;

            // Обрабатываем движение по клику/тапу в мире
            if (InteractWithMovement()) return;

            // Если ничего не сработало, устанавливаем курсор по умолчанию
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
            // Если камера использует ввод, не обрабатываем взаимодействие с компонентами
            if (TopDownOrbitCamera.IsInputUsedByCamera) return false;

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
            for (int i = 0; i < hits.Length; i++)
            {
                distances[i] = hits[i].distance;
            }
            Array.Sort(distances, hits);
            return hits;
        }

        private bool InteractWithMovement()
        {
            // Если камера использует ввод, не обрабатываем движение персонажа
            if (TopDownOrbitCamera.IsInputUsedByCamera) return false;

#if UNITY_EDITOR || UNITY_STANDALONE
            return HandlePCMovement();
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
            return HandleMobileMovement();
#else
            return HandlePCMovement(); // Fallback для других платформ
#endif
        }

        private bool HandlePCMovement()
        {
            Vector3 target;
            bool hasHit = RaycastNavmesh(out target);

            if (hasHit)
            {
                if (Input.GetMouseButtonUp(0) && _mover.CanMoveTo(target))
                {
                    _movementTargetIndicator.transform.position = target;
                    _movementTargetIndicator.Play();
                }

                if (Input.GetMouseButton(0))
                {
                    if (_mover.CanMoveTo(target))
                    {
                        _mover.StartMoveAction(target, 1f);
                    }
                    else if (_movementTargetIndicatorNoTarget != null)
                    {
                        _movementTargetIndicatorNoTarget.transform.position = target;
                        _movementTargetIndicatorNoTarget.Play();
                    }
                }

                SetCursor(CursorType.Movement);
                return true;
            }

            return false;
        }

        private bool HandleMobileMovement()
        {
            // Если нет касаний, выходим
            if (Input.touchCount == 0)
            {
                // Сбрасываем состояние обработки тача
                ResetTouchState();
                return false;
            }

            // Если пользователь двигает камеру (свайп/зум), не обрабатываем движение персонажа
            if (TopDownOrbitCamera.IsInputUsedByCamera)
            {
                return false;
            }

            // Обрабатываем только одиночное касание - для движения нам нужен только один палец
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Новое касание началось
                if (touch.phase == TouchPhase.Began && !_isTouchActive)
                {
                    _touchStartPosition = touch.position;
                    _touchStartTime = Time.time;
                    _currentTouchId = touch.fingerId;
                    _isTouchActive = true;
                    _isTouchProcessed = false;
                }
                // Продолжение активного касания
                else if (touch.phase == TouchPhase.Moved && _isTouchActive && touch.fingerId == _currentTouchId)
                {
                    // Если палец двигается слишком далеко, это уже не тап, а свайп
                    // Камера должна обработать это как свайп, поэтому мы просто не обрабатываем это как движение
                    float distanceMoved = Vector2.Distance(_touchStartPosition, touch.position);
                    if (distanceMoved > tapThresholdDistance && !_isTouchProcessed)
                    {
                        // Этот тач больше не будет обрабатываться как тап для движения
                        _isTouchProcessed = true;
                    }
                }
                // Завершение касания
                else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) &&
                         _isTouchActive && touch.fingerId == _currentTouchId && !_isTouchProcessed)
                {
                    float touchDuration = Time.time - _touchStartTime;
                    float distanceMoved = Vector2.Distance(_touchStartPosition, touch.position);

                    // Если это был короткий тап с маленьким смещением, обрабатываем как движение
                    if (touchDuration < tapThresholdTime && distanceMoved < tapThresholdDistance)
                    {
                        // Выполняем рейкаст для определения цели движения
                        Vector3 target;
                        bool hasHit = RaycastNavmesh(out target);

                        if (hasHit && _mover.CanMoveTo(target))
                        {
                            _mover.StartMoveAction(target, 1f);

                            // Показываем индикатор движения
                            if (_movementTargetIndicator != null)
                            {
                                _movementTargetIndicator.transform.position = target;
                                _movementTargetIndicator.Play();
                            }

                            // Отмечаем, что тач обработан
                            _isTouchProcessed = true;
                            SetCursor(CursorType.Movement);

                            // Сбрасываем состояние тача
                            ResetTouchState();
                            return true;
                        }
                        else if (_movementTargetIndicatorNoTarget != null)
                        {
                            // Показываем индикатор невозможности движения
                            _movementTargetIndicatorNoTarget.transform.position = target;
                            _movementTargetIndicatorNoTarget.Play();
                        }
                    }

                    // Сбрасываем состояние тача
                    ResetTouchState();
                }
            }
            else if (Input.touchCount > 1)
            {
                // Если более одного касания, сбрасываем состояние тача
                ResetTouchState();
            }

            return false;
        }

        private void ResetTouchState()
        {
            _isTouchActive = false;
            _isTouchProcessed = false;
            _currentTouchId = -1;
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
                if (mapping.type == type)
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