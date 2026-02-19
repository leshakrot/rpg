using System;
using System.Collections;
using UnityEngine;
using RPG.Control;
using RPG.Stats;
using GameDevTV.Inventories;
using RPG.UI.RequirementText;

namespace RPG.Harvesting
{
	using GameDevTV.Saving;
	using System.Collections.Generic;
	public abstract class HarvestableSource : MonoBehaviour, IHarvestable, IRaycastable, ISaveable
	{
		protected HarvestingToolVisualizer toolVisualizer = null;
		protected HarvestingTool currentDisplayedTool = null;
    	
        [Header("Ресурс")]
        [SerializeField] protected HarvestableResource resource = null;
        
        [Header("Настройки")]
        [SerializeField] protected float respawnTime = 300f; // 5 минут
        [SerializeField] protected bool startDepleted = false;
		[SerializeField] protected float harvestDistance = 2f; // Расстояние для добычи
		[SerializeField] protected bool isOneTimeResource = false;
        
        [Header("Визуализация")]
        [SerializeField] protected GameObject activeModel = null;
        [SerializeField] protected GameObject depletedModel = null;
        
        // Состояние
        protected bool isHarvesting = false;
        protected bool isDepleted = false;
        protected float currentProgress = 1f;
        protected int remainingResources = 0;
        protected PlayerController currentPlayer = null;
        protected Coroutine harvestCoroutine = null;
        protected Coroutine respawnCoroutine = null;
        
        // События
        public event Action<float> OnHarvestProgress;
        public event Action<HarvestableResource, int> OnHarvestComplete;
        public event Action OnResourceDepleted;
        
        // Свойства
        public bool IsHarvesting => isHarvesting;
        public bool IsDepeleted => isDepleted;
        public HarvestableResource GetResource() => resource;
        public int GetRemainingResources() => remainingResources;
        
        protected virtual void Awake()
        {
            if (resource != null)
            {
                remainingResources = resource.ResourceAmount;
            }
            
            if (startDepleted)
            {
                SetDepleted(true);
            }
        }

        protected virtual void OnEnable()
        {
            Core.InteractableRegistry.Instance.Register(this);
        }

        protected virtual void OnDisable()
        {
            if (Core.InteractableRegistry.Instance != null)
            {
                Core.InteractableRegistry.Instance.Unregister(this);
            }
        }

        protected virtual void Start()
        {
            UpdateVisuals();

            // ИСПРАВЛЕНИЕ: Инициализируем прогресс только если не добываем
            if (!isHarvesting)
            {
                currentProgress = (float)remainingResources / resource.ResourceAmount;
                OnHarvestProgress?.Invoke(currentProgress);
            }
        }

        public virtual bool CanStartHarvesting(int playerLevel, GameDevTV.Inventories.Inventory inventory = null)
        {
            if (resource == null)
            {
                Debug.Log("CanStartHarvesting: resource is null");
                return false;
            }
            
            if (isDepleted)
            {
                Debug.Log("CanStartHarvesting: ресурс истощён");
                return false;
            }
            
            if (isHarvesting)
            {
                Debug.Log("CanStartHarvesting: добыча уже идёт");
                return false;
            }
            
            // Если инвентарь предоставлен, проверяем инструменты
            if (inventory != null)
            {
                bool canHarvest = resource.CanHarvest(playerLevel, inventory);
                Debug.Log($"CanStartHarvesting с инвентарём: {canHarvest}");
                return canHarvest;
            }
            
            // Обратная совместимость - проверяем только уровень
            bool canHarvestLevel = resource.CanHarvest(playerLevel);
            Debug.Log($"CanStartHarvesting только по уровню: {canHarvestLevel}");
            return canHarvestLevel;
        }
        
        // Перегрузка для обратной совместимости
        public virtual bool CanStartHarvesting(int playerLevel)
        {
            return CanStartHarvesting(playerLevel, null);
        }

        public virtual void StartHarvesting(PlayerController player)
        {
            var playerLevel = player.GetComponent<BaseStats>().GetLevel();
            var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();

            if (!CanStartHarvesting(playerLevel, inventory))
            {
                return;
            }

            if (isHarvesting)
            {
                return;
            }

            currentPlayer = player;
            isHarvesting = true;

            // ИСПРАВЛЕНИЕ: Обновляем прогресс на основе оставшихся ресурсов
            currentProgress = (float)remainingResources / resource.ResourceAmount;

            // Получаем визуализатор инструментов
            toolVisualizer = player.GetComponent<HarvestingToolVisualizer>();
            if (toolVisualizer == null)
            {
                Debug.LogWarning("HarvestableSource: не найден HarvestingToolVisualizer на игроке");
            }

            // Определяем и показываем подходящий инструмент
            ShowHarvestingTool(inventory);

            // Очищаем корутину ожидания если она была
            if (waitForPlayerCoroutine != null)
            {
                StopCoroutine(waitForPlayerCoroutine);
                waitForPlayerCoroutine = null;
            }

            // Запускаем анимацию добычи
            StartHarvestAnimation();

            // Запускаем процесс добычи
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }
            harvestCoroutine = StartCoroutine(HarvestProcess());

            // ИСПРАВЛЕНИЕ: Теперь отправляем актуальный прогресс
            OnHarvestProgress?.Invoke(currentProgress);
        }

        // Проверить есть ли NavMesh в сцене
        private bool CheckNavMeshExists()
        {
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
            bool hasNavMesh = triangulation.vertices.Length > 0;
            
            if (!hasNavMesh)
            {
                Debug.LogError("[HARVEST] В сцене НЕТ NavMesh! Откройте Window -> AI -> Navigation и создайте NavMesh.");
            }
            
            return hasNavMesh;
        }
        
        // Получить ближайшую точку на NavMesh
        private Vector3 GetNavMeshPosition(Vector3 worldPosition)
        {
            UnityEngine.AI.NavMeshHit hit;
            float searchRadius = 10f; // Увеличиваем радиус поиска
            
            if (UnityEngine.AI.NavMesh.SamplePosition(worldPosition, out hit, searchRadius, UnityEngine.AI.NavMesh.AllAreas))
            {
                float distance = Vector3.Distance(worldPosition, hit.position);
                Debug.Log($"[HARVEST] Найдена точка NavMesh: {hit.position} (расстояние: {distance:F2}м)");
                
                return hit.position;
            }
            
            // Если не нашли точку на NavMesh, возвращаем исходную позицию
            Debug.LogError($"[HARVEST] НЕ НАЙДЕНА точка NavMesh в радиусе {searchRadius}м от {worldPosition}");
            return worldPosition;
        }
        
        // Диагностика NavMesh для разработчиков
        [ContextMenu("Диагностика NavMesh")]
        private void DiagnoseNavMesh()
        {
            Debug.Log("=== ДИАГНОСТИКА NAVMESH ===");
            
            // Проверяем NavMesh в сцене
            var triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
            Debug.Log($"NavMesh vertices: {triangulation.vertices.Length}");
            
            if (triangulation.vertices.Length == 0)
            {
                Debug.LogError("❌ NavMesh НЕ найден! Создайте NavMesh: Window -> AI -> Navigation");
                return;
            }
            
            // Проверяем позицию дерева
            Vector3 treePos = transform.position;
            Vector3 navMeshPos = GetNavMeshPosition(treePos);
            float distance = Vector3.Distance(treePos, navMeshPos);
            
            Debug.Log($"Позиция дерева: {treePos}");
            Debug.Log($"Ближайшая NavMesh точка: {navMeshPos}");
            Debug.Log($"Расстояние до NavMesh: {distance:F2}м");
            
            if (distance > 5f)
            {
                Debug.LogWarning($"⚠️ Дерево далеко от NavMesh ({distance:F2}м > 5м)");
                Debug.LogWarning("💡 Переместите дерево ближе к NavMesh области");
            }
            else
            {
                Debug.Log("✅ Дерево находится рядом с NavMesh");
            }
            
            // Проверяем игрока
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                Debug.Log($"Player NavMeshAgent - enabled: {agent?.enabled}, isOnNavMesh: {agent?.isOnNavMesh}");
                
                var mover = player.GetComponent<RPG.Movement.Mover>();
                bool canMove = mover.CanMoveTo(navMeshPos);
                Debug.Log($"Может ли игрок добраться до дерева: {canMove}");
            }
            
            Debug.Log("=== КОНЕЦ ДИАГНОСТИКИ ===");
        }
        
        // Визуальная помощь в Scene view
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            // Показываем позицию дерева
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Показываем ближайшую NavMesh точку
            Vector3 navMeshPos = GetNavMeshPosition(transform.position);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(navMeshPos, 0.3f);
            Gizmos.DrawLine(transform.position, navMeshPos);
            
            // Показываем радиус добычи
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            
            // Если расстояние до NavMesh большое - показываем красным
            float distance = Vector3.Distance(transform.position, navMeshPos);
            if (distance > 3f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, navMeshPos);
            }
        }
        
        // Получить причину, почему нельзя начать добычу
        private string GetHarvestBlockReason(int playerLevel, GameDevTV.Inventories.Inventory inventory)
        {
            if (playerLevel < resource.MinimumLevel)
	            return $"Требуется уровень {resource.MinimumLevel} (у Вас - {playerLevel})";
                
            if (resource.RequiredToolType != HarvestingToolType.None)
            {
                var tool = resource.GetBestToolFromInventory(inventory);
                if (tool == null)
                {
                    string toolName = resource.RequiredToolType switch
                    {
                        HarvestingToolType.Axe => "топор",
                        HarvestingToolType.Pickaxe => "кирка",
                        HarvestingToolType.Sickle => "серп",
                        HarvestingToolType.Skinning => "нож для снятия шкур",
                        _ => "инструмент"
                    };
                    return $"Требуется {toolName} T{resource.ResourceTier}+";
                }
                
                if (!tool.CanHarvestResourceTier(resource.ResourceTier))
                {
                    return $"Ваш инструмент T{tool.ToolTier} не подходит для ресурса T{resource.ResourceTier}";
                }
            }
            
            return "Неизвестная причина";
        }
        
		public virtual void StopHarvesting()
		{
			if (!isHarvesting)
			{
				return;
			}
    
			isHarvesting = false;
			currentPlayer = null;
    
			// Скрываем инструмент добычи
			HideHarvestingTool();
    
			// Останавливаем корутину добычи
			if (harvestCoroutine != null)
			{
				StopCoroutine(harvestCoroutine);
				harvestCoroutine = null;
			}
    
			// Останавливаем корутину ожидания
			if (waitForPlayerCoroutine != null)
			{
				StopCoroutine(waitForPlayerCoroutine);
				waitForPlayerCoroutine = null;
			}
    
			// Останавливаем анимацию
			StopHarvestAnimation();
		}
		
		/// <summary>
		/// Показывает подходящий инструмент для добычи данного ресурса
		/// </summary>
		private void ShowHarvestingTool(GameDevTV.Inventories.Inventory inventory)
		{
			if (toolVisualizer == null || resource == null)
			{
				return;
			}
    
			// Если ресурс не требует инструмента, ничего не показываем
			if (resource.RequiredToolType == HarvestingToolType.None)
			{
				Debug.Log("HarvestableSource: ресурс не требует инструмента, добыча руками");
				return;
			}
    
			// Получаем лучший инструмент для этого ресурса
			currentDisplayedTool = resource.GetBestToolFromInventory(inventory);
    
			if (currentDisplayedTool != null)
			{
				toolVisualizer.ShowHarvestingTool(currentDisplayedTool);
				Debug.Log($"HarvestableSource: показан инструмент {currentDisplayedTool.name}");
			}
			else
			{
				Debug.LogWarning("HarvestableSource: подходящий инструмент не найден в инвентаре");
			}
		}

		/// <summary>
		/// Скрывает текущий инструмент добычи
		/// </summary>
		private void HideHarvestingTool()
		{
			if (toolVisualizer != null)
			{
				toolVisualizer.HideHarvestingTool();
				Debug.Log("HarvestableSource: инструмент скрыт");
			}
    
			currentDisplayedTool = null;
		}
        
        protected virtual IEnumerator HarvestProcess()
        {
            int playerLevel = currentPlayer.GetComponent<BaseStats>().GetLevel();
            var inventory = currentPlayer.GetComponent<GameDevTV.Inventories.Inventory>();
            
            // Получаем лучший инструмент для добычи
            var bestTool = resource.GetBestToolFromInventory(inventory);
            
            // Вычисляем время добычи с учётом инструмента
            float harvestTime = resource.GetHarvestTime(playerLevel, bestTool);
            float timePerUnit = harvestTime / resource.ResourceAmount;
            
            while (remainingResources > 0 && isHarvesting)
            {
                // Ждём время для добычи одной единицы
                yield return new WaitForSeconds(timePerUnit);
                
                if (!isHarvesting) 
                {
                    break;
                }
                
                // Добываем одну единицу ресурса
                HarvestSingleUnit();
                
                // Обновляем прогресс (бар уменьшается от 1.0 до 0.0)
                currentProgress = (float)remainingResources / resource.ResourceAmount;
                OnHarvestProgress?.Invoke(currentProgress);
            }
            
            // Если ресурсы закончились
            if (remainingResources <= 0)
            {
                SetDepleted(true);
                OnResourceDepleted?.Invoke();
                StartRespawnTimer();
            }
            
            StopHarvesting();
        }
        
        protected virtual void HarvestSingleUnit()
        {
            remainingResources--;
            
            // Добавляем ресурс в инвентарь игрока
            var inventory = currentPlayer.GetComponent<Inventory>();
            if (inventory != null && resource.InventoryItem != null)
            {
                inventory.AddToFirstEmptySlot(resource.InventoryItem, 1);
            }
            
            // Проигрываем звук и эффекты
            PlayHarvestEffects();
            
            OnHarvestComplete?.Invoke(resource, 1);
        }
        
        protected virtual void SetDepleted(bool depleted)
        {
            isDepleted = depleted;
            
            if (depleted)
            {
                remainingResources = 0;
                currentProgress = 0f; // Бар полностью пустой
                
                // Создаём эффект истощения ресурса
                var vfxManager = FindObjectOfType<HarvestingVFXManager>();
                if (vfxManager != null)
                {
                    vfxManager.CreateResourceDepletedEffect(transform.position);
                }
                
                // Запускаем корутину респавна
                if (respawnCoroutine == null)
                {
                    respawnCoroutine = StartCoroutine(RespawnCoroutine());
                }
            }
            else
            {
                remainingResources = resource.ResourceAmount;
                currentProgress = 1f; // Бар полностью полный
            }
            
            UpdateVisuals();
            OnHarvestProgress?.Invoke(currentProgress);
        }
        
        protected virtual void StartRespawnTimer()
        {
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
            }
            
            respawnCoroutine = StartCoroutine(RespawnCoroutine());
        }
        
        protected virtual IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(respawnTime);
            
            SetDepleted(false);
            respawnCoroutine = null;
        }
        
        protected virtual void UpdateVisuals()
        {
            if (activeModel != null)
                activeModel.SetActive(!isDepleted);
                
            if (depletedModel != null)
                depletedModel.SetActive(isDepleted);
        }
        
        protected virtual void PlayHarvestEffects()
        {
            // Звук
            if (resource.HarvestSound != null)
            {
                AudioSource.PlayClipAtPoint(resource.HarvestSound, transform.position);
            }
            
            // Эффекты частиц
            if (resource.HarvestEffect != null)
            {
                Instantiate(resource.HarvestEffect, transform.position, Quaternion.identity);
            }
            
            // Интеграция с новой системой VFX
            if (HarvestingManager.Instance != null)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();
                    var bestTool = HarvestingToolChecker.FindBestTool(inventory, resource.RequiredToolType, resource.ResourceTier);
                    var toolType = bestTool != null ? bestTool.ToolType : HarvestingToolType.None;
                    
                    // Создаём эффект добычи ресурса
                    var vfxManager = FindObjectOfType<HarvestingVFXManager>();
                    if (vfxManager != null)
                    {
                        vfxManager.CreateResourceHarvestEffect(transform.position, resource);
                    }
                }
            }
        }
        
        // Абстрактные методы для переопределения в наследниках
        protected abstract void StartHarvestAnimation();
        protected abstract void StopHarvestAnimation();
        
        // Реализация IRaycastable
        public virtual CursorType GetCursorType()
        {
            if (isDepleted)
            {
                return CursorType.None;
            }
            
            // Проверяем доступность добычи для текущего игрока
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                int playerLevel = player.GetComponent<BaseStats>().GetLevel();
                var inventory = player.GetComponent<GameDevTV.Inventories.Inventory>();
                
                if (CanStartHarvesting(playerLevel, inventory))
                {
                    return CursorType.Harvesting;  // Зеленый курсор - можно добывать
                }
                else
                {
                    return CursorType.HarvestingBlocked;  // Красный курсор - нельзя добывать
                }
            }
            
            // Если нет игрока, показываем обычный курсор добычи
            return CursorType.Harvesting;
        }
        
        private Coroutine waitForPlayerCoroutine = null;

		public virtual bool HandleRaycast(PlayerController callingController)
		{
			if (isDepleted)
			{
				return false;
			}
    
			if (isHarvesting)
			{
				return true;
			}
    
			if (waitForPlayerCoroutine != null)
			{
				return true;
			}
    
			if (Input.GetMouseButtonDown(0))
			{
				var playerLevel = callingController.GetComponent<BaseStats>().GetLevel();
				var inventory = callingController.GetComponent<GameDevTV.Inventories.Inventory>();
        
				if (CanStartHarvesting(playerLevel, inventory))
				{
					float distance = Vector3.Distance(callingController.transform.position, transform.position);

					if (distance <= harvestDistance)
					{
						HarvestingManager.RegisterHarvestStart(this);
					}
					else
					{
						var mover = callingController.GetComponent<RPG.Movement.Mover>();
						if (mover != null)
						{
							Vector3 dir = (callingController.transform.position - transform.position).normalized;
							Vector3 targetPos = transform.position + dir * harvestDistance;

							mover.StartMoveAction(targetPos, 1f);
							StartCoroutine(CheckDistanceAndStartHarvest(callingController));
						}
					}
				}
				else
				{
					// *** НАЧАЛО ИЗМЕНЕНИЙ ***
					// Если добывать нельзя, получаем причину и показываем ее игроку
					string reason = GetHarvestBlockReason(playerLevel, inventory);
					RequirementTextManager.Show(reason);
					// *** КОНЕЦ ИЗМЕНЕНИЙ ***
				}
			}
    
			return true;
		}
        
                protected virtual IEnumerator WaitForPlayerAndStartHarvest(PlayerController player)
        {
            float harvestDistance = 2f;
            float timeout = 10f;
            float elapsed = 0f;
            
            Debug.Log("WaitForPlayerAndStartHarvest: начало ожидания");
            
            // Ждём пока игрок приблизится
            while (elapsed < timeout)
            {
                // Проверяем, не началась ли уже добыча
                if (isHarvesting) 
                {
                    Debug.Log("WaitForPlayerAndStartHarvest: добыча уже началась, выходим");
                    waitForPlayerCoroutine = null;
                    yield break;
                }
                
                float distance = Vector3.Distance(player.transform.position, transform.position);
                
                if (distance <= harvestDistance)
                {
                    // Игрок приблизился - проверяем что он остановился
                    var navAgent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (navAgent != null && navAgent.velocity.magnitude < 0.5f)
                    {
                        // Игрок остановился - начинаем добычу
                        Debug.Log($"WaitForPlayerAndStartHarvest: игрок остановился (расстояние: {distance:F2}м, скорость: {navAgent.velocity.magnitude:F2}), начинаем добычу");
                        waitForPlayerCoroutine = null;
                        HarvestingManager.RegisterHarvestStart(this);
                        yield break;
                    }
                    else
                    {
                        Debug.Log($"WaitForPlayerAndStartHarvest: игрок близко но ещё движется (скорость: {navAgent.velocity.magnitude:F2}), ждём...");
                    }
                }
                
                elapsed += Time.deltaTime;
                yield return new WaitForSeconds(0.1f); // Проверяем каждые 0.1 сек
            }
            
            // Таймаут - очищаем корутину
            Debug.LogWarning("WaitForPlayerAndStartHarvest: таймаут ожидания");
            waitForPlayerCoroutine = null;
        }

		private IEnumerator CheckDistanceAndStartHarvest(PlayerController player)
		{
			float timeout = 10f;
			float elapsed = 0f;

			var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();

			while (elapsed < timeout)
			{
				if (isHarvesting) yield break;

				float distance = Vector3.Distance(player.transform.position, transform.position);

				// Считаем, что дошёл, если расстояние <= harvestDistance + 0.2f
				if (distance <= harvestDistance + 0.2f)
				{
					if (agent != null)
					{
						agent.isStopped = true; // Принудительно останавливаем
						agent.ResetPath();
					}
					HarvestingManager.RegisterHarvestStart(this);
					yield break;
				}

				elapsed += Time.deltaTime;
				yield return new WaitForSeconds(0.1f);
			}
		}
		
		public object CaptureState()
		{
			// Создаем словарь для хранения данных.
			var data = new Dictionary<string, object>();
			// Сохраняем текущее состояние "истощённости"
			data["isDepleted"] = isDepleted;
			// Сохраняем количество оставшихся ресурсов
			data["remainingResources"] = remainingResources;

			return data;
		}
		
		public void RestoreState(object state)
		{
			// Восстанавливаем данные из словаря
			var data = (Dictionary<string, object>)state;
			isDepleted = JsonSaveHelper.ToBool(data["isDepleted"]);
			remainingResources = JsonSaveHelper.ToInt(data["remainingResources"]);

			// Обновляем внешний вид ресурса
			UpdateVisuals();

			// Самое важное: если ресурс одноразовый и истощён, мы не запускаем таймер респауна.
			if (isOneTimeResource && isDepleted)
			{
				// Выходим из метода, не делая ничего
				return;
			}

			// Если ресурс истощён, но не одноразовый, запускаем таймер респауна
			if (isDepleted)
			{
				StartRespawnTimer();
			}
		}
    }
} 