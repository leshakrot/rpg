using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RPG.Homestead;
using System.IO;
using TMPro;
using UnityEngine.UI;

namespace RPG.Homestead.Editor
{
    /// <summary>
    /// Editor utility to create and setup the homestead scene programmatically.
    /// </summary>
    public class HomesteadSceneSetup : EditorWindow
    {
        [MenuItem("Homestead/Setup Homestead Scene")]
        public static void SetupScene()
        {
            if (EditorUtility.DisplayDialog("Setup Homestead Scene",
                "This will create a new Homestead scene with all required components. Continue?",
                "Yes", "Cancel"))
            {
                CreateHomesteadScene();
            }
        }

        private static void CreateHomesteadScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Setup scene components
            SetupTerrain();
            SetupLighting();
            SetupEstateManager();
            SetupBuildingSlots();
            SetupUI();
            
            // Save scene
            string scenePath = "Assets/Internal Assets/Homestead/Scenes";
            if (!Directory.Exists(scenePath))
            {
                Directory.CreateDirectory(scenePath);
            }
            
            string fullPath = Path.Combine(scenePath, "Homestead.unity");
            EditorSceneManager.SaveScene(newScene, fullPath);
            
            Debug.Log($"Homestead scene created successfully at {fullPath}");
            EditorUtility.DisplayDialog("Success", "Homestead scene created successfully!", "OK");
        }

        private static void SetupTerrain()
        {
            // Create a simple ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10); // 100x100 units
            
            // Add a simple material
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.3f, 0.5f, 0.3f); // Green-ish
            ground.GetComponent<Renderer>().material = groundMat;
            
            Debug.Log("Ground plane created");
        }

        private static void SetupLighting()
        {
            // Find or create directional light
            Light dirLight = Object.FindObjectOfType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }
            
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            dirLight.intensity = 1.0f;
            dirLight.color = Color.white;
            
            // Setup ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.0f;
            
            Debug.Log("Lighting configured");
        }

        private static void SetupEstateManager()
        {
            // Create EstateManager GameObject
            GameObject estateManagerObj = new GameObject("EstateManager");
            EstateManager estateManager = estateManagerObj.AddComponent<EstateManager>();
            
            // Create Buildings parent
            GameObject buildingsParent = new GameObject("Buildings");
            buildingsParent.transform.SetParent(estateManagerObj.transform);
            
            // Use reflection to set the private serialized field
            var field = typeof(EstateManager).GetField("buildingsParent", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(estateManager, buildingsParent.transform);
            }
            
            // Set homestead identifier
            var identifierField = typeof(EstateManager).GetField("homesteadIdentifier", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (identifierField != null)
            {
                identifierField.SetValue(estateManager, "main_homestead");
            }
            
            EditorUtility.SetDirty(estateManager);
            
            Debug.Log("EstateManager created and configured");
        }

        private static void SetupBuildingSlots()
        {
            // Load building data assets
            string[] buildingGuids = AssetDatabase.FindAssets("t:BuildingData", new[] { "Assets/Internal Assets/Homestead/Buildings" });
            BuildingData[] allBuildings = new BuildingData[buildingGuids.Length];
            
            for (int i = 0; i < buildingGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(buildingGuids[i]);
                allBuildings[i] = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            }
            
            if (allBuildings.Length == 0)
            {
                Debug.LogWarning("No BuildingData assets found. Building slots will have empty allowed buildings. " +
                    "Run 'Tools > Homestead > Create Example Buildings' to create test buildings.");
            }
            
            // Create 5 building slots in a logical layout
            Vector3[] slotPositions = new Vector3[]
            {
                new Vector3(-20, 0, 20),   // Top-left
                new Vector3(0, 0, 20),     // Top-center
                new Vector3(20, 0, 20),    // Top-right
                new Vector3(-20, 0, 0),    // Middle-left
                new Vector3(20, 0, 0)      // Middle-right
            };
            
            for (int i = 0; i < 5; i++)
            {
                GameObject slotObj = new GameObject($"BuildingSlot_{i + 1}");
                slotObj.transform.position = slotPositions[i];
                
                BuildingSlot slot = slotObj.AddComponent<BuildingSlot>();
                
                // Set interaction radius using reflection
                var radiusField = typeof(BuildingSlot).GetField("interactionRadius", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (radiusField != null)
                {
                    radiusField.SetValue(slot, 3.0f);
                }
                
                // Assign allowed buildings based on slot
                if (allBuildings.Length > 0)
                {
                    var allowedField = typeof(BuildingSlot).GetField("allowedBuildings", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (allowedField != null)
                    {
                        // Slot 1-3: All buildings
                        // Slot 4-5: Only storage and workshop
                        if (i < 3)
                        {
                            allowedField.SetValue(slot, allBuildings);
                        }
                        else
                        {
                            // Filter for storage and workshop
                            var filtered = System.Array.FindAll(allBuildings, b => 
                                b != null && (b.BuildingId.Contains("storage") || b.BuildingId.Contains("workshop")));
                            allowedField.SetValue(slot, filtered.Length > 0 ? filtered : allBuildings);
                        }
                    }
                }
                
                EditorUtility.SetDirty(slot);
            }
            
            Debug.Log("5 building slots created");
        }

        private static void SetupUI()
        {
            // Create UI prefabs first
            CreateUIElementPrefabs();
            
            // Create ConstructionMenu prefab
            CreateConstructionMenuPrefab();
            
            // Add ConstructionMenu instance to scene
            string prefabPath = "Assets/Internal Assets/Homestead/Prefabs/UI/ConstructionMenu.prefab";
            GameObject menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (menuPrefab != null)
            {
                GameObject menuInstance = PrefabUtility.InstantiatePrefab(menuPrefab) as GameObject;
                menuInstance.name = "ConstructionMenuUI";
                Debug.Log("ConstructionMenu instance added to scene");
            }
            else
            {
                Debug.LogWarning("ConstructionMenu prefab not found, skipping scene instance");
            }
        }

        private static void CreateUIElementPrefabs()
        {
            string prefabPath = "Assets/Internal Assets/Homestead/Prefabs/UI";
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }
            
            // Create BuildingButton prefab
            CreateBuildingButtonPrefab(prefabPath);
            
            // Create RequirementItem prefab
            CreateRequirementItemPrefab(prefabPath);
            
            AssetDatabase.Refresh();
            Debug.Log("UI element prefabs created");
        }

        private static void CreateBuildingButtonPrefab(string basePath)
        {
            GameObject buttonObj = new GameObject("BuildingButton");
            
            // Add Image component (background)
            Image bgImage = buttonObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Create Icon child
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(buttonObj.transform);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(10, 0);
            iconRect.sizeDelta = new Vector2(40, 40);
            
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // Create Text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(60, 5);
            textRect.offsetMax = new Vector2(-5, -5);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Building Name";
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            
            // Set button size
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            if (buttonRect == null)
            {
                buttonRect = buttonObj.AddComponent<RectTransform>();
            }
            buttonRect.sizeDelta = new Vector2(200, 50);
            
            // Save as prefab
            string prefabPath = Path.Combine(basePath, "BuildingButton.prefab");
            PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
            Object.DestroyImmediate(buttonObj);
            
            Debug.Log($"BuildingButton prefab created at {prefabPath}");
        }

        private static void CreateRequirementItemPrefab(string basePath)
        {
            GameObject itemObj = new GameObject("RequirementItem");
            
            // Add RectTransform
            RectTransform rect = itemObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 30);
            
            // Add TextMeshProUGUI
            TextMeshProUGUI text = itemObj.AddComponent<TextMeshProUGUI>();
            text.text = "✓ Requirement Description";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            
            // Save as prefab
            string prefabPath = Path.Combine(basePath, "RequirementItem.prefab");
            PrefabUtility.SaveAsPrefabAsset(itemObj, prefabPath);
            Object.DestroyImmediate(itemObj);
            
            Debug.Log($"RequirementItem prefab created at {prefabPath}");
        }

        private static void CreateConstructionMenuPrefab()
        {
            string prefabPath = "Assets/Internal Assets/Homestead/Prefabs/UI";
            
            // Create Canvas
            GameObject canvasObj = new GameObject("ConstructionMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Add ConstructionMenuUI component
            ConstructionMenuUI menuUI = canvasObj.AddComponent<ConstructionMenuUI>();
            
            // Create Menu Panel
            GameObject menuPanel = CreateUIObject("MenuPanel", canvasObj.transform);
            Image menuPanelImage = menuPanel.AddComponent<Image>();
            menuPanelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            RectTransform menuPanelRect = menuPanel.GetComponent<RectTransform>();
            menuPanelRect.anchorMin = new Vector2(0.2f, 0.1f);
            menuPanelRect.anchorMax = new Vector2(0.8f, 0.9f);
            menuPanelRect.offsetMin = Vector2.zero;
            menuPanelRect.offsetMax = Vector2.zero;
            
            // Create Building List Section (Left side)
            GameObject listSection = CreateUIObject("BuildingListSection", menuPanel.transform);
            RectTransform listRect = listSection.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0);
            listRect.anchorMax = new Vector2(0.35f, 1);
            listRect.offsetMin = new Vector2(10, 10);
            listRect.offsetMax = new Vector2(-5, -10);
            
            // Create ScrollView for building list
            GameObject scrollView = CreateScrollView("BuildingListScrollView", listSection.transform);
            GameObject buildingListContainer = scrollView.transform.Find("Viewport/Content").gameObject;
            
            // Create Details Section (Right side)
            GameObject detailsSection = CreateUIObject("DetailsSection", menuPanel.transform);
            RectTransform detailsRect = detailsSection.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.35f, 0);
            detailsRect.anchorMax = new Vector2(1, 1);
            detailsRect.offsetMin = new Vector2(5, 10);
            detailsRect.offsetMax = new Vector2(-10, -10);
            
            // Create building details UI elements
            GameObject nameText = CreateTextObject("BuildingNameText", detailsSection.transform, "Building Name", 24);
            SetRectTransform(nameText, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -10), new Vector2(-10, -50));
            
            GameObject iconObj = CreateUIObject("BuildingIcon", detailsSection.transform);
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;
            SetRectTransform(iconObj, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -150), new Vector2(110, -50));
            
            GameObject levelText = CreateTextObject("BuildingLevelText", detailsSection.transform, "Level 1 / 3", 16);
            SetRectTransform(levelText, new Vector2(0, 1), new Vector2(1, 1), new Vector2(120, -70), new Vector2(-10, -90));
            
            GameObject descText = CreateTextObject("BuildingDescriptionText", detailsSection.transform, "Building description goes here...", 14);
            SetRectTransform(descText, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -150), new Vector2(-10, -200));
            
            // Create Requirements Section
            GameObject reqLabel = CreateTextObject("RequirementsLabel", detailsSection.transform, "Requirements:", 16);
            SetRectTransform(reqLabel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -220), new Vector2(-10, -240));
            
            GameObject reqScrollView = CreateScrollView("RequirementsScrollView", detailsSection.transform);
            SetRectTransform(reqScrollView, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 120), new Vector2(-10, -250));
            GameObject requirementsContainer = reqScrollView.transform.Find("Viewport/Content").gameObject;
            
            // Create Action Buttons
            GameObject buildButton = CreateButton("BuildButton", detailsSection.transform, "Build");
            SetRectTransform(buildButton, new Vector2(0, 0), new Vector2(0.32f, 0), new Vector2(10, 70), new Vector2(-5, 110));
            
            GameObject upgradeButton = CreateButton("UpgradeButton", detailsSection.transform, "Upgrade");
            SetRectTransform(upgradeButton, new Vector2(0.34f, 0), new Vector2(0.66f, 0), new Vector2(5, 70), new Vector2(-5, 110));
            
            GameObject demolishButton = CreateButton("DemolishButton", detailsSection.transform, "Demolish");
            SetRectTransform(demolishButton, new Vector2(0.68f, 0), new Vector2(1, 0), new Vector2(5, 70), new Vector2(-10, 110));
            
            GameObject closeButton = CreateButton("CloseButton", detailsSection.transform, "Close");
            SetRectTransform(closeButton, new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 10), new Vector2(-10, 60));
            
            // Create Confirmation Dialog
            GameObject confirmDialog = CreateUIObject("ConfirmationDialog", canvasObj.transform);
            Image confirmBg = confirmDialog.AddComponent<Image>();
            confirmBg.color = new Color(0, 0, 0, 0.8f);
            RectTransform confirmRect = confirmDialog.GetComponent<RectTransform>();
            confirmRect.anchorMin = Vector2.zero;
            confirmRect.anchorMax = Vector2.one;
            confirmRect.offsetMin = Vector2.zero;
            confirmRect.offsetMax = Vector2.zero;
            
            GameObject confirmPanel = CreateUIObject("ConfirmPanel", confirmDialog.transform);
            Image confirmPanelImg = confirmPanel.AddComponent<Image>();
            confirmPanelImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            SetRectTransform(confirmPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -100), new Vector2(200, 100));
            
            GameObject confirmText = CreateTextObject("ConfirmationText", confirmPanel.transform, "Are you sure?", 18);
            SetRectTransform(confirmText, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-10, -10));
            
            GameObject yesButton = CreateButton("YesButton", confirmPanel.transform, "Yes");
            SetRectTransform(yesButton, new Vector2(0, 0), new Vector2(0.48f, 0.4f), new Vector2(10, 10), new Vector2(-5, -10));
            
            GameObject noButton = CreateButton("NoButton", confirmPanel.transform, "No");
            SetRectTransform(noButton, new Vector2(0.52f, 0), new Vector2(1, 0.4f), new Vector2(5, 10), new Vector2(-10, -10));
            
            confirmDialog.SetActive(false);
            
            // Wire up references using reflection
            WireUpMenuUIReferences(menuUI, menuPanel, buildingListContainer, 
                nameText, descText, levelText, iconImage,
                requirementsContainer,
                buildButton, upgradeButton, demolishButton, closeButton,
                confirmDialog, confirmText, yesButton, noButton);
            
            // Save as prefab
            string fullPrefabPath = Path.Combine(prefabPath, "ConstructionMenu.prefab");
            PrefabUtility.SaveAsPrefabAsset(canvasObj, fullPrefabPath);
            Object.DestroyImmediate(canvasObj);
            
            Debug.Log($"ConstructionMenu prefab created at {fullPrefabPath}");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return obj;
        }

        private static GameObject CreateTextObject(string name, Transform parent, string text, int fontSize)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string text)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Button btn = obj.AddComponent<Button>();
            
            GameObject textObj = CreateTextObject("Text", obj.transform, text, 16);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            
            return obj;
        }

        private static GameObject CreateScrollView(string name, Transform parent)
        {
            GameObject scrollView = CreateUIObject(name, parent);
            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            
            GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);
            
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            
            return scrollView;
        }

        private static void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void WireUpMenuUIReferences(ConstructionMenuUI menuUI,
            GameObject menuPanel, GameObject buildingListContainer,
            GameObject nameText, GameObject descText, GameObject levelText, Image iconImage,
            GameObject requirementsContainer,
            GameObject buildButton, GameObject upgradeButton, GameObject demolishButton, GameObject closeButton,
            GameObject confirmDialog, GameObject confirmText, GameObject yesButton, GameObject noButton)
        {
            var type = typeof(ConstructionMenuUI);
            
            // Load prefabs
            GameObject buildingButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Internal Assets/Homestead/Prefabs/UI/BuildingButton.prefab");
            GameObject requirementItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Internal Assets/Homestead/Prefabs/UI/RequirementItem.prefab");
            
            // Set fields using reflection
            SetField(type, menuUI, "menuPanel", menuPanel);
            SetField(type, menuUI, "buildingListContainer", buildingListContainer.transform);
            SetField(type, menuUI, "buildingButtonPrefab", buildingButtonPrefab);
            
            SetField(type, menuUI, "buildingNameText", nameText.GetComponent<TextMeshProUGUI>());
            SetField(type, menuUI, "buildingDescriptionText", descText.GetComponent<TextMeshProUGUI>());
            SetField(type, menuUI, "buildingLevelText", levelText.GetComponent<TextMeshProUGUI>());
            SetField(type, menuUI, "buildingIconImage", iconImage);
            SetField(type, menuUI, "requirementsContainer", requirementsContainer.transform);
            SetField(type, menuUI, "requirementItemPrefab", requirementItemPrefab);
            
            SetField(type, menuUI, "buildButton", buildButton.GetComponent<Button>());
            SetField(type, menuUI, "upgradeButton", upgradeButton.GetComponent<Button>());
            SetField(type, menuUI, "demolishButton", demolishButton.GetComponent<Button>());
            SetField(type, menuUI, "closeButton", closeButton.GetComponent<Button>());
            
            SetField(type, menuUI, "confirmationDialog", confirmDialog);
            SetField(type, menuUI, "confirmationText", confirmText.GetComponent<TextMeshProUGUI>());
            SetField(type, menuUI, "confirmYesButton", yesButton.GetComponent<Button>());
            SetField(type, menuUI, "confirmNoButton", noButton.GetComponent<Button>());
            
            EditorUtility.SetDirty(menuUI);
        }

        private static void SetField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found on {type.Name}");
            }
        }
    }
}
