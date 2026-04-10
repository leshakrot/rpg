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
        [Tooltip("Ширина виртуального джойстика (% от ширины экрана).")]
        [SerializeField] float joystickWidth = 0.3f;
        [Tooltip("Высота виртуального джойстика (% от высоты экрана).")]
        [SerializeField] float joystickHeight = 0.3f;
        [Tooltip("Ограничение свайпов для камеры только правой половиной экрана.")]
        [SerializeField] bool restrictCameraSwipesToRightHalf = true;

        public bool _isDraggingUI = false;

        private Transform cameraTransform;

        // Переменные для обработки мобильного ввода
        private Vector2 _touchStartPosition;
        private float _touchStartTime;
        private int _currentTouchId = -1;
        private bool _isTouchActive = false;
        private bool _isTouchProcessed = false;

        // Переменные для виртуального джойстика
        private bool _isJoystickActive = false;
        private Vector2 _joystickStartPosition;
        private Vector2 _joystickCurrentPosition;
        private float _joystickMaxRadius;
        private int _joystickTouchId = -1;
        private bool _wasJoystickUsedThisFrame = false;

        // Текстуры для отрисовки джойстика
        private Texture2D _joystickBgTexture;
        private Texture2D _joystickKnobTexture;
        private bool _joystickTexturesInitialized = false;

        private bool IsMobilePlatform
        {
            get
            {
                #if UNITY_EDITOR
                    return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android || 
                           UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS;
                #else
                    return Application.platform == RuntimePlatform.Android || 
                           Application.platform == RuntimePlatform.IPhonePlayer;
                #endif
            }
        }

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

            // Инициализация джойстика
            _joystickMaxRadius = Screen.width * 0.1f; // 10% экрана
            InitializeJoystickTextures();
        }

        private void InitializeJoystickTextures()
        {
            // Создаем текстуры для джойстика
            _joystickBgTexture = new Texture2D(1, 1);
            _joystickBgTexture.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            _joystickBgTexture.Apply();

            _joystickKnobTexture = new Texture2D(1, 1);
            _joystickKnobTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.8f));
            _joystickKnobTexture.Apply();

            _joystickTexturesInitialized = true;
        }

        private void Update()
        {
            if (InteractWithUI()) return;
            if (_health.IsDead())
            {
                SetCursor(CursorType.None);
                return;
            }

            _wasJoystickUsedThisFrame = false;

            // Обрабатываем мобильный джойстик только на мобильных платформах
            bool didMoveWithJoystick = IsMobilePlatform && HandleJoystickMovement();

            // Обрабатываем WASD движение на десктопе (только если не двигались джойстиком)
            bool didMoveWithWASD = !didMoveWithJoystick && HandleWASDMovement();

            // Обрабатываем способности (нажатие 1-6)
            UseAbilities();

            // На десктопе и WebGL обрабатываем взаимодействие с компонентами и движение по клику
            if (!IsMobilePlatform)
            {
                if (InteractWithComponent()) return;
                if (InteractWithMovement()) return;
            }
            // На мобильных устройствах обрабатываем тачи только если не использовался джойстик
            else if (!didMoveWithJoystick)
            {
                if (InteractWithComponent()) return;
                if (InteractWithMovement()) return;
            }

            SetCursor(CursorType.None);
        }

        private void OnGUI()
        {
            // Рисуем джойстик только на мобильных устройствах
            if (IsMobilePlatform && _isJoystickActive && _joystickTexturesInitialized)
            {
                // Рисуем фон джойстика
                float bgSize = _joystickMaxRadius * 2;
                GUI.DrawTexture(new Rect(_joystickStartPosition.x - bgSize / 2, Screen.height - _joystickStartPosition.y - bgSize / 2, bgSize, bgSize), _joystickBgTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, 0);

                // Рисуем кноб джойстика
                float knobSize = bgSize * 0.5f;
                GUI.DrawTexture(new Rect(_joystickCurrentPosition.x - knobSize / 2, Screen.height - _joystickCurrentPosition.y - knobSize / 2, knobSize, knobSize), _joystickKnobTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, 0);
            }
        }

        private bool HandleJoystickMovement()
        {
            // Проверяем, что мы на мобильной платформе
            if (!IsMobilePlatform)
                return false;

            int touchCount = Input.touchCount;
            if (touchCount == 0)
            {
                // Сбрасываем джойстик, если нет касаний
                ResetJoystick();
                return false;
            }

            // Обработка активных касаний
            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);

                // Если касание уже отслеживается для джойстика
                if (_isJoystickActive && touch.fingerId == _joystickTouchId)
                {
                    // Обновляем позицию джойстика
                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        _joystickCurrentPosition = touch.position;
                        Vector2 direction = _joystickCurrentPosition - _joystickStartPosition;

                        // Ограничиваем движение джойстика
                        if (direction.magnitude > _joystickMaxRadius)
                        {
                            direction = direction.normalized * _joystickMaxRadius;
                            _joystickCurrentPosition = _joystickStartPosition + direction;
                        }

                        // Преобразуем направление 2D в 3D для движения персонажа
                        HandleJoystickDirection(direction / _joystickMaxRadius);
                        _wasJoystickUsedThisFrame = true;
                    }
                    // Завершаем использование джойстика
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        ResetJoystick();
                    }

                    // Касание уже обработано джойстиком
                    continue;
                }

                // Создаем новый джойстик, если:
                // 1. Джойстик еще не активен
                // 2. Касание началось в левой половине экрана (если включено ограничение)
                // 3. Не обрабатывается камерой
                if (!_isJoystickActive && touch.phase == TouchPhase.Began &&
                    touch.position.x < Screen.width * 0.5f &&
                    !TopDownOrbitCamera.IsInputUsedByCamera)
                {
                    _joystickStartPosition = touch.position;
                    _joystickCurrentPosition = touch.position;
                    _joystickTouchId = touch.fingerId;
                    _isJoystickActive = true;
                }
            }

            // Возвращаем true если джойстик использовался в этом кадре
            return _wasJoystickUsedThisFrame;
        }

        private void HandleJoystickDirection(Vector2 normalizedDirection)
        {
            if (normalizedDirection.magnitude < 0.1f || cameraTransform == null)
                return;

            // Конвертируем 2D направление джойстика в 3D направление относительно камеры
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            // Направление движения в мировых координатах
            Vector3 moveDirection = (cameraForward * normalizedDirection.y + cameraRight * normalizedDirection.x).normalized;

            // Целевая позиция для NavMeshAgent
            Vector3 targetPosition = transform.position + moveDirection * lookAheadDistance;

            // Скорость зависит от интенсивности отклонения джойстика
            float speedFraction = normalizedDirection.magnitude * wasdMoveSpeedFraction;

            // Передаем команду движения в Mover
            _mover.StartMoveAction(targetPosition, speedFraction);

            // Поворачиваем персонажа в направлении движения
            HandleRotation(moveDirection);
        }

        private void ResetJoystick()
        {
            _isJoystickActive = false;
            _joystickTouchId = -1;
            _wasJoystickUsedThisFrame = false;
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
            
            // Проверяем, находится ли курсор над UI
            bool isOverUI = EventSystem.current.IsPointerOverGameObject();
            
            if (isOverUI)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isDraggingUI = true;
                }
                SetCursor(CursorType.UI);
                return true;
            }
            
            // Если мы начали драг на UI, продолжаем блокировать
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
            // Проверяем, что это клик мышью или тач
            bool isMouseClick = Input.GetMouseButtonDown(0);
            if (!isMouseClick && !IsMobilePlatform)
                return false;

            Vector3 target;
            bool hasHit = RaycastNavmesh(out target);

            if (hasHit)
            {
                if (!_mover.CanMoveTo(target)) 
                {
                    SetCursor(CursorType.None);
                    return true;
                }

                if (Input.GetMouseButton(0))
                {
                    _mover.StartMoveAction(target, wasdMoveSpeedFraction);
                    if (_movementTargetIndicator != null)
                    {
                        _movementTargetIndicator.transform.position = target;
                        _movementTargetIndicator.Play();
                    }
                }
                SetCursor(CursorType.Movement);
                return true;
            }
            return false;
        }

        private bool RaycastNavmesh(out Vector3 target)
        {
            target = Vector3.zero;
            
            RaycastHit hit;
            Ray ray = GetMouseRay();
            bool hasHit = Physics.Raycast(ray, out hit);

            if (!hasHit) return false;

            NavMeshHit navMeshHit;
            bool hasCastToNavMesh = NavMesh.SamplePosition(
                hit.point, out navMeshHit, _maxNavmeshProjectionDistance, NavMesh.AllAreas);

            if (!hasCastToNavMesh) return false;

            target = navMeshHit.position;
            
            return true;
        }

        private void SetCursor(CursorType type)
        {
            CursorMapping mapping = GetCursorMapping(type);
            if (mapping.texture != null)
            {
                Cursor.SetCursor(mapping.texture, mapping.hotspot, CursorMode.Auto);
            }
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

        // Публичный метод для проверки, активен ли джойстик
        // Может использоваться другими системами для проверки состояния ввода
        public bool IsJoystickActive()
        {
            return _isJoystickActive;
        }

        // Публичный метод для проверки, находится ли касание в левой половине экрана
        // (полезен для скрипта камеры, чтобы определить область для свайпов)
        public static bool IsTouchInLeftHalfOfScreen(Vector2 touchPosition)
        {
            return touchPosition.x < Screen.width * 0.5f;
        }
    }
}