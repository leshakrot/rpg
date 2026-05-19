using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace RPG.Control
{
    /// <summary>
    /// Singleton-менеджер для управления подсветкой интерактивных объектов.
    /// Отслеживает объект под курсором и управляет его подсветкой.
    /// </summary>
    public class HighlightManager : MonoBehaviour
    {
        private static HighlightManager instance;
        public static HighlightManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("HighlightManager");
                    instance = go.AddComponent<HighlightManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private HashSet<InteractableHighlight> registeredHighlights = new HashSet<InteractableHighlight>();
        private InteractableHighlight currentHighlightedObject = null;

        [SerializeField] private float raycastRadius = 1f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            // Не подсвечиваем, если курсор над UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (currentHighlightedObject != null)
                {
                    currentHighlightedObject.DisableHighlight();
                    currentHighlightedObject = null;
                }
                return;
            }

            InteractableHighlight targetHighlight = GetHighlightUnderCursor();

            if (targetHighlight != currentHighlightedObject)
            {
                if (currentHighlightedObject != null)
                {
                    currentHighlightedObject.DisableHighlight();
                }

                currentHighlightedObject = targetHighlight;

                if (currentHighlightedObject != null)
                {
                    currentHighlightedObject.EnableHighlight();
                }
            }
        }

        private InteractableHighlight GetHighlightUnderCursor()
        {
            if (Camera.main == null) return null;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.SphereCastAll(ray, raycastRadius);

            // Сортируем по расстоянию
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // Сначала проверяем сам объект
                InteractableHighlight highlight = hit.transform.GetComponent<InteractableHighlight>();
                
                // Если не нашли, проверяем родителя
                if (highlight == null)
                {
                    highlight = hit.transform.GetComponentInParent<InteractableHighlight>();
                }

                // Проверяем, что компонент зарегистрирован и активен
                if (highlight != null && highlight.enabled && registeredHighlights.Contains(highlight))
                {
                    // Дополнительно проверяем наличие IRaycastable
                    IRaycastable raycastable = highlight.GetComponent<IRaycastable>();
                    if (raycastable != null)
                    {
                        return highlight;
                    }
                }
            }

            return null;
        }

        public void Register(InteractableHighlight highlight)
        {
            registeredHighlights.Add(highlight);
        }

        public void Unregister(InteractableHighlight highlight)
        {
            registeredHighlights.Remove(highlight);
            
            if (currentHighlightedObject == highlight)
            {
                currentHighlightedObject.DisableHighlight();
                currentHighlightedObject = null;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
