using System;
using System.Collections;
using RPG.Control;
using GameDevTV.Saving;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TMPro;
using RPG.UI;

namespace RPG.SceneManagement
{
	using UnityEngine.UI;
    public class Portal : MonoBehaviour, ISaveable
    {
        enum DestinationIdentifier
        {
            Pond_PondPath, 
            Pond_PondCave,
	        PondPath_MainTown,
	        MainTown_PlayerHouse1,
            MainTown_Tavern,
            MainTownTavern_Cellar,
            MainTownTavernCellar_Cave, 
            MainTown_ForestPath,
            ForestPath_Forest,
            MainTown_MainTownCavePath,
            MainTownCavePath_CellarCave,
            E,   
        }

        [SerializeField] int sceneToLoad = -1;
        [SerializeField] Transform spawnPoint;
        [SerializeField] DestinationIdentifier destination;
        [SerializeField] float fadeOutTime = 1f;
        [SerializeField] float fadeInTime = 2f;
        [SerializeField] float fadeWaitTime = 0.5f;

        [SerializeField] bool isAvailable = true;

        [Header("Interaction")]
        [SerializeField] private InteractButton interactButton;
        [SerializeField] private Sprite interactIcon;
	    [SerializeField] private string interactText = "Переход";

        private bool isPlayerInRange = false;

        public void ToggleAvailability(bool b)
        {
            isAvailable = b;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAvailable) return;
            if (other.CompareTag("Player") && interactButton != null)
            {
                isPlayerInRange = true;
                interactButton.SetIcon(interactIcon);
                interactButton.SetInteractionText(interactText);
                interactButton.gameObject.SetActive(true);

                Button button = interactButton.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnInteractButtonClicked);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && interactButton != null)
            {
                isPlayerInRange = false;
                interactButton.gameObject.SetActive(false);
            }
        }

        private void OnInteractButtonClicked()
        {
            if (isPlayerInRange && isAvailable)
            {
                StartCoroutine(Transition());
            }
        }

        private IEnumerator Transition()
        {
            if (sceneToLoad < 0)
            {
                Debug.LogError("Scene to load not set.");
                yield break;
            }

            DontDestroyOnLoad(gameObject);

            Fader fader = FindObjectOfType<Fader>();
            SavingWrapper savingWrapper = FindObjectOfType<SavingWrapper>();
            PlayerController playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            playerController.enabled = false;

            yield return fader.FadeOut(fadeOutTime);

            savingWrapper.Save();

            yield return SceneManager.LoadSceneAsync(sceneToLoad);
            PlayerController newPlayerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
            newPlayerController.enabled = false;

            savingWrapper.Load();

            Portal otherPortal = GetOtherPortal();
            UpdatePlayer(otherPortal);

            yield return new WaitForSeconds(fadeWaitTime);

            // Обновляем UI после перехода между сценами
            UIManager.RefreshUIFromAnywhere();

            fader.FadeIn(fadeInTime);

            newPlayerController.enabled = true;

            // Сохраняем ПОСЛЕ fadeWaitTime — все Start() уже отработали,
            // RestoreState применён, состояние объектов корректное
            savingWrapper.Save();

            Destroy(gameObject);
        }

        private void UpdatePlayer(Portal otherPortal)
        {
            GameObject player = GameObject.FindWithTag("Player");
            player.GetComponent<NavMeshAgent>().enabled = false;
            player.transform.position = otherPortal.spawnPoint.position;
            player.transform.rotation = otherPortal.spawnPoint.rotation;
            player.GetComponent<NavMeshAgent>().enabled = true;
        }

        private Portal GetOtherPortal()
        {
            foreach (Portal portal in FindObjectsOfType<Portal>())
            {
                if (portal == this) continue;
                if (portal.destination != destination) continue;

                return portal;
            }

            return null;
        }

        private void OnDisable()
        {
            if (interactButton != null)
            {
                interactButton.gameObject.SetActive(false);
            }
        }

        public object CaptureState()
        {
            return isAvailable;
        }

        public void RestoreState(object state)
        {
            if (state is bool)
            {
                isAvailable = (bool)state;
            }
            else
            {
                Debug.LogError("Неверный тип данных при восстановлении состояния портала.");
            }
        }
    }
}
