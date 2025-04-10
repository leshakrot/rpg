using UnityEngine;
using GameDevTV.Saving;
using System.Collections.Generic;

public class TopDownOrbitCamera : MonoBehaviour, ISaveable
{
	[Header("Target")]
	[Tooltip("Объект, за которым следит камера (игрок).")]
	[SerializeField] private Transform target;
	[Tooltip("Смещение точки фокуса камеры по высоте от пивота цели.")]
	[SerializeField] private float targetHeightOffset = 1.2f;

	[Header("Orbit & Rotation")]
	[Tooltip("Скорость вращения камеры по горизонтали.")]
	[SerializeField] private float horizontalRotationSpeed = 200f;
	[Tooltip("Скорость вращения камеры по вертикали.")]
	[SerializeField] private float verticalRotationSpeed = 100f;
	[Tooltip("Чувствительность вращения для мобильных устройств.")]
	[SerializeField] private float mobileRotationSensitivity = 0.5f;
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
	[Tooltip("Скорость приближения/отдаления колесиком мыши.")]
	[SerializeField] private float mouseZoomSpeed = 5f;
	[Tooltip("Скорость приближения/отдаления жестом на мобильных устройствах.")]
	[SerializeField] private float mobileZoomSpeed = 0.02f;

	[Header("Smoothing")]
	[Tooltip("Время сглаживания движения камеры.")]
	[SerializeField] private float positionSmoothTime = 0.15f;
	[Tooltip("Время сглаживания вращения камеры.")]
	[SerializeField] private float rotationSmoothTime = 0.1f;
	[Tooltip("Время сглаживания изменения дистанции.")]
	[SerializeField] private float distanceSmoothTime = 0.15f;

	// Приватные переменные
	private float _currentX = 0f;
	private float _currentY = 45f; // Начальный угол наклона
	private float _smoothX = 0f;
	private float _smoothY = 0f;
	private float _smoothDistance = 0f;

	private Vector3 _currentPositionVelocity = Vector3.zero;
	private float _currentXVelocity = 0f;
	private float _currentYVelocity = 0f;
	private float _currentDistanceVelocity = 0f;

	private bool _isRotating = false; // Флаг для отслеживания вращения (особенно на мобильных)
	private float _initialPinchDistance = 0f; // Для мобильного зума
	private float _initialDistanceOnPinch = 0f; // Для мобильного зума
	
	[System.Serializable]
	private struct CameraSaveData
	{
		public float currentX;
		public float currentY;
		public float distance;
	}
	

	void Start()
	{
		if (target == null)
		{
			Debug.LogError("Цель для камеры не назначена!");
			enabled = false; // Выключаем скрипт, если нет цели
			return;
		}

		// Инициализация углов на основе начальной позиции камеры, если нужно
		// Vector3 angles = transform.eulerAngles;
		// _currentX = angles.y;
		// _currentY = angles.x;

		// Устанавливаем начальные сглаженные значения
		_smoothX = _currentX;
		_smoothY = _currentY;
		_smoothDistance = distance;

		// Ограничиваем начальную дистанцию
		distance = Mathf.Clamp(distance, minDistance, maxDistance);
	}

	void LateUpdate()
	{
		if (!target) return; // Если цель пропала во время игры

		HandleInput();
		CalculateCameraTransform();
	}

	void HandleInput()
	{
		// --- Обработка ввода для ПК (Мышь) ---
#if UNITY_EDITOR || UNITY_STANDALONE
		// Вращение при зажатой ПКМ
		if (Input.GetMouseButtonDown(1)) // Нажатие ПКМ
		{
			_isRotating = true;
		}
		if (Input.GetMouseButtonUp(1)) // Отпускание ПКМ
		{
			_isRotating = false;
		}
		if (_isRotating && Input.GetMouseButton(1))
		{
			_currentX += Input.GetAxis("Mouse X") * horizontalRotationSpeed * Time.deltaTime;
			_currentY -= Input.GetAxis("Mouse Y") * verticalRotationSpeed * Time.deltaTime; // Инвертируем Y для привычного управления
		}

		// Масштабирование колесиком мыши
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (Mathf.Abs(scroll) > 0.01f) // Проверяем, было ли вращение колеса
		{
			distance -= scroll * mouseZoomSpeed;
		}

#endif

		// --- Обработка ввода для Мобильных устройств ---
#if UNITY_IOS || UNITY_ANDROID
		// Вращение (один палец, свайп)
		if (Input.touchCount == 1)
		{
		Touch touch = Input.GetTouch(0);
		if (touch.phase == TouchPhase.Began)
		{
		_isRotating = true; // Можно использовать для определения начала свайпа
		}
		else if (touch.phase == TouchPhase.Moved && _isRotating)
		{
		_currentX += touch.deltaPosition.x * mobileRotationSensitivity * Time.deltaTime * (horizontalRotationSpeed / 5); // Подбираем коэфф.
		_currentY -= touch.deltaPosition.y * mobileRotationSensitivity * Time.deltaTime * (verticalRotationSpeed / 5); // Подбираем коэфф.
		}
		else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
		{
		_isRotating = false;
		}
		}
		// Если было больше одного касания, сбрасываем флаг вращения одним пальцем
		else if (Input.touchCount > 1)
		{
		_isRotating = false;
		}


		// Масштабирование (два пальца, pinch-to-zoom)
		if (Input.touchCount == 2)
		{
		Touch touchZero = Input.GetTouch(0);
		Touch touchOne = Input.GetTouch(1);

		// Если один из пальцев только что коснулся экрана, запоминаем начальное состояние
		if (touchZero.phase == TouchPhase.Began || touchOne.phase == TouchPhase.Began)
		{
		_initialPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);
		_initialDistanceOnPinch = distance; // Запоминаем текущую дистанцию камеры
		_isRotating = false; // Отключаем вращение во время зума
		}
		// Если пальцы двигаются
		else if (touchZero.phase == TouchPhase.Moved || touchOne.phase == TouchPhase.Moved)
		{
		float currentPinchDistance = Vector2.Distance(touchZero.position, touchOne.position);

		// Избегаем деления на ноль
		if (_initialPinchDistance > 0.01f)
		{
		// Рассчитываем новую дистанцию на основе изменения расстояния между пальцами
		// Чем БОЛЬШЕ текущее расстояние между пальцами относительно начального, тем МЕНЬШЕ должна быть distance (приближение)
		// Чем МЕНЬШЕ текущее расстояние, тем БОЛЬШЕ distance (отдаление)
		float scaleFactor = _initialPinchDistance / currentPinchDistance;
		distance = _initialDistanceOnPinch * scaleFactor;

		// Альтернативный вариант: изменение дистанции пропорционально изменению расстояния между пальцами
		// float deltaDistance = (_initialPinchDistance - currentPinchDistance) * mobileZoomSpeed;
		// distance = _initialDistanceOnPinch + deltaDistance;
		}
		}
		}
#endif

		// Ограничение вертикального угла
		_currentY = ClampAngle(_currentY, minVerticalAngle, maxVerticalAngle);

		// Ограничение дистанции
		distance = Mathf.Clamp(distance, minDistance, maxDistance);
	}

	void CalculateCameraTransform()
	{
		// Сглаживание значений
		_smoothX = Mathf.SmoothDamp(_smoothX, _currentX, ref _currentXVelocity, rotationSmoothTime);
		_smoothY = Mathf.SmoothDamp(_smoothY, _currentY, ref _currentYVelocity, rotationSmoothTime);
		_smoothDistance = Mathf.SmoothDamp(_smoothDistance, distance, ref _currentDistanceVelocity, distanceSmoothTime);

		// Вычисление вращения и позиции
		Vector3 targetPivotPosition = target.position + Vector3.up * targetHeightOffset;
		Quaternion rotation = Quaternion.Euler(_smoothY, _smoothX, 0); // Y - вертикальный угол, X - горизонтальный
		Vector3 direction = rotation * Vector3.forward;
		Vector3 desiredPosition = targetPivotPosition - direction * _smoothDistance;

		// Применение сглаженной позиции и вращения
		transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentPositionVelocity, positionSmoothTime);
		transform.rotation = rotation;

		// Альтернатива: можно заставить камеру всегда смотреть на точку фокуса
		// transform.LookAt(targetPivotPosition);
		// Однако установка rotation напрямую дает больше контроля над орбитальным вращением
	}

	// Вспомогательная функция для ограничения угла, т.к. углы Эйлера "заворачиваются"
	private float ClampAngle(float angle, float min, float max)
	{
		// Пример простой нормализации для вертикального угла, обычно этого достаточно
		// if (angle < -360f) angle += 360f;
		// if (angle > 360f) angle -= 360f;
		return Mathf.Clamp(angle, min, max);
	}

	// Позволяет сменить цель камеры из другого скрипта
	public void SetTarget(Transform newTarget)
	{
		target = newTarget;
		if (target == null)
		{
			Debug.LogError("Новая цель для камеры не назначена (null)!");
			enabled = false;
		} else {
			enabled = true;
		}
	}
	
	public object CaptureState()
	{
		CameraSaveData data = new CameraSaveData();
		data.currentX = _currentX;
		data.currentY = _currentY;
		data.distance = distance;
		Debug.Log($"Camera [{gameObject.name}] Capturing State: X={data.currentX}, Y={data.currentY}, Dist={data.distance}"); // <-- Добавь лог

		return data;
	}

	public void RestoreState(object state)
	{
		if (state is CameraSaveData data)
		{
			_currentX = data.currentX;
			_currentY = data.currentY;
			distance = data.distance;

			_smoothX = _currentX;
			_smoothY = _currentY;
			_smoothDistance = distance;
			
			Debug.Log($"Camera [{gameObject.name}] RestoreState called with data: X={data.currentX}, Y={data.currentY}, Dist={data.distance}"); // <-- Добавь лог


		}
		else
		{
			Debug.LogWarning($"[{gameObject.name}] Camera RestoreState received invalid data type: {state?.GetType()}");
		}
	}
}