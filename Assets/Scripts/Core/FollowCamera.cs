using UnityEngine;

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

        private Transform prevHit; // Предыдущий объект, который стал прозрачным
        private Renderer prevRenderer; // Кэшированный Renderer предыдущего объекта
        private Color originalColor; // Исходный цвет материала
        private int layerMaskValue; // Значение битовой маски для слоев

        private void Start()
        {
            // Преобразуем LayerMask в целочисленное значение для быстрого сравнения
            layerMaskValue = layersToTransparent.value;
        }

        private void Update()
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