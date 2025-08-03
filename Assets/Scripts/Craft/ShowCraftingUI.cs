using UnityEngine;
using RPG.Crafting;
using RPG.Control;
using RPG.Movement;

namespace RPG.UI.Crafting
{
    public class ShowCraftingUI : MonoBehaviour, IRaycastable
    {
        [SerializeField] CraftingRecipe craftingRecipe = null;
        [SerializeField] float minimumCraftingDistance = 2.5f;

        [Header("Настройки поиска UI")]
        [SerializeField] string craftingUITag = "CraftingUI"; // Тег для поиска UI
        [SerializeField] string craftingUIName = "CraftingWindow"; // Имя GameObject'а с UI

        PlayerController playerController;
        Mover characterMovement;
        GameObject[] craftingTables;

        // Кэшированные ссылки (находятся автоматически)
        private CraftingUI craftingItems;
        private GameObject craftingUI;

        private void Awake()
        {
            // Find the player gameobject using the tag "Player", and get its PlayerController component.
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

            // Find the CharacterMovement component from the playerController.
            characterMovement = playerController.GetComponent<Mover>();

            // Find all game objects with the tag "CraftingTable".
            craftingTables = GameObject.FindGameObjectsWithTag("CraftingTable");
        }

        private void Start()
        {
            // Находим UI компоненты при старте
            FindCraftingUIComponents();

            // Disable the crafting UI gameobject if found.
            if (craftingUI != null)
            {
                craftingUI.SetActive(false);
            }
        }

        /// <summary>
        /// Автоматически находит компоненты CraftingUI в сцене.
        /// </summary>
        private void FindCraftingUIComponents()
        {
            // Способ 1: Поиск по тегу
            if (!string.IsNullOrEmpty(craftingUITag))
            {
                GameObject uiObject = GameObject.FindGameObjectWithTag(craftingUITag);
                if (uiObject != null)
                {
                    craftingUI = uiObject;
                    craftingItems = uiObject.GetComponentInChildren<CraftingUI>();

                    if (craftingItems != null)
                    {
                        Debug.Log($"[ShowCraftingUI] UI найден по тегу: {craftingUITag}");
                        return;
                    }
                }
            }

            // Способ 2: Поиск по имени
            if (!string.IsNullOrEmpty(craftingUIName))
            {
                GameObject uiObject = GameObject.Find(craftingUIName);
                if (uiObject != null)
                {
                    craftingUI = uiObject;
                    craftingItems = uiObject.GetComponentInChildren<CraftingUI>();

                    if (craftingItems != null)
                    {
                        Debug.Log($"[ShowCraftingUI] UI найден по имени: {craftingUIName}");
                        return;
                    }
                }
            }

            // Способ 3: Поиск по типу компонента (если другие способы не сработали)
            CraftingUI foundCraftingUI = FindObjectOfType<CraftingUI>();
            if (foundCraftingUI != null)
            {
                craftingItems = foundCraftingUI;
                craftingUI = foundCraftingUI.transform.parent?.gameObject ?? foundCraftingUI.gameObject;
                Debug.Log("[ShowCraftingUI] UI найден по типу компонента CraftingUI");
                return;
            }

            // Если ничего не найдено
            Debug.LogWarning($"[ShowCraftingUI] Не удалось найти CraftingUI! Проверьте:" +
                $"\n- Тег '{craftingUITag}' на UI объекте" +
                $"\n- Имя '{craftingUIName}' UI объекта" +
                $"\n- Наличие компонента CraftingUI в сцене");
        }

        /// <summary>
        /// Повторная попытка найти UI компоненты (вызывается при взаимодействии).
        /// </summary>
        private void TryRefindUIComponents()
        {
            if (craftingItems == null || craftingUI == null)
            {
                Debug.Log("[ShowCraftingUI] Повторный поиск UI компонентов...");
                FindCraftingUIComponents();
            }
        }

        private bool IsWithinDistance(PlayerController playerController)
        {
            // Iterate through the craftingTables GameObjects array.
            foreach (GameObject craftingTable in craftingTables)
            {
                // Using Vector3.Distance check the distance between the player and the current iteration.
                // We use the cached PlayerController component to get the position of the gameobject.
                // Return true if yes.
                if (Vector3.Distance(playerController.transform.position, craftingTable.transform.position) < minimumCraftingDistance) return true;
            }

            // Return false by default.
            return false;
        }

        private void Update()
        {
            // Check if IsWithinDistance returns false.
            if (!IsWithinDistance(playerController) && craftingUI != null && craftingUI.activeSelf)
            {
                // Disable the crafting UI gameobject.
                craftingUI.SetActive(false);
            }
        }

        public CursorType GetCursorType()
        {
            // Return the desired CursorType enum member.
            return CursorType.Harvesting;
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            // Check if the player clicked the left mouse button.
            if (Input.GetMouseButtonDown(0))
            {
                // Пробуем найти UI компоненты, если они не найдены
                TryRefindUIComponents();

                // Проверяем, что UI компоненты найдены
                if (craftingItems == null || craftingUI == null)
                {
                    Debug.LogError("[ShowCraftingUI] UI компоненты не найдены! Невозможно открыть интерфейс крафта.");
                    return true;
                }

                // If the left mouse button was clicked, check if the player is not within the minimum distance to the crafting table.
                if (!IsWithinDistance(callingController))
                {
                    // If outside minimum distance:
                    // Move player character to the crafting table position.
                    characterMovement.MoveTo(transform.position, 1f);
                }
                else
                {
                    // If within minimum distance:
                    // Cancel character movement using the navmesh isStopped property. 
                    characterMovement.Cancel();

                    // Setup the recipes in the crafting UI.
                    // The SetupRecipes function simply assigns craftingRecipe to the local variable in CraftingUI component (craftingItems).
                    // We don't do this in Awake or Start for example, because there might be more than one crafting tables that offer different recipes.
                    craftingItems.SetupRecipes(craftingRecipe);

                    // Enable the crafting UI gameobject.
                    craftingUI.SetActive(!craftingUI.activeSelf);
                }
            }
            return true;
        }

        /// <summary>
        /// Публичный метод для принудительного обновления ссылок на UI (можно вызвать извне).
        /// </summary>
        public void RefreshUIReferences()
        {
            FindCraftingUIComponents();
        }

        /// <summary>
        /// Проверяет, найдены ли UI компоненты.
        /// </summary>
        public bool HasValidUIReferences()
        {
            return craftingItems != null && craftingUI != null;
        }
    }
}