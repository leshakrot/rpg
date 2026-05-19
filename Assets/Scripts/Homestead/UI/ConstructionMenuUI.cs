using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RPG.Control;
using System.Collections.Generic;

namespace RPG.Homestead
{
    public class ConstructionMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Transform buildingListContainer;
        [SerializeField] private GameObject buildingButtonPrefab;
        
        [Header("Details")]
        [SerializeField] private TextMeshProUGUI buildingNameText;
        [SerializeField] private TextMeshProUGUI buildingDescriptionText;
        [SerializeField] private TextMeshProUGUI buildingLevelText;
        [SerializeField] private Image buildingIconImage;
        [SerializeField] private Transform requirementsContainer;
        [SerializeField] private GameObject requirementItemPrefab;
        
        [Header("Actions")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button demolishButton;
        [SerializeField] private Button closeButton;
        
        [Header("Confirmation")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI confirmationText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        
        private BuildingSlot currentSlot;
        private BuildingData selectedBuilding;
        private System.Action pendingAction;
        
        private void Awake()
        {
            buildButton.onClick.AddListener(OnBuildClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            demolishButton.onClick.AddListener(OnDemolishClicked);
            closeButton.onClick.AddListener(Close);
            confirmYesButton.onClick.AddListener(OnConfirmYes);
            confirmNoButton.onClick.AddListener(OnConfirmNo);
            
            menuPanel.SetActive(false);
            confirmationDialog.SetActive(false);
        }
        
        public void Open(BuildingSlot slot, PlayerController player)
        {
            currentSlot = slot;
            menuPanel.SetActive(true);
            Time.timeScale = 0f;
            
            PopulateBuildingList();
            
            if (slot.IsOccupied())
            {
                SelectBuilding(slot.GetCurrentBuilding().buildingData);
            }
            else if (slot.GetAllowedBuildings().Length > 0)
            {
                SelectBuilding(slot.GetAllowedBuildings()[0]);
            }
        }
        
        public void Close()
        {
            menuPanel.SetActive(false);
            Time.timeScale = 1f;
            currentSlot = null;
            selectedBuilding = null;
        }
        
        private void PopulateBuildingList()
        {
            foreach (Transform child in buildingListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (currentSlot == null) return;
            
            foreach (var buildingData in currentSlot.GetAllowedBuildings())
            {
                if (buildingData == null) continue;
                
                GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingListContainer);
                var textComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (textComponent != null)
                {
                    string buttonText = buildingData.DisplayName;
                    if (EstateManager.Instance.IsBuildingUnlocked(buildingData.BuildingId))
                    {
                        buttonText += " [FREE]";
                    }
                    textComponent.text = buttonText;
                }
                
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    BuildingData capturedData = buildingData;
                    button.onClick.AddListener(() => SelectBuilding(capturedData));
                }
            }
        }
        
        private void SelectBuilding(BuildingData buildingData)
        {
            selectedBuilding = buildingData;
            UpdateDetails();
            UpdateButtons();
        }
        
        private void UpdateDetails()
        {
            if (selectedBuilding == null) return;
            
            buildingNameText.text = selectedBuilding.DisplayName;
            buildingDescriptionText.text = selectedBuilding.Description;
            
            if (buildingIconImage != null && selectedBuilding.Icon != null)
            {
                buildingIconImage.sprite = selectedBuilding.Icon;
                buildingIconImage.enabled = true;
            }
            else if (buildingIconImage != null)
            {
                buildingIconImage.enabled = false;
            }
            
            if (currentSlot.IsOccupied() && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding)
            {
                int currentLevel = currentSlot.GetCurrentBuilding().currentLevel;
                buildingLevelText.text = $"Level {currentLevel} / {selectedBuilding.MaxLevel}";
            }
            else
            {
                buildingLevelText.text = "Not Built";
            }
            
            UpdateRequirements();
        }
        
        private void UpdateRequirements()
        {
            foreach (Transform child in requirementsContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (selectedBuilding == null) return;
            
            int targetLevel = 1;
            bool isUpgrade = false;
            
            if (currentSlot.IsOccupied() && currentSlot.GetCurrentBuilding().buildingData == selectedBuilding)
            {
                targetLevel = currentSlot.GetCurrentBuilding().currentLevel + 1;
                isUpgrade = true;
                
                if (targetLevel > selectedBuilding.MaxLevel)
                {
                    CreateRequirementItem("✓ Max Level Reached", Color.green);
                    return;
                }
            }
            
            BuildingRequirement[] requirements = selectedBuilding.GetRequirementsForLevel(targetLevel);
            bool isFreeReconstruction = !isUpgrade && EstateManager.Instance.IsBuildingUnlocked(selectedBuilding.BuildingId);
            
            if (isFreeReconstruction)
            {
                CreateRequirementItem("✓ FREE RECONSTRUCTION", Color.cyan);
            }
            
            if (requirements.Length == 0)
            {
                CreateRequirementItem("✓ No requirements", Color.green);
            }
            else
            {
                foreach (var requirement in requirements)
                {
                    if (requirement == null) continue;
                    
                    bool met = requirement.Validate(isFreeReconstruction);
                    string prefix = met ? "✓" : "✗";
                    string text = $"{prefix} {requirement.GetDescription()}";
                    
                    if (!met)
                    {
                        text += $"\n  {requirement.GetMissingInfo()}";
                    }
                    
                    CreateRequirementItem(text, met ? Color.green : Color.red);
                }
            }
        }
        
        private void CreateRequirementItem(string text, Color color)
        {
            GameObject item = Instantiate(requirementItemPrefab, requirementsContainer);
            var textComponent = item.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
            }
        }
        
        private void UpdateButtons()
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
            
            buildButton.gameObject.SetActive(!isOccupied || !isSameBuilding);
            upgradeButton.gameObject.SetActive(isSameBuilding && currentSlot.GetCurrentBuilding().CanUpgrade());
            demolishButton.gameObject.SetActive(isOccupied);
            
            if (buildButton.gameObject.activeSelf)
            {
                bool isFree = EstateManager.Instance.IsBuildingUnlocked(selectedBuilding.BuildingId);
                RequirementValidator validator = new RequirementValidator();
                var result = validator.ValidateRequirements(selectedBuilding.GetRequirementsForLevel(1), isFree);
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
        
        private void OnBuildClicked()
        {
            if (currentSlot.IsOccupied())
            {
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
                if (EstateManager.Instance.ConstructBuilding(currentSlot, selectedBuilding))
                {
                    Close();
                }
            }
        }
        
        private void OnUpgradeClicked()
        {
            if (EstateManager.Instance.UpgradeBuilding(currentSlot))
            {
                UpdateDetails();
                UpdateButtons();
            }
        }
        
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
        
        private void ShowConfirmation(string message, System.Action onConfirm)
        {
            confirmationText.text = message;
            confirmationDialog.SetActive(true);
            pendingAction = onConfirm;
        }
        
        private void OnConfirmYes()
        {
            confirmationDialog.SetActive(false);
            pendingAction?.Invoke();
            pendingAction = null;
        }
        
        private void OnConfirmNo()
        {
            confirmationDialog.SetActive(false);
            pendingAction = null;
        }
    }
}
