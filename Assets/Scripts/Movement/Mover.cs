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

		private void Awake()
		{
			_navMeshAgent = GetComponent<NavMeshAgent>();
			_health = GetComponent<Health>();
			_actionScheduler = GetComponent<ActionScheduler>();           
			_animator = GetComponent<Animator>();        
			_baseStats = GetComponent<BaseStats>();
		}

		private void Update()
		{
			_navMeshAgent.enabled = !_health.IsDead();
			UpdateAnimator();
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
			if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
			{
				_navMeshAgent.destination = destination;
				_navMeshAgent.speed = _baseStats.GetStat(Stat.MovementSpeed) * Mathf.Clamp01(speedFraction);
				_navMeshAgent.isStopped = false;
			}
		}

		public void Cancel()
		{
			if (_navMeshAgent.enabled && _navMeshAgent.isOnNavMesh)
			{
				_navMeshAgent.isStopped = true;
			}
		}

		private void UpdateAnimator()
		{
			Vector3 velocity = _navMeshAgent.velocity;
			Vector3 localVelocity = transform.InverseTransformDirection(velocity);
			float speed = localVelocity.z;
			_animator.SetFloat("forwardSpeed", speed);
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