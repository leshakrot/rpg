using UnityEngine;
using GameDevTV.Inventories;
using UnityEngine.SceneManagement;
using System.Collections;
using RPG.SceneManagement;

namespace RPG.Homestead
{
    /// <summary>
    /// Special inventory item that teleports player to homestead.
    /// Not consumed on use, supports cooldown to prevent spam.
    /// Saves return location for later teleport back.
    /// </summary>
    [CreateAssetMenu(fileName = "Homestead Teleport", menuName = "Homestead/Teleport Item", order = 0)]
    public class HomesteadTeleportItem : ActionItem
    {
        [Header("Teleport Configuration")]
        [SerializeField] private string homesteadSceneName = "Homestead";
        [SerializeField] private float cooldownSeconds = 5f;
        
        private static float lastUseTime = -999f;
        private const string ReturnSceneKey = "HomesteadReturnScene";
        
        /// <summary>
        /// Use the teleport item to travel to the homestead.
        /// Checks cooldown, saves return location, and loads homestead scene.
        /// </summary>
        /// <param name="user">The player GameObject using this item</param>
        /// <returns>True if teleport initiated successfully, false if on cooldown</returns>
        public override bool Use(GameObject user)
        {
            // Check cooldown
            if (Time.time - lastUseTime < cooldownSeconds)
            {
                float remaining = cooldownSeconds - (Time.time - lastUseTime);
                Debug.Log($"Teleport on cooldown. Wait {remaining:F1} seconds.");
                return false;
            }
            
            // Save current location for return teleport
            SaveReturnLocation();
            
            // Find SavingWrapper and Fader for proper scene transition
            SavingWrapper savingWrapper = Object.FindObjectOfType<SavingWrapper>();
            Fader fader = Object.FindObjectOfType<Fader>();
            
            if (savingWrapper != null && user != null)
            {
                // Start coroutine on a persistent GameObject
                MonoBehaviour coroutineRunner = savingWrapper;
                coroutineRunner.StartCoroutine(TeleportToHomestead(user, savingWrapper, fader));
            }
            else
            {
                // Fallback: direct scene load without fade/save
                Debug.LogWarning("SavingWrapper not found. Loading scene directly without save.");
                SceneManager.LoadSceneAsync(homesteadSceneName);
            }
            
            lastUseTime = Time.time;
            return true;
        }
        
        /// <summary>
        /// Coroutine to handle the teleport transition with fade and save.
        /// Follows the pattern from Portal.cs for proper scene transitions.
        /// </summary>
        private IEnumerator TeleportToHomestead(GameObject user, SavingWrapper savingWrapper, Fader fader)
        {
            // Disable player control during transition
            var playerController = user.GetComponent<RPG.Control.PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Fade out
            if (fader != null)
            {
                yield return fader.FadeOut(1f);
            }
            
            // Save current state
            savingWrapper.Save();
            
            // Load homestead scene
            yield return SceneManager.LoadSceneAsync(homesteadSceneName);
            
            // Load saved state in new scene
            savingWrapper.Load();
            
            // Wait a moment for scene initialization
            yield return new WaitForSeconds(0.5f);
            
            // Re-enable player control
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var newPlayerController = player.GetComponent<RPG.Control.PlayerController>();
                if (newPlayerController != null)
                {
                    newPlayerController.enabled = true;
                }
            }
            
            // Fade in
            if (fader != null)
            {
                fader.FadeIn(2f);
            }
            
            // Save after transition completes
            savingWrapper.Save();
        }
        
        /// <summary>
        /// Saves the current scene name to PlayerPrefs for return teleport.
        /// </summary>
        private void SaveReturnLocation()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString(ReturnSceneKey, currentScene);
            PlayerPrefs.Save();
            Debug.Log($"Saved return location: {currentScene}");
        }
        
        /// <summary>
        /// Static method to return player to the previously saved location.
        /// Can be called from homestead to return to the world.
        /// </summary>
        public static void ReturnToPreviousLocation()
        {
            string returnScene = PlayerPrefs.GetString(ReturnSceneKey, "");
            if (!string.IsNullOrEmpty(returnScene))
            {
                Debug.Log($"Returning to: {returnScene}");
                
                // Find SavingWrapper and Fader for proper transition
                SavingWrapper savingWrapper = Object.FindObjectOfType<SavingWrapper>();
                Fader fader = Object.FindObjectOfType<Fader>();
                
                if (savingWrapper != null)
                {
                    savingWrapper.StartCoroutine(ReturnTransition(returnScene, savingWrapper, fader));
                }
                else
                {
                    // Fallback: direct scene load
                    SceneManager.LoadSceneAsync(returnScene);
                }
            }
            else
            {
                Debug.LogWarning("No return location saved. Cannot teleport back.");
            }
        }
        
        /// <summary>
        /// Coroutine to handle the return transition with fade and save.
        /// </summary>
        private static IEnumerator ReturnTransition(string returnScene, SavingWrapper savingWrapper, Fader fader)
        {
            // Disable player control
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var playerController = player.GetComponent<RPG.Control.PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }
            }
            
            // Fade out
            if (fader != null)
            {
                yield return fader.FadeOut(1f);
            }
            
            // Save current state
            savingWrapper.Save();
            
            // Load return scene
            yield return SceneManager.LoadSceneAsync(returnScene);
            
            // Load saved state
            savingWrapper.Load();
            
            // Wait for initialization
            yield return new WaitForSeconds(0.5f);
            
            // Re-enable player control
            player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var newPlayerController = player.GetComponent<RPG.Control.PlayerController>();
                if (newPlayerController != null)
                {
                    newPlayerController.enabled = true;
                }
            }
            
            // Fade in
            if (fader != null)
            {
                fader.FadeIn(2f);
            }
            
            // Save after transition
            savingWrapper.Save();
        }
    }
}
