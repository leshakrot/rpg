using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RPG.Control
{
    /// <summary>
    /// Floating world-space UI label above a pickup.
    /// 
    /// Usage A — Prefab (recommended):
    ///   Create your own Canvas prefab in WorldSpace mode, add this component to its root.
    ///   In the prefab, assign LabelText and LabelButton in the inspector.
    ///   ClickablePickup will Instantiate it and call Setup().
    ///
    /// Usage B — Procedural fallback:
    ///   If no prefab is assigned in ClickablePickup, a plain dark label is built at runtime.
    /// </summary>
    public class PickupLabelUI : MonoBehaviour
    {
        [Header("Wire up in your prefab")]
        [Tooltip("Text component that will display the item name")]
        [SerializeField] private Text _labelText;

        [Tooltip("Button the player clicks to pick up the item")]
        [SerializeField] private Button _labelButton;

        // ── Internal state ────────────────────────────────────────────────────
        private Transform _target;
        private Camera    _mainCamera;

        private static readonly Vector3 WorldOffset      = new Vector3(0f, 1.8f, 0f);
        private const           float   LabelWorldWidth  = 1.6f;
        private const           float   LabelWorldHeight = 0.25f;

        // ── Called by ClickablePickup after Instantiate ───────────────────────

        /// <summary>
        /// Initialises the label. Called once right after instantiation.
        /// </summary>
        public void Setup(Transform target, ClickablePickup owner)
        {
            _target     = target;
            _mainCamera = Camera.main;

            EnsureEventSystem();
            EnsureGraphicRaycaster();

            if (_labelButton != null)
                _labelButton.onClick.AddListener(owner.OnLabelClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetText(string text, Color? color = null)
        {
            if (_labelText == null) return;
            _labelText.text = text;
            if (color.HasValue) _labelText.color = color.Value;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            if (_target == null) { Destroy(gameObject); return; }

            transform.position = _target.position + WorldOffset;

            if (_mainCamera != null)
            {
                transform.LookAt(
                    transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up
                );
            }
        }

        // ── Procedural build (fallback) ───────────────────────────────────────

        /// <summary>
        /// Builds the label entirely in code. Called only when no prefab is supplied.
        /// </summary>
        public static PickupLabelUI CreateProcedural(Transform target, ClickablePickup owner)
        {
            var go = new GameObject("PickupLabel_" + target.name);
            go.transform.SetParent(null);

            var label = go.AddComponent<PickupLabelUI>();

            // Canvas
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            const float pixelW = 200f, pixelH = 40f;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta  = new Vector2(pixelW, pixelH);
            canvasRect.localScale = new Vector3(LabelWorldWidth  / pixelW,
                                                LabelWorldHeight / pixelH,
                                                LabelWorldWidth  / pixelW);

            // Button background
            var btnGO   = new GameObject("Button");
            btnGO.transform.SetParent(go.transform, false);
            var btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = btnRect.offsetMax = Vector2.zero;

            var image   = btnGO.AddComponent<Image>();
            image.color = new Color(0.05f, 0.05f, 0.05f, 0.80f);

            label._labelButton               = btnGO.AddComponent<Button>();
            var colors                       = ColorBlock.defaultColorBlock;
            colors.highlightedColor          = new Color(1f, 0.85f, 0.25f, 1f);
            colors.pressedColor              = new Color(0.7f, 0.6f, 0.1f, 1f);
            label._labelButton.colors        = colors;
            label._labelButton.targetGraphic = image;

            // Text
            var textGO   = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f,  2f);
            textRect.offsetMax = new Vector2(-6f, -2f);

            label._labelText                      = textGO.AddComponent<Text>();
            label._labelText.alignment            = TextAnchor.MiddleCenter;
            label._labelText.color                = Color.white;
            label._labelText.fontSize             = 18;
            label._labelText.resizeTextForBestFit = true;
            label._labelText.resizeTextMinSize    = 8;
            label._labelText.resizeTextMaxSize    = 20;
            label._labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var outline            = textGO.AddComponent<Outline>();
            outline.effectColor    = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            label.Setup(target, owner);
            return label;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void EnsureGraphicRaycaster()
        {
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Debug.LogWarning("[PickupLabelUI] EventSystem not found — created one automatically.");
        }
    }
}
