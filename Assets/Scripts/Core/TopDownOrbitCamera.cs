using UnityEngine;
using GameDevTV.Saving; // Убедись, что using для ISaveable на месте, если он нужен

// Добавляем пространство имен UnityEngine.EventSystems для проверки UI
using UnityEngine.EventSystems;

public class TopDownOrbitCamera : MonoBehaviour, ISaveable // ISaveable опционально, если используется
{
	// ... (все твои [SerializeField] переменные остаются без изменений) ...
	[Header("Target")]
	[SerializeField] private Transform target;
	[SerializeField] private float targetHeightOffset = 1.2f;

	[Header("Orbit & Rotation")]
	[SerializeField] private float horizontalRotationSpeed = 200f;
	[SerializeField] private float verticalRotationSpeed = 100f;
	[Tooltip("Чувствительность вращения для мобильных устройств (подбери значение).")]
	[SerializeField] private float mobileRotationSensitivity = 0.4f; // Возможно, потребуется настройка
	[SerializeField] private float minVerticalAngle = 10f;
	[SerializeField] private float maxVerticalAngle = 85f;

	[Header("Zoom")]
	[SerializeField] private float distance = 7f;
	[SerializeField] private float minDistance = 2f;
	[SerializeField] private float maxDistance = 15f;
	[SerializeField] private float mouseZoomSpeed = 5f;
	[Tooltip("Чувствительность масштабирования для мобильных устройств (подбери значение).")]
	[SerializeField] private float mobileZoomSpeed = 0.05f; // Возможно, потребуется настройка

	[Header("Smoothing")]
	[SerializeField] private float positionSmoothTime = 0.15f;
	[SerializeField] private float rotationSmoothTime = 0.1f;
	[SerializeField] private float distanceSmoothTime = 0.15f;

	// Приватные переменные
	private float _currentX = 0f;
	private float _currentY = 45f;
	private float _smoothX = 0f;
	private float _smoothY = 0f;
	private float _smoothDistance = 0f;

	private Vector3 _currentPositionVelocity = Vector3.zero;
	private float _currentXVelocity = 0f;
	private float _currentYVelocity = 0f;
	private float _currentDistanceVelocity = 0f;

	// Флаги для управления состоянием ввода
	private bool _isDragging = false; // Для отслеживания активного перетаскивания мышью/пальцем
	private bool _isPinching = false; // Для отслеживания активного масштабирования пальцами

	// Структура для сохранения (если используется ISaveable)
	[System.Serializable]
	private struct CameraSaveData { public float currentX; public float currentY; public float distance; }

	void Start()
	{
		// ... (твой код Start) ...
		if (target == null) { /*...*/ enabled = false; return; }
		_smoothX = _currentX; _smoothY = _currentY; _smoothDistance = distance;
		distance = Mathf.Clamp(distance, minDistance, maxDistance);
	}

	void LateUpdate()
	{
		if (!target) return;

		// Проверяем, не над UI ли происходит взаимодействие, чтобы не вращать камеру случайно
		if (!IsPointerOverUIObject())
		{
			HandleInput();
		}
		else
		{
			// Сбрасываем флаги, если взаимодействие над UI
			_isDragging = false;
			_isPinching = false;
		}

		CalculateCameraTransform();
	}

	// --- ОБНОВЛЕННЫЙ МЕТОД ОБРАБОТКИ ВВОДА ---
	void HandleInput()
	{
		bool useMobileInput = false; // Определяем, использовать ли мобильную логику

#if UNITY_IOS || UNITY_ANDROID
		useMobileInput = true;
#elif UNITY_WEBGL
		useMobileInput = Input.touchSupported; // В WebGL ориентируемся на поддержку тачскрина
		// Debug.Log($"WebGL Touch Supported: {useMobileInput}"); // Для отладки
#endif

		// --- Логика для Мобильных устройств (iOS, Android, WebGL с тачскрином) ---
		if (useMobileInput)
		{
			// Масштабирование (Pinch-to-Zoom) - Проверяем в первую очередь
			if (Input.touchCount == 2)
			{
				Touch touchZero = Input.GetTouch(0);
				Touch touchOne = Input.GetTouch(1);

				// Рассчитываем позиции пальцев в предыдущем кадре
				Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

				// Рассчитываем расстояние между пальцами в текущем и предыдущем кадрах
				float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
				float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

				// Разница расстояний между кадрами
				float difference = currentMagnitude - prevMagnitude;

				// Изменяем дистанцию. Если пальцы расходятся (difference > 0), зум OUT (distance УВЕЛИЧИВАЕТСЯ).
				// Если пальцы сходятся (difference < 0), зум IN (distance УМЕНЬШАЕТСЯ).
				// Поэтому используем difference с обратным знаком или меняем порядок вычитания.
				distance -= difference * mobileZoomSpeed; // Подбери mobileZoomSpeed

				_isPinching = true; // Устанавливаем флаг масштабирования
				_isDragging = false; // Отключаем вращение во время масштабирования
			}
			else
			{
				_isPinching = false; // Сбрасываем флаг, если пальцев не два
			}

			// Вращение (один палец, свайп) - Срабатывает, только если не масштабируем
			if (!_isPinching && Input.touchCount == 1)
			{
				Touch touch = Input.GetTouch(0);

				if (touch.phase == TouchPhase.Began)
				{
					_isDragging = true; // Начинаем перетаскивание/вращение
				}
				else if (touch.phase == TouchPhase.Moved && _isDragging)
				{
					// Вращаем камеру на основе движения пальца
					_currentX += touch.deltaPosition.x * mobileRotationSensitivity;
					_currentY -= touch.deltaPosition.y * mobileRotationSensitivity;
				}
				else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
				{
					_isDragging = false; // Заканчиваем перетаскивание
				}
			}
			else if (Input.touchCount == 0) // Сбрасываем флаг, если палец убран
			{
				_isDragging = false;
			}
		}
		// --- Логика для ПК (Редактор, Standalone, WebGL без тачскрина) ---
		else // if (!useMobileInput)
		{
			// Вращение при зажатой ПКМ
			if (Input.GetMouseButtonDown(1))
			{
				_isDragging = true;
			}

			if (Input.GetMouseButton(1) && _isDragging)
			{
				// Используем Time.deltaTime для GetAxis, т.к. это непрерывное значение
				_currentX += Input.GetAxis("Mouse X") * horizontalRotationSpeed * Time.deltaTime;
				_currentY -= Input.GetAxis("Mouse Y") * verticalRotationSpeed * Time.deltaTime;
			}

			if (Input.GetMouseButtonUp(1))
			{
				_isDragging = false;
			}

			// Масштабирование колесиком мыши
			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if (Mathf.Abs(scroll) > 0.01f)
			{
				distance -= scroll * mouseZoomSpeed;
			}
		}

		// --- Ограничения применяются всегда ---
		_currentY = ClampAngle(_currentY, minVerticalAngle, maxVerticalAngle);
		distance = Mathf.Clamp(distance, minDistance, maxDistance);
	}
	// -------------------------------------------

	void CalculateCameraTransform()
	{
		// ... (твой код CalculateCameraTransform остается без изменений) ...
		_smoothX = Mathf.SmoothDamp(_smoothX, _currentX, ref _currentXVelocity, rotationSmoothTime);
		_smoothY = Mathf.SmoothDamp(_smoothY, _currentY, ref _currentYVelocity, rotationSmoothTime);
		_smoothDistance = Mathf.SmoothDamp(_smoothDistance, distance, ref _currentDistanceVelocity, distanceSmoothTime);

		Vector3 targetPivotPosition = target.position + Vector3.up * targetHeightOffset;
		Quaternion rotation = Quaternion.Euler(_smoothY, _smoothX, 0);
		Vector3 direction = rotation * Vector3.forward;
		Vector3 desiredPosition = targetPivotPosition - direction * _smoothDistance;

		transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentPositionVelocity, positionSmoothTime);
		transform.rotation = rotation;
	}

	private float ClampAngle(float angle, float min, float max)
	{
		// ... (твой код ClampAngle) ...
		return Mathf.Clamp(angle, min, max);
	}

	public void SetTarget(Transform newTarget)
	{
		// ... (твой код SetTarget) ...
	}

	// --- Вспомогательный метод для проверки UI ---
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
	// ------------------------------------------


	// --- ISaveable Implementation (если используется) ---
	public object CaptureState()
	{
		CameraSaveData data = new CameraSaveData();
		data.currentX = _currentX; data.currentY = _currentY; data.distance = distance;
		// Debug.Log($"Camera [{gameObject.name}] Capturing State..."); // Для отладки
		return data;
	}

	public void RestoreState(object state)
	{
		if (state is CameraSaveData data)
		{
			// Debug.Log($"Camera [{gameObject.name}] RestoreState called..."); // Для отладки
			_currentX = data.currentX; _currentY = data.currentY; distance = data.distance;
			_smoothX = _currentX; _smoothY = _currentY; _smoothDistance = distance;
		}
		// else { Debug.LogWarning(...); } // Для отладки
	}
	// -------------------------------------------------
}