using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Control;
using System.Collections.Generic;

namespace RPG.Homestead
{
    /// <summary>
    /// UI for browsing and constructing buildings.
    /// Displays building information, requirements, and action buttons.
    /// </summary>
    public class ConstructionMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform buildingListContainer;
        [SerializeField] private GameObject buildingButtonPrefab;
        
        [Header("Building Details Panel")]
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI buildingDescriptionText;
        [SerializeField] private TextMeshProUGUI buildingLevelText;
        [SerializeField] private Image buildingIconImage;
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private GameObject requirementItemPrefab;
        
        [Header("Action Buttons")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button demolishButton;
        [SerializeField] private Button closeButton;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        private BuildingSlot currentSlot;
        private PlayerController currentPlayer;
        private BuildingData selectedBuilding;
        private System.Action pendingAction;
        
        private void Awake()
        {
            // Setup button listeners
            buildButton.onClick.AddListener(OnBuildClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            demolishButton.onClick.AddListener(OnDemolishClicked);
            closeButton.onClick.AddListener(Close);
            
            confirmYesButton.onClick.AddListener(OnConfirmYes);
            confirmNoButton.onClick.AddListener(OnConfirmNo);
            
            // Hide menu initially
            menuPanel.SetActive(false);
            confirmationDialog.SetActive(false);
        }
        
        /// <summary>
        /// Open the construction menu for a specific building slot.
        /// </summary>
        public void Open(BuildingSlot slot, PlayerController player)
        {
            currentSlot = slot;
            currentPlayer = player;
            
            menuPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game
            
            PopulateBuildingList();
            
            // Select first building or current building
            if (slot.IsOccupied())
            {
                SelectBuilding(slot.GetCurrentBuilding().buildingData);
            }
            else if (slot.GetAllowedBuildings().Length > 0)
            {
                SelectBuilding(slot.GetAllowedBuildings()[0]);
            }
        }
        
        /// <summary>
        /// Close the construction menu and resume game.
        /// </summary>
        public void Close()
        {
            menuPanel.SetActive(false);
            Time.timeScale = 1f; // Resume game
            
            currentSlot = null;
            currentPlayer = null;
            selectedBuilding = null;
        }
        
        /// <summary>
        /// Populate the building list with buttons for all allowed buildings.
        /// </summary>
        private void PopulateBuildingList()
        {
            // Clear existing buttons
            foreach (Transform child in buildingListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (currentSlot == null) return;
            
            BuildingData[] allowedBuildings = currentSlot.GetAllowedBuildings();
            
            foreach (var buildingData in allowedBuildings)
            {
                if (buildingData == null) continue;
                
                // Instantiate button
                GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingListContainer);
                
                // Setup button text
                var textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    string buttonText = buildingData.DisplayName;
                    
                    // Add [FREE] indicator for unlocked buildings
                    if (EstateManager.Instance.IsBuildingUnlocked(buildingData.BuildingId))
                    {
                        buttonText += " [FREE]";
                    }
                    
                    textComponent.text = buttonText;
                }
                
                // Setup button icon
                var imageComponent = buttonObj.GetComponentInChildren<Image>();
                if (imageComponent != null && buildingData.Icon != null)
                {
                    imageComponent.sprite = buildingData.Icon;
                }
                
                // Wire button click
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    BuildingData capturedData = buildingData; // Capture for lambda
                    button.onClick.AddListener(() => SelectBuilding(capturedData));
                }
            }
        }
        
        /// <summary>
        /// Select a building to display its details.
        /// </summary>
        private void SelectBuilding(BuildingData buildingData)
        {
            selectedBuilding = buildingData;
            UpdateBuildingDetails();
            UpdateActionButtons();
        }
        
        /// <summary>
        /// Update the building details panel with selected building information.
        /// </summary>
        private void UpdateBuildingDetails()
        {
            if (selectedBuilding == null) return;
            
            // Update name
            if (buildingNameText != null)
            {
                buildingNameText.text = selectedBuilding.DisplayName;
            }
            
            // Update description
            if (buildingDescriptionText != null)
            {
                buildingDescriptionText.text = selectedBuilding.Description;
            }
            
            // Update icon
            if (buildingIconImage != null && selectedBuilding.Icon != null)
            {
                buildingIconImage.sprite = selectedBuilding.Icon;
                buildingIconImage.enabled = true;
            }
            else if (buildingIconImage != null)
            {
                buildingIconImage.enabled = false;
            }
            
            // Update level text
            if (buildingLevelText != null)
            {
                if (currentSlot.IsOccupied() && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding)
                {
                    int currentLevel = currentSlot.GetCurrentBuilding().currentLevel;
                    buildingLevelText.text = $"Level {currentLevel} / {selectedBuilding.MaxLevel}";
                }
                else
                {
                    buildingLevelText.text = "Not Built";
                }
            }
            
            // Update requirements list
            UpdateRequirementsList();
        }
        
        /// <summary>
        /// Update the requirements list display.
        /// </summary>
        private void UpdateRequirementsList()
        {
            // Clear existing requirement items
            foreach (Transform child in requirementsContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (selectedBuilding == null) return;
            
            // Determine target level
            int targetLevel = 1;
            bool isUpgrade = false;
            
            if (currentSlot.IsOccupied() && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding)
            {
                // This is an upgrade
                targetLevel = currentSlot.GetCurrentBuilding().currentLevel + 1;
                isUpgrade = true;
                
                // If at max level, show no requirements
                if (targetLevel > selectedBuilding.MaxLevel)
                {
                    GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.text = "✓ Max Level Reached";
                        text.color = Color.green;
                    }
                    return;
                }
            }
            
            // Get requirements for target level
            BuildingRequirement[] requirements = selectedBuilding.GetRequirementsForLevel(targetLevel);
            
            // Check if this is free reconstruction
            bool isFreeReconstruction = !isUpgrade && EstateManager.Instance.IsBuildingUnlocked(selectedBuilding.BuildingId);
            
            // Display free reconstruction indicator
            if (isFreeReconstruction)
            {
                GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "✓ FREE RECONSTRUCTION (Building Unlocked)";
                    text.color = Color.cyan;
                }
            }
            
            // Validate and display requirements
            RequirementValidator validator = new RequirementValidator();
            var result = validator.ValidateRequirements(requirements, isFreeReconstruction);
            
            if (requirements.Length == 0)
            {
                GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                var text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = "✓ No requirements";
                    text.color = Color.green;
                }
            }
            else
            {
                foreach (var requirement in requirements)
                {
                    if (requirement == null) continue;
                    
                    GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
                    var text = item.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (text != null)
                    {
                        bool met = requirement.Validate(isFreeReconstruction);
                        string prefix = met ? "✓" : "✗";
                        text.text = $"{prefix} {requirement.GetDescription()}";
                        text.color = met ? Color.green : Color.red;
                        
                        // Add detailed info on hover (could use tooltip system)
                        if (!met)
                        {
                            text.text += $"\n  {requirement.GetMissingInfo()}";
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Update the action buttons based on current state.
        /// </summary>
        private void UpdateActionButtons()
        {
            if (selectedBuilding == null)
            {
                buildButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(false);
                demolishButton.gameObject.SetActive(false);
                return;
            }
            
            bool isOccupied = currentSlot.IsOccupied();
            bool isSameBuilding = isOccupied && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding;
            
            // Build button: show if slot is empty or different building
            buildButton.gameObject.SetActive(!isOccupied || !isSameBuilding);
            
            // Upgrade button: show if same building and not max level
            bool canUpgrade = isSameBuilding && currentSlot.GetCurrentBuilding().CanUpgrade();
            upgradeButton.gameObject.SetActive(canUpgrade);
            
            // Demolish button: show if slot is occupied
            demolishButton.gameObject.SetActive(isOccupied);
            
            // Enable/disable based on requirements
            if (buildButton.gameObject.activeSelf)
            {
                bool isFreeReconstruction = EstateManager.Instance.IsBuildingUnlocked(selectedBuilding.BuildingId);
                RequirementValidator validator = new RequirementValidator();
                var result = validator.ValidateRequirements(selectedBuilding.GetRequirementsForLevel(1), isFreeReconstruction);
                buildButton.interactable = result.IsValid;
            }
            
            if (upgradeButton.gameObject.activeSelf)
            {
                int nextLevel = currentSlot.GetCurrentBuilding().currentLevel + 1;
                RequirementValidator validator = new RequirementValidator();
                var result = validator.ValidateRequirements(selectedBuilding.GetRequirementsForLevel(nextLevel), false);
                upgradeButton.interactable = result.IsValid;
            }
        }
        
        /// <summary>
        /// Handle Build button click.
        /// </summary>
        private void OnBuildClicked()
        {
            if (currentSlot.IsOccupied())
            {
                // Need to demolish first
                ShowConfirmation(
                    $"Demolish {currentSlot.GetCurrentBuilding().buildingData.DisplayName} and build {selectedBuilding.DisplayName}?",
                    () => {
                        EstateManager.Instance.DemolishBuilding(currentSlot);
                        EstateManager.Instance.ConstructBuilding(currentSlot, selectedBuilding);
                        Close();
                    }
                );
            }
            else
            {
                bool success = EstateManager.Instance.ConstructBuilding(currentSlot, selectedBuilding);
                if (success)
                {
                    Close();
                }
                else
                {
                    Debug.LogWarning("Construction failed");
                }
            }
        }
        
        /// <summary>
        /// Handle Upgrade button click.
        /// </summary>
        private void OnUpgradeClicked()
        {
            bool success = EstateManager.Instance.UpgradeBuilding(currentSlot);
            if (success)
            {
                // Refresh UI to show new level
                UpdateBuildingDetails();
                UpdateActionButtons();
            }
            else
            {
                Debug.LogWarning("Upgrade failed");
            }
        }
        
        /// <summary>
        /// Handle Demolish button click.
        /// </summary>
        private void OnDemolishClicked()
        {
            ShowConfirmation(
                $"Demolish {currentSlot.GetCurrentBuilding().buildingData.DisplayName}? (No refund, but can rebuild for free)",
                () => {
                    EstateManager.Instance.DemolishBuilding(currentSlot);
                    Close();
                }
            );
        }
        
        /// <summary>
        /// Show confirmation dialog with custom message and action.
        /// </summary>
        private void ShowConfirmation(string message, System.Action onConfirm)
        {
            confirmationText.text = message;
            confirmationDialog.SetActive(true);
            pendingAction = onConfirm;
        }
        
        /// <summary>
        /// Handle confirmation Yes button click.
        /// </summary>
        private void OnConfirmYes()
        {
            confirmationDialog.SetActive(false);
            pendingAction?.Invoke();
            pendingAction = null;
        }
        
        /// <summary>
        /// Handle confirmation No button click.
        /// </summary>
        private void OnConfirmNo()
        {
            confirmationDialog.SetActive(false);
            pendingAction = null;
        }
    }
}
