using UnityEngine;
using RPG.Crafting;
using RPG.Control;
using RPG.Movement;

namespace RPG.UI.Crafting
{
    public class ShowCraftingUI : InteractableObject
    {
        [SerializeField] CraftingRecipe craftingRecipe = null;

        [Header("Настройки поиска UI")]
        [SerializeField] string craftingUITag = "CraftingUI";
        [SerializeField] string craftingUIName = "CraftingWindow";

        PlayerController playerController;
        Mover characterMovement;
        GameObject[] craftingTables;

        private CraftingUI craftingItems;
        private GameObject craftingUI;

        private void Awake()
        {
            playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            characterMovement = playerController.GetComponent<Mover>();
            craftingTables = GameObject.FindGameObjectsWithTag("CraftingTable");
        }

        private void Start()
        {
            FindCraftingUIComponents();

            if (craftingUI != null)
            {
                craftingUI.SetActive(false);
            }
        }

        private void FindCraftingUIComponents()
        {
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

            CraftingUI foundCraftingUI = FindObjectOfType<CraftingUI>();
            if (foundCraftingUI != null)
            {
                craftingItems = foundCraftingUI;
                craftingUI = foundCraftingUI.transform.parent?.gameObject ?? foundCraftingUI.gameObject;
                Debug.Log("[ShowCraftingUI] UI найден по типу компонента CraftingUI");
                return;
            }

            Debug.LogWarning($"[ShowCraftingUI] Не удалось найти CraftingUI! Проверьте:" +
                $"\n- Есть '{craftingUITag}' на UI объекте" +
                $"\n- Есть '{craftingUIName}' UI объекта" +
                $"\n- Наличие компонента CraftingUI в сцене");
        }

        private void TryRefindUIComponents()
        {
            if (craftingItems == null || craftingUI == null)
            {
                Debug.Log("[ShowCraftingUI] Попытка найти UI компоненты...");
                FindCraftingUIComponents();
            }
        }

        private bool IsWithinDistance(PlayerController playerController)
        {
            foreach (GameObject craftingTable in craftingTables)
            {
                if (Vector3.Distance(playerController.transform.position, craftingTable.transform.position) < interactionDistance) return true;
            }

            return false;
        }

        private void Update()
        {
            if (!IsWithinDistance(playerController) && craftingUI != null && craftingUI.activeSelf)
            {
                craftingUI.SetActive(false);
            }
        }

        public override CursorType GetCursorType()
        {
            return CursorType.Harvesting;
        }

        protected override void OnInteract(PlayerController callingController)
        {
            TryRefindUIComponents();

            if (craftingItems == null || craftingUI == null)
            {
                Debug.LogError("[ShowCraftingUI] UI компоненты не найдены! Невозможно открыть интерфейс крафта.");
                return;
            }

            characterMovement.Cancel();
            craftingItems.SetupRecipes(craftingRecipe);
            craftingUI.SetActive(!craftingUI.activeSelf);
        }

        public void RefreshUIReferences()
        {
            FindCraftingUIComponents();
        }

        public bool HasValidUIReferences()
        {
            return craftingItems != null && craftingUI != null;
        }
    }
}