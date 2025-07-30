using UnityEngine;
using System.Collections.Generic;

namespace RPG.Harvesting
{
	using RPG.Control;
    /// <summary>
    /// Управляет VFX эффектами добычи
    /// </summary>
    public class HarvestingVFXManager : MonoBehaviour
    {
        [Header("Эффекты добычи")]
        [SerializeField] private GameObject[] woodHarvestingEffects;
        [SerializeField] private GameObject[] stoneHarvestingEffects;
        [SerializeField] private GameObject[] metalHarvestingEffects;
        [SerializeField] private GameObject[] herbHarvestingEffects;
        
        [Header("Эффекты инструментов")]
        [SerializeField] private GameObject[] axeEffects;
        [SerializeField] private GameObject[] pickaxeEffects;
        [SerializeField] private GameObject[] sickleEffects;
        [SerializeField] private GameObject[] handHarvestingEffects;
        
        [Header("Эффекты завершения")]
        [SerializeField] private GameObject resourceDepletedEffect;
        [SerializeField] private GameObject resourceCompleteEffect;
        
        [Header("Настройки")]
        [SerializeField] private float effectDuration = 3f;
        [SerializeField] private Vector3 effectOffset = new Vector3(0, 1f, 0);
        [SerializeField] private bool autoDestroyEffects = true;
        
        private Dictionary<HarvestingToolType, GameObject[]> toolEffects;
        private Dictionary<HarvestableResource, GameObject[]> resourceEffects;
        
        private void Awake()
        {
            InitializeEffectDictionaries();
        }
        
        private void Start()
        {
            // Подписываемся на события добычи
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted += OnHarvestingStarted;
                HarvestingManager.Instance.OnHarvestingStopped += OnHarvestingStopped;
                HarvestingManager.Instance.OnResourceHarvestedEvent += OnResourceHarvested;
                HarvestingManager.Instance.OnResourceDepletedEvent += OnResourceDepleted;
            }
        }
        
        private void OnDestroy()
        {
            // Отписываемся от событий
            if (HarvestingManager.Instance != null)
            {
                HarvestingManager.Instance.OnHarvestingStarted -= OnHarvestingStarted;
                HarvestingManager.Instance.OnHarvestingStopped -= OnHarvestingStopped;
                HarvestingManager.Instance.OnResourceHarvestedEvent -= OnResourceHarvested;
                HarvestingManager.Instance.OnResourceDepletedEvent -= OnResourceDepleted;
            }
        }
        
        private void InitializeEffectDictionaries()
        {
            // Инициализируем словарь эффектов инструментов
            toolEffects = new Dictionary<HarvestingToolType, GameObject[]>
            {
                { HarvestingToolType.Axe, axeEffects },
                { HarvestingToolType.Pickaxe, pickaxeEffects },
                { HarvestingToolType.Sickle, sickleEffects },
                { HarvestingToolType.None, handHarvestingEffects }
            };
            
            // Инициализируем словарь эффектов ресурсов
            resourceEffects = new Dictionary<HarvestableResource, GameObject[]>();
        }
        
        /// <summary>
        /// Создаёт эффект начала добычи
        /// </summary>
        public void CreateHarvestingStartEffect(Vector3 position, HarvestingToolType toolType)
        {
            if (toolEffects.ContainsKey(toolType) && toolEffects[toolType] != null && toolEffects[toolType].Length > 0)
            {
                GameObject effectPrefab = GetRandomEffect(toolEffects[toolType]);
                CreateEffect(effectPrefab, position);
            }
        }
        
        /// <summary>
        /// Создаёт эффект добычи ресурса
        /// </summary>
        public void CreateResourceHarvestEffect(Vector3 position, HarvestableResource resource)
        {
            GameObject[] effects = GetResourceEffects(resource);
            if (effects != null && effects.Length > 0)
            {
                GameObject effectPrefab = GetRandomEffect(effects);
                CreateEffect(effectPrefab, position);
            }
        }
        
        /// <summary>
        /// Создаёт эффект завершения добычи
        /// </summary>
        public void CreateHarvestingCompleteEffect(Vector3 position)
        {
            if (resourceCompleteEffect != null)
            {
                CreateEffect(resourceCompleteEffect, position);
            }
        }
        
        /// <summary>
        /// Создаёт эффект истощения ресурса
        /// </summary>
        public void CreateResourceDepletedEffect(Vector3 position)
        {
            if (resourceDepletedEffect != null)
            {
                CreateEffect(resourceDepletedEffect, position);
            }
        }
        
        /// <summary>
        /// Создаёт эффект в указанной позиции
        /// </summary>
        private void CreateEffect(GameObject effectPrefab, Vector3 position)
        {
            if (effectPrefab == null) return;
            
            Vector3 spawnPosition = position + effectOffset;
            GameObject effect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
            
            // Настраиваем эффект
            SetupEffect(effect);
            
            // Автоматически уничтожаем эффект
            if (autoDestroyEffects)
            {
                DestroyEffect(effect, effectDuration);
            }
        }
        
        /// <summary>
        /// Настраивает созданный эффект
        /// </summary>
        private void SetupEffect(GameObject effect)
        {
            // Запускаем ParticleSystem если есть
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Play();
            }
            
            // Запускаем AudioSource если есть
            AudioSource audioSource = effect.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
        
        /// <summary>
        /// Уничтожает эффект через указанное время
        /// </summary>
        private void DestroyEffect(GameObject effect, float delay)
        {
            if (effect != null)
            {
                Destroy(effect, delay);
            }
        }
        
        /// <summary>
        /// Получает случайный эффект из массива
        /// </summary>
        private GameObject GetRandomEffect(GameObject[] effects)
        {
            if (effects == null || effects.Length == 0) return null;
            return effects[Random.Range(0, effects.Length)];
        }
        
        /// <summary>
        /// Получает эффекты для типа ресурса
        /// </summary>
        private GameObject[] GetResourceEffects(HarvestableResource resource)
        {
            if (resource == null) return null;
            
            // Определяем тип ресурса по названию
            string resourceName = resource.ResourceName.ToLower();
            
            if (resourceName.Contains("wood") || resourceName.Contains("дерево") || resourceName.Contains("береза") || 
                resourceName.Contains("сосна") || resourceName.Contains("кедр"))
            {
                return woodHarvestingEffects;
            }
            else if (resourceName.Contains("stone") || resourceName.Contains("камень") || resourceName.Contains("руда"))
            {
                return stoneHarvestingEffects;
            }
            else if (resourceName.Contains("metal") || resourceName.Contains("металл") || resourceName.Contains("железо"))
            {
                return metalHarvestingEffects;
            }
            else if (resourceName.Contains("herb") || resourceName.Contains("трава") || resourceName.Contains("цветок"))
            {
                return herbHarvestingEffects;
            }
            
            // По умолчанию возвращаем эффекты дерева
            return woodHarvestingEffects;
        }
        
        // Обработчики событий
        private void OnHarvestingStarted(IHarvestable harvestable, PlayerController player)
        {
            var resource = harvestable.GetResource();
            if (resource != null)
            {
                var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();
                var bestTool = HarvestingToolChecker.FindBestTool(inventory, resource.RequiredToolType, resource.ResourceTier);
                var toolType = bestTool != null ? bestTool.ToolType : HarvestingToolType.None;
                
                // Получаем позицию из MonoBehaviour
                Vector3 position = (harvestable as MonoBehaviour)?.transform.position ?? Vector3.zero;
                CreateHarvestingStartEffect(position, toolType);
            }
        }
        
        private void OnHarvestingStopped()
        {
            // Можно добавить эффект остановки добычи
        }
        
        private void OnResourceHarvested(HarvestableResource resource, int amount)
        {
            // Эффект добычи ресурса будет создаваться в HarvestableSource
        }
        
        private void OnResourceDepleted()
        {
            // Эффект истощения ресурса будет создаваться в HarvestableSource
        }
    }
} 