using RPG.Attributes;
using RPG.Core;
using RPG.Stats;
using GameDevTV.Saving;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json;

namespace RPG.Movement
{
	public class Mover : MonoBehaviour, IAction, ISaveable
	{
		[SerializeField] private Transform _target;
		[SerializeField] private float _maxNavPathLength = 40f;

		private ActionScheduler _actionScheduler;
		private NavMeshAgent _navMeshAgent;
		private Animator _animator;
		private Health _health;
		private BaseStats _baseStats;

		// Кэшированное состояние для оптимизации
		private bool _isDead = false;
		private bool _wasEnabledLastFrame = true;
		
		// Дефолтная скорость для NPC без BaseStats
		[SerializeField] private float _defaultMovementSpeed = 3.5f;

		private void Awake()
		{
			_navMeshAgent = GetComponent<NavMeshAgent>();
			_health = GetComponent<Health>(); // Может быть null
			_actionScheduler = GetComponent<ActionScheduler>();           
			_animator = GetComponent<Animator>();        
			_baseStats = GetComponent<BaseStats>();
		}

		private void Start()
		{
			// Подписываемся на события смерти если есть Health
			if (_health != null)
			{
				_health.onDie.AddListener(OnDied);
			}
			
			// Устанавливаем начальное состояние NavMeshAgent
			UpdateNavMeshAgentState();
		}

		private void OnDestroy()
		{
			// Отписываемся от событий
			if (_health != null)
			{
				_health.onDie.RemoveListener(OnDied);
			}
		}

		private void OnDied()
		{
			_isDead = true;
			UpdateNavMeshAgentState();
		}

		private void Update()
		{
			// Обновляем анимацию только если нужно (когда NavMeshAgent активен)
			if (_navMeshAgent.enabled && !_isDead)
			{
				UpdateAnimator();
			}
		}

		private void UpdateNavMeshAgentState()
		{
			bool shouldBeEnabled = !_isDead;
			
			// Обновляем состояние только если оно изменилось
			if (_navMeshAgent.enabled != shouldBeEnabled)
			{
				_navMeshAgent.enabled = shouldBeEnabled;
			}
		}

		public void StartMoveAction(Vector3 destination, float speedFraction)
		{
			_actionScheduler.StartAction(this);
			MoveTo(destination, speedFraction);
		}

		public bool CanMoveTo(Vector3 destination)
		{
			NavMeshPath path = new NavMeshPath();
			bool hasPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
			if (!hasPath) return false;
			if (path.status != NavMeshPathStatus.PathComplete) return false;
			if (GetPathLength(path) > _maxNavPathLength) return false;

			return true;
		}

		public void MoveTo(Vector3 destination, float speedFraction)
		{
			// Не двигаемся если мертвы
			if (_isDead) return;
			
			// Проверяем что NavMeshAgent не null и корректно настроен
			if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
			{
				_navMeshAgent.destination = destination;
				
				// Получаем скорость из BaseStats или используем дефолтную
				float baseSpeed = _baseStats != null ? _baseStats.GetStat(Stat.MovementSpeed) : _defaultMovementSpeed;
				_navMeshAgent.speed = baseSpeed * Mathf.Clamp01(speedFraction);
				_navMeshAgent.isStopped = false;
			}
		}

		public void Cancel()
		{
			if (_navMeshAgent != null && _navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
			{
				_navMeshAgent.isStopped = true;
			}
		}

		// Публичные методы для внешнего управления состоянием
		public bool IsDead => _isDead;
		public bool IsMoving => _navMeshAgent != null && _navMeshAgent.enabled && !_navMeshAgent.isStopped && _navMeshAgent.hasPath;
		
		// Методы для настройки скорости мирных NPC
		public void SetDefaultMovementSpeed(float speed)
		{
			_defaultMovementSpeed = Mathf.Max(0.1f, speed);
		}
		
		public float GetCurrentSpeed()
		{
			return _baseStats != null ? _baseStats.GetStat(Stat.MovementSpeed) : _defaultMovementSpeed;
		}

		private void UpdateAnimator()
		{
			if (_navMeshAgent != null && _animator != null)
			{
				Vector3 velocity = _navMeshAgent.velocity;
				Vector3 localVelocity = transform.InverseTransformDirection(velocity);
				float speed = localVelocity.z;
				_animator.SetFloat("forwardSpeed", speed);
			}
		}

		private float GetPathLength(NavMeshPath path)
		{
			float total = 0;
			if (path.corners.Length < 2) return total;
			for (int i = 0; i < path.corners.Length - 1; i++)
			{
				total += Vector3.Distance(path.corners[i], path.corners[i + 1]);
			}
			return total;
		}

		[System.Serializable]
		public struct MoverSaveData
		{
			[JsonProperty] public SerializableVector3 position;
			[JsonProperty] public SerializableQuaternion rotation;
		}

		public object CaptureState()
		{
			MoverSaveData data = new MoverSaveData();
			data.position = new SerializableVector3(transform.position);
			data.rotation = new SerializableQuaternion(transform.rotation);
			return data;
		}

		public void RestoreState(object state)
		{
			MoverSaveData data = (MoverSaveData)state;
			_navMeshAgent.enabled = false;
			transform.position = data.position.ToVector();
			transform.rotation = data.rotation.ToQuaternion();
			_navMeshAgent.enabled = true;
            
			// ДОБАВЛЕНО: Обновляем скорость NavMeshAgent на основе сохраненных характеристик.
			// Это гарантирует, что бонусы от TraitStore будут применены после загрузки.
			_navMeshAgent.speed = _baseStats.GetStat(Stat.MovementSpeed);
		}
	}
}