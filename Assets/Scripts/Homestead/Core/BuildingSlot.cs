using UnityEngine;
using RPG.Control;

namespace RPG.Homestead
{
    /// <summary>
    /// Represents a location where buildings can be constructed.
    /// Placed manually in the homestead scene by designers.
    /// </summary>
    public class BuildingSlot : MonoBehaviour, IRaycastable
    {
        [Header("Slot Configuration")]
        [SerializeField] private string slotId; // Unique identifier for this slot
        [SerializeField] private BuildingData[] allowedBuildings; // Buildings that can be constructed here
        [SerializeField] private string slotCategory = "General"; // For UI organization
        
        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 3f;
        [SerializeField] private string interactionPrompt = "Open Construction Menu";
        
        private BuildingInstance currentBuilding;
        
        private void Awake()
        {
            // Generate unique ID if not set
            if (string.IsNullOrEmpty(slotId))
            {
                slotId = System.Guid.NewGuid().ToString();
            }
            
            // Register with EstateManager
            if (EstateManager.Instance != null)
            {
                EstateManager.Instance.RegisterSlot(this);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from EstateManager
            if (EstateManager.Instance != null)
            {
                EstateManager.Instance.UnregisterSlot(this);
            }
        }
        
        /// <summary>
        /// Get the unique identifier for this slot.
        /// </summary>
        public string GetSlotId() => slotId;
        
        /// <summary>
        /// Get the category of this slot for UI organization.
        /// </summary>
        public string GetSlotCategory() => slotCategory;
        
        /// <summary>
        /// Get the array of buildings allowed on this slot.
        /// </summary>
        public BuildingData[] GetAllowedBuildings() => allowedBuildings;
        
        /// <summary>
        /// Get the current building instance on this slot.
        /// Returns null if slot is empty.
        /// </summary>
        public BuildingInstance GetCurrentBuilding() => currentBuilding;
        
        /// <summary>
        /// Check if this slot currently has a building on it.
        /// </summary>
        public bool IsOccupied() => currentBuilding != null;
        
        /// <summary>
        /// Set the current building instance on this slot.
        /// Called by EstateManager during construction/demolition.
        /// </summary>
        public void SetCurrentBuilding(BuildingInstance building)
        {
            currentBuilding = building;
        }
        
        // IRaycastable implementation for player interaction
        
        /// <summary>
        /// Get the cursor type to display when hovering over this slot.
        /// </summary>
        public CursorType GetCursorType()
        {
            return CursorType.Dialogue; // Using Dialogue cursor for interaction
        }
        
        /// <summary>
        /// Handle player raycast interaction.
        /// Opens the Construction Menu when player clicks on the slot.
        /// </summary>
        public bool HandleRaycast(PlayerController callingController)
        {
            if (Vector3.Distance(callingController.transform.position, transform.position) > interactionRadius)
            {
                return false;
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                OpenConstructionMenu(callingController);
            }
            
            return true;
        }
        
        private void OpenConstructionMenu(PlayerController player)
        {
            var menu = FindObjectOfType<ConstructionMenuUI>();
            if (menu != null)
            {
                menu.Open(this, player);
            }
            else
            {
                Debug.LogError("BuildingSlot: ConstructionMenuUI not found in scene");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Visualize interaction radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
            
            // Draw slot ID label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"Slot: {slotId}\nCategory: {slotCategory}");
            #endif
        }
        
        private void OnValidate()
        {
            // Validate allowed buildings
            if (allowedBuildings != null)
            {
                foreach (var building in allowedBuildings)
                {
                    if (building == null)
                    {
                        Debug.LogWarning($"BuildingSlot {slotId}: Null building reference in allowed buildings list");
                    }
                }
            }
        }
    }
}
