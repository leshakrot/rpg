using GameDevTV.Inventories;
using UnityEngine;

namespace RPG.Control
{
    [RequireComponent(typeof(Pickup))]
    public class ClickablePickup : MonoBehaviour, IRaycastable
    {
        [Tooltip("Distance at which the label appears")]
        [SerializeField] private float _detectionRadius = 2.5f;

        [Tooltip("Optional: assign your own label prefab (Canvas WorldSpace with PickupLabelUI component).\n" +
                 "Leave empty to use the built-in fallback label.")]
        [SerializeField] private PickupLabelUI _labelPrefab = null;

        // ── Private state ─────────────────────────────────────────────────────
        private Pickup        _pickup;
        private PickupLabelUI _labelInstance;
        private Transform     _player;
        private bool          _labelVisible = false;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _pickup = GetComponent<Pickup>();
        }

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) _player = playerGO.transform;

            _labelInstance = SpawnLabel();
            UpdateLabelText();
            _labelInstance.Hide();
        }

        private void Update()
        {
            if (_player == null) return;

            bool shouldShow = Vector3.Distance(transform.position, _player.position) <= _detectionRadius;

            if (shouldShow && !_labelVisible)
            {
                UpdateLabelText();
                _labelInstance.Show();
                _labelVisible = true;
            }
            else if (!shouldShow && _labelVisible)
            {
                _labelInstance.Hide();
                _labelVisible = false;
            }
        }

        private void OnDestroy()
        {
            if (_labelInstance != null)
                Destroy(_labelInstance.gameObject);
        }

        // ── IRaycastable ──────────────────────────────────────────────────────

        /// <summary>
        /// Called every frame by PlayerController when the mouse ray hits this collider.
        /// Returns true so PlayerController knows to use our cursor and stop checking others.
        /// We deliberately do NOT pick up on click here — the UI button handles that.
        /// </summary>
        public bool HandleRaycast(PlayerController callingController)
        {
            // Just return true so the cursor gets set — pickup happens via the UI label button
            return true;
        }

        public CursorType GetCursorType()
        {
            return _pickup.CanBePickedUp() ? CursorType.Pickup : CursorType.FullPickup;
        }

        // ── Label callback ────────────────────────────────────────────────────

        /// <summary>Called by the UI Button inside the label prefab.</summary>
        public void OnLabelClicked()
        {
            _pickup.PickupItem();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private PickupLabelUI SpawnLabel()
        {
            if (_labelPrefab != null)
            {
                var instance = Instantiate(_labelPrefab);
                instance.transform.SetParent(null);
                instance.Setup(transform, this);
                return instance;
            }

            return PickupLabelUI.CreateProcedural(transform, this);
        }

        private void UpdateLabelText()
        {
            if (_labelInstance == null) return;
            var item = _pickup.GetItem();
            if (item == null) return;
            _labelInstance.SetText(item.GetDisplayName());
        }
    }
}
