using UnityEngine;
using UnityEngine.EventSystems; // Для проверки UI
using GameDevTV.Saving;         // Опционально, если используется ISaveable

// Убедись, что ты находишься в правильном пространстве имен, если оно есть
// namespace YourNamespace {

[DisallowMultipleComponent] // Предотвращает добавление скрипта несколько раз
public class TopDownOrbitCamera : MonoBehaviour, ISaveable // ISaveable опционально
{
    #region Variables

    [Header("Target")]
    [Tooltip("Объект, за которым следит камера (игрок).")]
    [SerializeField] private Transform target;
    [Tooltip("Смещение точки фокуса камеры по высоте от пивота цели.")]
    [SerializeField] private float targetHeightOffset = 1.2f;

    [Header("Orbit & Rotation")]
    [Tooltip("Скорость вращения камеры по горизонтали (ПК).")]
    [SerializeField] private float horizontalRotationSpeed = 200f;
    [Tooltip("Скорость вращения камеры по вертикали (ПК).")]
    [SerializeField] private float verticalRotationSpeed = 100f;
    [Tooltip("Чувствительность вращения для мобильных устройств (подбери значение).")]
    [SerializeField] private float mobileRotationSensitivity = 0.4f;
    [Tooltip("Минимальный угол наклона камеры (градусы).")]
    [SerializeField] private float minVerticalAngle = 10f;
    [Tooltip("Максимальный угол наклона камеры (градусы).")]
    [SerializeField] private float maxVerticalAngle = 85f;

    [Header("Zoom")]
    [Tooltip("Текущее расстояние до цели.")]
    [SerializeField] private float distance = 7f;
    [Tooltip("Минимальное расстояние до цели.")]
    [SerializeField] private float minDistance = 2f;
    [Tooltip("Максимальное расстояние до цели.")]
    [SerializeField] private float maxDistance = 15f;
    [Tooltip("Скорость приближения/отдаления колесиком мыши (ПК).")]
    [SerializeField] private float mouseZoomSpeed = 5f;
    [Tooltip("Чувствительность масштабирования для мобильных устройств (подбери значение).")]
    [SerializeField] private float mobileZoomSpeed = 0.05f;

    [Header("Mobile Controls")]
    [Tooltip("Минимальное расстояние для определения свайпа (в пикселях).")]
    [SerializeField] private float minSwipeDistance = 10f;
    [Tooltip("Максимальное время для определения свайпа (в секундах).")]
    [SerializeField] private float maxSwipeTime = 0.5f;
    [Tooltip("Задержка перед определением свайпа (в секундах).")]
    [SerializeField] private float swipeDetectionDelay = 0.1f;

    [Header("Smoothing")]
    [Tooltip("Время сглаживания движения камеры.")]
    [SerializeField] private float positionSmoothTime = 0.15f;
    [Tooltip("Время сглаживания вращения камеры.")]
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [Tooltip("Время сглаживания изменения дистанции.")]
    [SerializeField] private float distanceSmoothTime = 0.15f;

    [Header("Debugging")]
    [Tooltip("Включить подробные логи в консоль для отладки ввода.")]
    [SerializeField] private bool enableDebugLogs = true; // <-- Включи для теста!

    // Приватные переменные состояния
    private float _currentX = 0f;
    private float _currentY = 45f;
    private float _smoothX = 0f;
    private float _smoothY = 0f;
    private float _smoothDistance = 0f;

    // Переменные для SmoothDamp
    private Vector3 _currentPositionVelocity = Vector3.zero;
    private float _currentXVelocity = 0f;
    private float _currentYVelocity = 0f;
    private float _currentDistanceVelocity = 0f;

    // Флаги состояния ввода
    private bool _isDragging = false;
    private bool _isPinching = false;

    // Переменные для отслеживания точек касания на мобильных устройствах
    private int _dragFingerId = -1;
    private Vector2 _previousTouchPosition;
    private float _previousPinchDistance = 0f;
    private int _pinchFinger1Id = -1;
    private int _pinchFinger2Id = -1;

    // Переменные для определения свайпа
    private Vector2 _touchStartPosition;
    private float _touchStartTime;
    private bool _isSwipeDetected = false;
    private bool _isCheckingForSwipe = false;

    // Статическое свойство для PlayerController
    public static bool IsInputUsedByCamera { get; private set; }

    // Структура для сохранения (если используется ISaveable)
    [System.Serializable]
    private struct CameraSaveData { public float currentX; public float currentY; public float distance; }

    #endregion

    #region Unity Methods

    void Start()
    {
        if (target == null) { Debug.LogError($"[{gameObject.name}] Цель для камеры не назначена!", this); enabled = false; return; }
        _smoothX = _currentX; _smoothY = _currentY; _smoothDistance = distance;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Camera Initialized. Target: {target.name}");
    }

    void LateUpdate()
    {
        if (!target) return;
        IsInputUsedByCamera = false; // Сбрасываем флаг в начале кадра

        bool isOverUI = IsPointerOverUIObject();
        if (!isOverUI) { HandleInput(); }
        else
        {
            if (_isDragging || _isPinching)
            {
                if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Input interaction stopped: Pointer over UI.");
                ResetInputState();
            }
        }

        CalculateCameraTransform();
    }

    // Сбрасываем все флаги и состояние ввода
    private void ResetInputState()
    {
        _isDragging = false;
        _isPinching = false;
        _dragFingerId = -1;
        _pinchFinger1Id = -1;
        _pinchFinger2Id = -1;
        _isSwipeDetected = false;
        _isCheckingForSwipe = false;
        IsInputUsedByCamera = false;
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandlePCInput();
        if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Using PC Input (Editor/Standalone)");
#elif UNITY_IOS || UNITY_ANDROID
        HandleMobileInput();
        if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Using Mobile Input (iOS/Android)");
#elif UNITY_WEBGL
        if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Checking Input (WebGL)");
        
        // Проверяем, имеем ли мы дело с мобильным устройством или ПК в WebGL
        bool pcInputActive = Input.GetMouseButton(1) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f;
        bool mobileInputActive = Input.touchCount > 0;

        // Разделяем логику для ПК и мобильных устройств в WebGL
        if (!mobileInputActive && pcInputActive)
        {
            // ПК ввод в WebGL
            if (enableDebugLogs && Time.frameCount % 60 == 0) Debug.Log($"[{gameObject.name}] PC Input Detected (WebGL)");
            HandlePCInput();
        }
        else if (mobileInputActive)
        {
            // Мобильный ввод в WebGL
            if (enableDebugLogs && Time.frameCount % 60 == 0) Debug.Log($"[{gameObject.name}] Mobile Input Detected (WebGL)");
            HandleMobileInput();
        }
#else
        HandlePCInput(); // Fallback
        if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Using PC Input (Fallback)");
#endif

        // Ограничения
        _currentY = ClampAngle(_currentY, minVerticalAngle, maxVerticalAngle);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    // --- Логика для ПК управления (остается в основном как есть) ---
    private void HandlePCInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (!IsPointerOverUIObject())
            {
                _isDragging = true;
                IsInputUsedByCamera = true;
                if (enableDebugLogs) Debug.Log("PC Drag Started (RMB Down)");
            }
        }

        if (Input.GetMouseButton(1) && _isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
            {
                _currentX += mouseX * horizontalRotationSpeed * Time.deltaTime;
                _currentY -= mouseY * verticalRotationSpeed * Time.deltaTime;
                IsInputUsedByCamera = true;
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (enableDebugLogs) Debug.Log("PC Drag Ended (RMB Up)");
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (enableDebugLogs) Debug.Log($"PC Scroll Detected: {scroll}");
            if (!IsPointerOverUIObject())
            {
                distance -= scroll * mouseZoomSpeed;
                IsInputUsedByCamera = true;
                _smoothDistance = Mathf.SmoothDamp(_smoothDistance, distance, ref _currentDistanceVelocity, distanceSmoothTime / 2f);
            }
        }
    }

    // --- ПЕРЕРАБОТАННАЯ ЛОГИКА МОБИЛЬНОГО ВВОДА ---
    private void HandleMobileInput()
    {
        int touchCount = Input.touchCount;

        if (touchCount == 0)
        {
            // Сбрасываем состояние, если нет касаний
            ResetInputState();
            return;
        }

        if (enableDebugLogs && Time.frameCount % 30 == 0) Debug.Log($"[{gameObject.name}] Mobile Input: Touch Count = {touchCount}");

        // Сначала проверяем наличие нескольких касаний (приоритет у масштабирования)
        if (touchCount >= 2)
        {
            HandlePinchZoom();
            // Если обнаружен пинч, то вращение отключаем
            _isDragging = false;
            _dragFingerId = -1;
            _isSwipeDetected = false;
            _isCheckingForSwipe = false;
        }
        else if (touchCount == 1)
        {
            // Если только одно касание, обрабатываем его для вращения/движения
            HandleSingleTouchInput();
        }
    }

    private void HandleSingleTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        // Обработка начала касания
        if (touch.phase == TouchPhase.Began)
        {
            // Запоминаем начальную позицию и время для определения свайпа
            _touchStartPosition = touch.position;
            _touchStartTime = Time.time;
            _isCheckingForSwipe = true;
            _isSwipeDetected = false;

            // Пока не определили свайп, не устанавливаем флаг камеры
            if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Touch Began: Checking for swipe...");
        }
        // Обработка движения пальца
        else if (touch.phase == TouchPhase.Moved && _isCheckingForSwipe)
        {
            // Если уже определили свайп, вращаем камеру
            if (_isSwipeDetected)
            {
                // Вращение камеры при свайпе
                _isDragging = true;
                _dragFingerId = touch.fingerId;

                Vector2 delta = touch.deltaPosition;
                _currentX += delta.x * mobileRotationSensitivity;
                _currentY -= delta.y * mobileRotationSensitivity;

                // Устанавливаем флаг для PlayerController
                IsInputUsedByCamera = true;

                if (enableDebugLogs && Time.frameCount % 10 == 0)
                    Debug.Log($"[{gameObject.name}] Swipe Dragging: Delta = {delta}");
            }
            // Если прошло время задержки и палец двигался достаточно, определяем свайп
            else if (Time.time - _touchStartTime > swipeDetectionDelay)
            {
                float distance = Vector2.Distance(_touchStartPosition, touch.position);

                // Если палец двигался достаточно, считаем это свайпом
                if (distance > minSwipeDistance)
                {
                    _isSwipeDetected = true;
                    _isDragging = true;
                    _dragFingerId = touch.fingerId;
                    _previousTouchPosition = touch.position;
                    IsInputUsedByCamera = true; // Блокируем ввод для PlayerController

                    if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Swipe Detected: Distance = {distance}");
                }
            }
        }
        // Обработка окончания касания
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            if (_isSwipeDetected)
            {
                _isDragging = false;
                _dragFingerId = -1;
                _isSwipeDetected = false;
                _isCheckingForSwipe = false;

                if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Swipe Ended");
            }
            else
            {
                // Если это был тап (не свайп), камера не блокирует ввод
                _isSwipeDetected = false;
                _isCheckingForSwipe = false;
                IsInputUsedByCamera = false;

                if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Tap Detected: Not using camera input");
            }
        }
    }

    private void HandlePinchZoom()
    {
        // Нам нужны как минимум 2 касания для масштабирования
        if (Input.touchCount < 2) return;

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // Инициализация жеста масштабирования
        if (!_isPinching)
        {
            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began ||
                (touch0.phase == TouchPhase.Moved && touch1.phase == TouchPhase.Moved))
            {
                _isPinching = true;
                _pinchFinger1Id = touch0.fingerId;
                _pinchFinger2Id = touch1.fingerId;
                _previousPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                _isDragging = false; // Отключаем вращение при масштабировании
                _isSwipeDetected = false;
                _isCheckingForSwipe = false;
                IsInputUsedByCamera = true; // Важно! Блокируем ввод для PlayerController

                if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Pinch Started: Distance = {_previousPinchDistance}");
            }
        }
        // Продолжаем жест масштабирования
        else if (_isPinching)
        {
            // Проверяем, что это те же пальцы, что и начали масштабирование
            if ((touch0.fingerId == _pinchFinger1Id && touch1.fingerId == _pinchFinger2Id) ||
                (touch0.fingerId == _pinchFinger2Id && touch1.fingerId == _pinchFinger1Id))
            {
                // Вычисляем текущее расстояние между пальцами
                float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);

                // Вычисляем разницу с предыдущим расстоянием
                float pinchDelta = currentPinchDistance - _previousPinchDistance;

                // Изменяем масштаб в зависимости от изменения расстояния между пальцами
                if (Mathf.Abs(pinchDelta) > 1f)  // Порог для избежания микро-движений
                {
                    distance -= pinchDelta * mobileZoomSpeed;
                    _previousPinchDistance = currentPinchDistance;
                    IsInputUsedByCamera = true; // Продолжаем блокировать ввод для PlayerController

                    if (enableDebugLogs && Time.frameCount % 10 == 0)
                        Debug.Log($"[{gameObject.name}] Pinching: Delta = {pinchDelta}, New Distance = {distance}");
                }
            }

            // Завершаем жест масштабирования, если один из пальцев поднят
            if (touch0.phase == TouchPhase.Ended || touch0.phase == TouchPhase.Canceled ||
                touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled)
            {
                _isPinching = false;
                _pinchFinger1Id = -1;
                _pinchFinger2Id = -1;

                if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Pinch Ended");
            }
        }
    }

    #endregion

    #region Camera Calculation & Helpers

    private void CalculateCameraTransform()
    {
        _smoothX = Mathf.SmoothDamp(_smoothX, _currentX, ref _currentXVelocity, rotationSmoothTime);
        _smoothY = Mathf.SmoothDamp(_smoothY, _currentY, ref _currentYVelocity, rotationSmoothTime);
        _smoothDistance = Mathf.SmoothDamp(_smoothDistance, distance, ref _currentDistanceVelocity, distanceSmoothTime);
        if (target == null) return;
        Vector3 targetPivotPosition = target.position + Vector3.up * targetHeightOffset;
        Quaternion rotation = Quaternion.Euler(_smoothY, _smoothX, 0);
        Vector3 direction = rotation * Vector3.forward;
        Vector3 desiredPosition = targetPivotPosition - direction * _smoothDistance;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentPositionVelocity, positionSmoothTime);
        transform.rotation = rotation;
    }

    private float ClampAngle(float angle, float min, float max) { return Mathf.Clamp(angle, min, max); }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null) { Debug.LogError($"[{gameObject.name}] Новая цель для камеры не назначена (null)!", this); enabled = false; }
        else { if (!enabled) enabled = true; if (enableDebugLogs) Debug.Log($"[{gameObject.name}] New Target Set: {target.name}"); }
    }

    private bool IsPointerOverUIObject()
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        if (Input.touchCount > 0) eventData.position = Input.GetTouch(0).position; else eventData.position = Input.mousePosition;
        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    #endregion

    #region ISaveable Implementation (Optional)
    // Переименовал структуру во избежание конфликтов имен
    [System.Serializable] private struct CameraSaveData_TopDownOrbit { public float currentX; public float currentY; public float distance; }

    public object CaptureState()
    {
        CameraSaveData_TopDownOrbit data = new CameraSaveData_TopDownOrbit();
        data.currentX = _currentX; data.currentY = _currentY; data.distance = distance;
        if (enableDebugLogs) Debug.Log($"[{gameObject.name}] CaptureState: X={data.currentX}, Y={data.currentY}, Dist={data.distance}");
        return data;
    }

    public void RestoreState(object state)
    {
        if (state is CameraSaveData_TopDownOrbit data) { _currentX = data.currentX; _currentY = data.currentY; distance = data.distance; _smoothX = _currentX; _smoothY = _currentY; _smoothDistance = distance; if (enableDebugLogs) Debug.Log($"[{gameObject.name}] RestoreState: X={data.currentX}, Y={data.currentY}, Dist={data.distance}"); }
        else if (state != null) { Debug.LogWarning($"[{gameObject.name}] RestoreState received invalid data type: {state.GetType()}", this); }
        else { if (enableDebugLogs) Debug.LogWarning($"[{gameObject.name}] RestoreState received null data.", this); }
    }
    #endregion
}

// } // Конец namespace, если используется