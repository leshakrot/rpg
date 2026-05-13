using UnityEngine;
using UnityEditor;
using RPG.Homestead;
using System.IO;

namespace RPG.Homestead.Editor
{
    /// <summary>
    /// Editor utility to create the HomesteadTeleportItem asset.
    /// Menu: Homestead → Create Teleport Item Asset
    /// </summary>
    public class CreateHomesteadTeleportAsset
    {
        [MenuItem("Homestead/Create Teleport Item Asset")]
        public static void CreateAsset()
        {
            if (EditorUtility.DisplayDialog(
                "Create Homestead Teleport Item",
                "This will create the HomesteadTeleport.asset file at:\n\n" +
                "Assets/Internal Assets/Homestead/HomesteadTeleport.asset\n\n" +
                "Continue?",
                "Yes", "Cancel"))
            {
                CreateTeleportItemAsset();
            }
        }

        private static void CreateTeleportItemAsset()
        {
            string assetPath = "Assets/Internal Assets/Homestead/HomesteadTeleport.asset";
            
            // Check if asset already exists
            if (File.Exists(assetPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Asset Already Exists",
                    "HomesteadTeleport.asset already exists. Overwrite?",
                    "Yes", "Cancel"))
                {
                    return;
                }
            }
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.Log($"Created directory: {directory}");
            }
            
            // Create the ScriptableObject instance
            HomesteadTeleportItem teleportItem = ScriptableObject.CreateInstance<HomesteadTeleportItem>();
            
            // Set the fields using reflection since they're private
            var type = typeof(HomesteadTeleportItem);
            
            // Set homesteadSceneName
            var homesteadSceneField = type.GetField("homesteadSceneName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (homesteadSceneField != null)
            {
                homesteadSceneField.SetValue(teleportItem, "Homestead");
                Debug.Log("Set homesteadSceneName to: Homestead");
            }
            
            // Set cooldownSeconds
            var cooldownField = type.GetField("cooldownSeconds", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cooldownField != null)
            {
                cooldownField.SetValue(teleportItem, 5.0f);
                Debug.Log("Set cooldownSeconds to: 5.0");
            }
            
            // Set base InventoryItem fields using reflection
            var baseType = typeof(GameDevTV.Inventories.InventoryItem);
            
            // Set displayName
            var displayNameField = baseType.GetField("displayName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (displayNameField != null)
            {
                displayNameField.SetValue(teleportItem, "Homestead Teleport");
                Debug.Log("Set displayName to: Homestead Teleport");
            }
            
            // Set description
            var descriptionField = baseType.GetField("description", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (descriptionField != null)
            {
                descriptionField.SetValue(teleportItem, "A magical item that teleports you to your homestead. Use it to quickly travel home and return to your previous location.");
                Debug.Log("Set description");
            }
            
            // Set stackable to false (not consumable, not stackable)
            var stackableField = baseType.GetField("stackable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stackableField != null)
            {
                stackableField.SetValue(teleportItem, false);
                Debug.Log("Set stackable to: false");
            }
            
            // Set cannotBeDropped to true (important item)
            var cannotBeDroppedField = baseType.GetField("cannotBeDropped", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cannotBeDroppedField != null)
            {
                cannotBeDroppedField.SetValue(teleportItem, true);
                Debug.Log("Set cannotBeDropped to: true");
            }
            
            // Set price to 0 (quest reward item, not for sale)
            var priceField = baseType.GetField("price", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (priceField != null)
            {
                priceField.SetValue(teleportItem, 0f);
                Debug.Log("Set price to: 0");
            }
            
            // Note: Icon is left null - can be set manually in Inspector if desired
            
            // Create the asset
            AssetDatabase.CreateAsset(teleportItem, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"<color=green>Successfully created HomesteadTeleport asset at: {assetPath}</color>");
            
            // Select the asset in the Project window
            Selection.activeObject = teleportItem;
            EditorGUIUtility.PingObject(teleportItem);
            
            EditorUtility.DisplayDialog(
                "Success",
                "HomesteadTeleport.asset has been created successfully!\n\n" +
                "Location: " + assetPath + "\n\n" +
                "The asset is now selected in the Project window.\n\n" +
                "Configuration:\n" +
                "- Scene Name: Homestead\n" +
                "- Cooldown: 5 seconds\n" +
                "- Display Name: Homestead Teleport\n" +
                "- Cannot be dropped: Yes\n\n" +
                "You can optionally set an icon in the Inspector.",
                "OK");
        }
    }
}
