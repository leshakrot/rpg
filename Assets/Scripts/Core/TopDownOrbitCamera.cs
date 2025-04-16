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

	[Header("Smoothing")]
	[Tooltip("Время сглаживания движения камеры.")]
	[SerializeField] private float positionSmoothTime = 0.15f;
	[Tooltip("Время сглаживания вращения камеры.")]
	[SerializeField] private float rotationSmoothTime = 0.1f;
	[Tooltip("Время сглаживания изменения дистанции.")]
	[SerializeField] private float distanceSmoothTime = 0.15f;

	[Header("Debugging")]
	[Tooltip("Включить подробные логи в консоль для отладки ввода.")]
	[SerializeField] private bool enableDebugLogs = false;

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
	private bool _isDragging = false; // Общий флаг для ПКМ и свайпа
	private bool _isPinching = false;

	// Статическое свойство для PlayerController
	/// <summary>
	/// Показывает, был ли ввод использован камерой в текущем кадре (для вращения или масштабирования).
	/// Используется PlayerController, чтобы игнорировать ввод для движения.
	/// </summary>
	public static bool IsInputUsedByCamera { get; private set; }

	// Структура для сохранения (если используется ISaveable)
	[System.Serializable]
	private struct CameraSaveData { public float currentX; public float currentY; public float distance; }

    #endregion

    #region Unity Methods

	void Start()
	{
		if (target == null)
		{
			Debug.LogError($"[{gameObject.name}] Цель для камеры не назначена!", this);
			enabled = false; // Выключаем скрипт, если нет цели
			return;
		}

		// Инициализация углов (можно оставить как есть или использовать начальное положение камеры)
		// Vector3 angles = transform.eulerAngles; _currentX = angles.y; _currentY = angles.x;

		_smoothX = _currentX;
		_smoothY = _currentY;
		_smoothDistance = distance;

		distance = Mathf.Clamp(distance, minDistance, maxDistance);

		if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Camera Initialized. Target: {target.name}");
	}

	void LateUpdate()
	{
		if (!target) return; // Если цель пропала во время игры

		// Сбрасываем статический флаг в начале каждого кадра LateUpdate
		IsInputUsedByCamera = false;

		// Проверяем, не над UI ли курсор/палец, чтобы игнорировать ввод для камеры
		bool isOverUI = IsPointerOverUIObject();
		if (!isOverUI)
		{
			HandleInput(); // Обрабатываем ввод, если не над UI
		}
		else
		{
			// Если курсор над UI, сбрасываем флаги взаимодействия, чтобы избежать залипания
			if (_isDragging || _isPinching)
			{
				if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Input interaction stopped: Pointer over UI.");
				_isDragging = false;
				_isPinching = false;
			}
		}

		// Вычисляем и применяем позицию/вращение камеры
		CalculateCameraTransform();
	}

    #endregion

    #region Input Handling

	private void HandleInput()
	{
		// --- Определение платформы ---
#if UNITY_EDITOR || UNITY_STANDALONE
		// В редакторе и Standalone всегда ПК управление
		HandlePCInput();
		if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Using PC Input (Editor/Standalone)");

#elif UNITY_IOS || UNITY_ANDROID
		// На мобильных платформах всегда мобильное управление
		HandleMobileInput();
		if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Using Mobile Input (iOS/Android)");

#elif UNITY_WEBGL
		// --- Особая логика для WebGL ---
		if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Checking Input (WebGL)");

		// 1. Проверяем активный ввод с ПК (мышь/колесо)
		// GetMouseButton(1) - проверка удержания ПКМ
		// GetMouseButtonDown(1) - проверка нажатия ПКМ (важно для начала перетаскивания)
		// GetMouseButtonUp(1) - проверка отпускания ПКМ (важно для окончания перетаскивания)
		// GetAxis("Mouse ScrollWheel") - проверка прокрутки
		bool pcInputActive = Input.GetMouseButtonDown(1) || Input.GetMouseButton(1) || Input.GetMouseButtonUp(1) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f;

		if (pcInputActive)
		{
		// Используем ПК-логику, если было взаимодействие с мышью/колесом
		if (enableDebugLogs && !_isPinching) Debug.Log($"[{gameObject.name}] PC Input Detected This Frame (WebGL) -> Handling PC");
		HandlePCInput();
		// Устанавливаем флаг, что ввод был использован камерой (даже если это ПКМ)
		if (_isDragging || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f) { IsInputUsedByCamera = true; }
		// Сбрасываем флаги мобильного ввода на всякий случай
		_isPinching = false;
		}
		else // 2. Если активного ввода с ПК не было, проверяем мобильный ввод
		{
		if (enableDebugLogs && Input.touchCount > 0) Debug.Log($"[{gameObject.name}] No PC Input Detected. Handling Mobile (WebGL)");
		HandleMobileInput(); // Обработка тач-ввода установит IsInputUsedByCamera внутри себя
		// Сбрасываем флаг _isDragging от ПК, если он вдруг остался активным, а мы перешли к тачам
		if(Input.touchCount > 0 && _isDragging && !Input.GetMouseButton(1))
		{
		_isDragging = false;
		}
		}
#else
		// Другие платформы - используем ПК как запасной вариант
		HandlePCInput();
		if (enableDebugLogs && Time.frameCount % 120 == 0) Debug.Log($"[{gameObject.name}] Using PC Input (Fallback)");
#endif

		// --- Ограничения применяются всегда после обработки ввода ---
		_currentY = ClampAngle(_currentY, minVerticalAngle, maxVerticalAngle);
		distance = Mathf.Clamp(distance, minDistance, maxDistance);
	}

	// --- Логика для ПК управления ---
	private void HandlePCInput()
	{
		// Вращение (ПКМ)
		if (Input.GetMouseButtonDown(1))
		{
			// Начинаем вращение только если не над UI (дополнительная проверка)
			if (!IsPointerOverUIObject())
			{
				_isDragging = true;
				IsInputUsedByCamera = true; // Ввод используется камерой
				if (enableDebugLogs) Debug.Log("PC Drag Started (RMB Down)");
			}
		}

		// Используем GetMouseButton, т.к. GetAxis может быть не 0 из-за легкого дрожания
		if (Input.GetMouseButton(1) && _isDragging)
		{
			float mouseX = Input.GetAxis("Mouse X");
			float mouseY = Input.GetAxis("Mouse Y");
			if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f) // Двигаем только если есть реальное смещение
			{
				_currentX += mouseX * horizontalRotationSpeed * Time.deltaTime;
				_currentY -= mouseY * verticalRotationSpeed * Time.deltaTime;
				IsInputUsedByCamera = true; // Ввод используется камерой
			}
		}

		if (Input.GetMouseButtonUp(1))
		{
			if (_isDragging)
			{
				_isDragging = false;
				// IsInputUsedByCamera сбросится в следующем кадре сам
				if (enableDebugLogs) Debug.Log("PC Drag Ended (RMB Up)");
			}
		}

		// Масштабирование (Колесо мыши)
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (Mathf.Abs(scroll) > 0.01f)
		{
			if (enableDebugLogs) Debug.Log($"PC Scroll Detected: {scroll}");
			// Убедимся, что масштабирование не происходит, если курсор над UI
			if (!IsPointerOverUIObject())
			{
				distance -= scroll * mouseZoomSpeed;
				// Устанавливаем флаг, т.к. скролл - это тоже взаимодействие с камерой
				IsInputUsedByCamera = true;
				// Optional: More responsive zoom smoothing
				_smoothDistance = Mathf.SmoothDamp(_smoothDistance, distance, ref _currentDistanceVelocity, distanceSmoothTime / 2f);
			}
		}
	}

	// --- Логика для Мобильного управления ---
	private void HandleMobileInput()
	{
		int touchCount = Input.touchCount;

		// Масштабирование (Pinch-to-Zoom) - приоритет над вращением
		if (touchCount == 2)
		{
			if (!_isPinching) // Начало масштабирования
			{
				_isPinching = true;
				_isDragging = false; // Отменяем возможное вращение
				IsInputUsedByCamera = true; // Ввод используется камерой
				if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Pinch Started.");
			}

			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);
			Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
			float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
			float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
			float difference = currentMagnitude - prevMagnitude;

			distance -= difference * mobileZoomSpeed;
			IsInputUsedByCamera = true; // Ввод используется камерой во время pinch
		}
		else // Если пальцев не два
		{
			if (_isPinching) // Конец масштабирования
			{
				_isPinching = false;
				// IsInputUsedByCamera сбросится в следующем кадре
				if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Pinch Ended.");
			}
		}

		// Вращение (Swipe) - только если не масштабируем и один палец
		if (!_isPinching && touchCount == 1)
		{
			Touch touch = Input.GetTouch(0);

			if (touch.phase == TouchPhase.Began)
			{
				// Начинаем перетаскивание только если не над UI
				if (!_isDragging && !IsPointerOverUIObject())
				{
					_isDragging = true;
					IsInputUsedByCamera = true; // Ввод используется камерой
					if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Swipe Drag Started.");
				}
			}
			else if (touch.phase == TouchPhase.Moved && _isDragging)
			{
				// Проверяем еще раз на UI на всякий случай во время движения
				if (!IsPointerOverUIObject())
				{
					_currentX += touch.deltaPosition.x * mobileRotationSensitivity;
					_currentY -= touch.deltaPosition.y * mobileRotationSensitivity;
					IsInputUsedByCamera = true; // Ввод используется камерой
				}
				else // Если палец заехал на UI во время свайпа, прекращаем вращение
				{
					_isDragging = false;
					if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Swipe Drag Stopped (Moved over UI).");
				}
			}
			else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
			{
				if (_isDragging)
				{
					_isDragging = false;
					// IsInputUsedByCamera сбросится в следующем кадре
					if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Swipe Drag Ended.");
				}
			}
		}
		// Сброс флага вращения, если пальцев не 1 (и не 2, т.к. это pinch)
		else if (touchCount != 1)
		{
			if (_isDragging)
			{
				_isDragging = false;
				// IsInputUsedByCamera сбросится в следующем кадре
				if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Swipe Drag Reset (Touch count: {touchCount}).");
			}
		}
	}

    #endregion

    #region Camera Calculation & Helpers

	private void CalculateCameraTransform()
	{
		// Сглаживание целевых значений
		_smoothX = Mathf.SmoothDamp(_smoothX, _currentX, ref _currentXVelocity, rotationSmoothTime);
		_smoothY = Mathf.SmoothDamp(_smoothY, _currentY, ref _currentYVelocity, rotationSmoothTime);
		_smoothDistance = Mathf.SmoothDamp(_smoothDistance, distance, ref _currentDistanceVelocity, distanceSmoothTime);

		// Вычисление желаемого вращения и позиции
		// Убедимся, что target все еще существует (может быть уничтожен)
		if (target == null) return;
		Vector3 targetPivotPosition = target.position + Vector3.up * targetHeightOffset;

		Quaternion rotation = Quaternion.Euler(_smoothY, _smoothX, 0);
		Vector3 direction = rotation * Vector3.forward; // Направление от камеры к цели
		Vector3 desiredPosition = targetPivotPosition - direction * _smoothDistance; // Отступаем назад от цели

		// Применение сглаженной позиции и точного вращения
		transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentPositionVelocity, positionSmoothTime);
		transform.rotation = rotation;
	}

	private float ClampAngle(float angle, float min, float max)
	{
		// Простая версия для вертикального угла
		return Mathf.Clamp(angle, min, max);
	}

	public void SetTarget(Transform newTarget)
	{
		target = newTarget;
		if (target == null)
		{
			Debug.LogError($"[{gameObject.name}] Новая цель для камеры не назначена (null)!", this);
			enabled = false;
		}
		else
		{
			if (!enabled) enabled = true; // Включаем, если была выключена
			if (enableDebugLogs) Debug.Log($"[{gameObject.name}] New Target Set: {target.name}");
		}
	}

	// Проверка, находится ли курсор/палец над элементом UI
	private bool IsPointerOverUIObject()
	{
		// Проверяем, есть ли вообще EventSystem
		if (EventSystem.current == null) return false;

		// Создаем данные для PointerEvent
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);

		// Определяем позицию для проверки (мышь или первый тач)
		if (Input.touchCount > 0)
		{
			eventDataCurrentPosition.position = Input.GetTouch(0).position;
		}
		else
		{
			eventDataCurrentPosition.position = Input.mousePosition;
		}

		// Выполняем Raycast по UI
		System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0; // Если есть хоть один результат, значит над UI
	}

    #endregion

    #region ISaveable Implementation (Optional)

	// --- Структура для сохранения данных ---
	// [System.Serializable] // Раскомментируй, если используешь ISaveable
	// private struct CameraSaveData { public float currentX; public float currentY; public float distance; }

	public object CaptureState()
	{
		CameraSaveData data = new CameraSaveData();
		data.currentX = _currentX;
		data.currentY = _currentY;
		data.distance = distance;
		if (enableDebugLogs) Debug.Log($"[{gameObject.name}] CaptureState: X={data.currentX}, Y={data.currentY}, Dist={data.distance}");
		return data;
	}

	public void RestoreState(object state)
	{
		if (state is CameraSaveData data)
		{
			_currentX = data.currentX;
			_currentY = data.currentY;
			distance = data.distance;
			// Немедленно обновляем сглаженные значения
			_smoothX = _currentX;
			_smoothY = _currentY;
			_smoothDistance = distance;
			if (enableDebugLogs) Debug.Log($"[{gameObject.name}] RestoreState: X={data.currentX}, Y={data.currentY}, Dist={data.distance}");
		}
		else if (state != null)
		{
			Debug.LogWarning($"[{gameObject.name}] RestoreState received invalid data type: {state.GetType()}", this);
		}
		else
		{
			// Если state == null, возможно, это первое сохранение или файл поврежден.
			// Можно либо ничего не делать, либо сбросить к дефолтным значениям.
			// Оставим как есть - камера останется в положении из Start().
			if (enableDebugLogs) Debug.LogWarning($"[{gameObject.name}] RestoreState received null data.", this);
		}
	}

    #endregion
}

// } // Конец namespace, если используется