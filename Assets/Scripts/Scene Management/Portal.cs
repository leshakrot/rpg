using System.Collections;
using GameDevTV.Saving;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using RPG.Control;
using RPG.UI;
using UnityEngine.UI;

namespace RPG.SceneManagement
{
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
            ForestPath_ForestPathCave,
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

        [Header("Location Image")]
        [Tooltip("Картинка локации, в которую ведёт этот портал — показывается на Fader во время перехода")]
        [SerializeField] private Sprite destinationImage;

        private bool isPlayerInRange = false;

        public void ToggleAvailability(bool b) { isAvailable = b; }

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
                Debug.LogError("Portal: Scene to load not set.");
                yield break;
            }

            // Корутина должна пережить смену сцены.
            DontDestroyOnLoad(gameObject);

            Fader fader = FindObjectOfType<Fader>();
            SavingWrapper savingWrapper = FindObjectOfType<SavingWrapper>();
            GameObject player = GameObject.FindWithTag("Player");

            if (fader == null || savingWrapper == null || player == null)
            {
                Debug.LogError("Portal: Не найден Fader, SavingWrapper или Player до перехода.");
                yield break;
            }

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null) playerController.enabled = false;

            fader.SetLocationImage(destinationImage);
            yield return fader.FadeOut(fadeOutTime);

            // Сохраняем состояние старой сцены до её уничтожения.
            savingWrapper.Save();

            yield return SceneManager.LoadSceneAsync(sceneToLoad);

            // ВАЖНО: SavingWrapper старой сцены мог быть уничтожен.
            // Поэтому после LoadSceneAsync обязательно получаем новый.
            savingWrapper = FindObjectOfType<SavingWrapper>();

            if (savingWrapper == null)
            {
                Debug.LogError("Portal: SavingWrapper не найден после загрузки новой сцены.");
                Destroy(gameObject);
                yield break;
            }

            GameObject newPlayer = GameObject.FindWithTag("Player");

            if (newPlayer == null)
            {
                Debug.LogError("Portal: Player не найден после загрузки новой сцены.");
                Destroy(gameObject);
                yield break;
            }

            PlayerController newPlayerController = newPlayer.GetComponent<PlayerController>();
            if (newPlayerController != null) newPlayerController.enabled = false;

            // Восстанавливаем сохранённое состояние в объектах новой сцены.
            savingWrapper.Load();

            Portal otherPortal = GetOtherPortal();
            if (otherPortal == null)
            {
                Debug.LogError($"Portal: Не найден портал назначения для '{destination}'.");
            }
            else
            {
                UpdatePlayer(otherPortal);
            }

            yield return new WaitForSeconds(fadeWaitTime);

            UIManager.RefreshUIFromAnywhere();

            yield return fader.FadeIn(fadeInTime);

            if (newPlayerController != null) newPlayerController.enabled = true;

            // Сохраняем уже корректное состояние новой сцены.
            savingWrapper.Save();

            Destroy(gameObject);
        }

        private void UpdatePlayer(Portal otherPortal)
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player == null)
            {
                Debug.LogError("Portal: Player не найден при UpdatePlayer().");
                return;
            }

            if (otherPortal == null || otherPortal.spawnPoint == null)
            {
                Debug.LogError("Portal: Портал назначения или его spawnPoint не задан.");
                return;
            }

            NavMeshAgent agent = player.GetComponent<NavMeshAgent>();

            if (agent != null) agent.enabled = false;

            player.transform.position = otherPortal.spawnPoint.position;
            player.transform.rotation = otherPortal.spawnPoint.rotation;

            if (agent != null) agent.enabled = true;
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
                Debug.LogError("Portal: Неверный тип данных при восстановлении состояния портала.");
            }
        }
    }
}
