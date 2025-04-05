using UnityEngine;
using Cinemachine;

namespace RPG.Core
{
    public class FollowCamera : MonoBehaviour
    {
        [Tooltip("Цель, за которой следует камера")]
        public Transform target;

        [Tooltip("Слои, которые должны становиться прозрачными при пересечении с рейкастом")]
        public LayerMask layersToTransparent;

        [Tooltip("Включить/выключить эффект прозрачности")]
        [SerializeField] private bool fadeObjects;

        [Tooltip("Уровень прозрачности (0 - полностью прозрачный, 1 - полностью непрозрачный)")]
        [Range(0f, 1f)]
        [SerializeField] private float alpha = 0.3f;

        [Tooltip("Скорость вращения камеры по горизонтали")]
        [SerializeField] private float rotationSpeed = 5f;

        [Tooltip("Минимальный угол наклона камеры (в градусах)")]
        [SerializeField] private float minVerticalAngle = -45f;

        [Tooltip("Максимальный угол наклона камеры (в градусах)")]
        [SerializeField] private float maxVerticalAngle = 45f;

        private CinemachineFreeLook freeLookCamera;
        private Transform prevHit; // Предыдущий объект, который стал прозрачным
        private Renderer prevRenderer; // Кэшированный Renderer предыдущего объекта
        private Color originalColor; // Исходный цвет материала
        private int layerMaskValue; // Значение битовой маски для слоев

        private void Start()
        {
            if (target == null)
            {
                Debug.LogError("Target is not assigned! Please assign a target in the Inspector.");
                return;
            }

            // Преобразуем LayerMask в целочисленное значение для быстрого сравнения
            layerMaskValue = layersToTransparent.value;

            // Находим компонент CinemachineFreeLook на камере
            freeLookCamera = GetComponent<CinemachineBrain>()?.ActiveVirtualCamera as CinemachineFreeLook;
            if (freeLookCamera == null)
            {
                Debug.LogError("CinemachineFreeLook camera not found! Please add a CinemachineFreeLook component to your camera.");
            }
        }

        private void Update()
        {
            HandleMouseOrbit();
            HandleTransparency();
        }

        private void HandleMouseOrbit()
        {
            if (freeLookCamera == null || !Input.GetMouseButton(1)) // Проверяем, зажата ли ПКМ
                return;

            // Получаем входные данные от мыши
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            // Обновляем углы поворота Cinemachine FreeLook
            freeLookCamera.m_YAxis.Value -= mouseY * Time.deltaTime; // Вертикальный угол
            freeLookCamera.m_XAxis.Value += mouseX * Time.deltaTime; // Горизонтальный угол

            // Ограничиваем вертикальный угол
            freeLookCamera.m_YAxis.Value = Mathf.Clamp(freeLookCamera.m_YAxis.Value, minVerticalAngle / 90f, maxVerticalAngle / 90f);
        }

        private void HandleTransparency()
        {
            if (!fadeObjects || target == null)
                return;

            // Создаем луч от камеры к цели
            Ray ray = new Ray(transform.position, (target.position - transform.position).normalized);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMaskValue))
            {
                Transform objectHit = hit.transform;

                // Проверяем, принадлежит ли объект к нужным слоям
                if (((1 << objectHit.gameObject.layer) & layerMaskValue) != 0)
                {
                    // Если объект отличается от предыдущего
                    if (prevHit != objectHit)
                    {
                        RestorePreviousObject();

                        // Сохраняем новый объект и его Renderer
                        prevHit = objectHit;
                        prevRenderer = objectHit.GetComponent<Renderer>();

                        if (prevRenderer != null && prevRenderer.sharedMaterial != null)
                        {
                            // Кэшируем исходный цвет
                            originalColor = prevRenderer.sharedMaterial.color;
                            prevRenderer.material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                        }
                    }
                }
                else
                {
                    RestorePreviousObject();
                }
            }
            else
            {
                RestorePreviousObject();
            }
        }

        private void RestorePreviousObject()
        {
            if (prevRenderer != null)
            {
                // Восстанавливаем исходный цвет
                prevRenderer.material.color = originalColor;

                // Очищаем ссылки
                prevHit = null;
                prevRenderer = null;
            }
        }

        private void OnDisable()
        {
            // При отключении скрипта восстанавливаем все объекты
            RestorePreviousObject();
        }
    }
}