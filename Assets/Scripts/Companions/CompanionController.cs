using UnityEngine;
using UnityEngine.AI;
using RPG.Combat;
using RPG.Attributes;
using RPG.Core;

namespace RPG.Companions
{
    /// <summary>
    /// Контроллер компаньона - управляет поведением компаньона в бою и следованием за игроком
    /// </summary>
    [RequireComponent(typeof(Fighter))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CompanionController : MonoBehaviour
    {
        [Header("Настройки следования")]
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float followStopDistance = 2f;
        [SerializeField] private float followSpeedFraction = 1f;
        
        [Header("Настройки боя")]
        [SerializeField] private float combatRange = 10f;
        [SerializeField] private float attackRange = 5f;
        
        private Transform _player;
        private Fighter _fighter;
        private Health _health;
        private NavMeshAgent _navMeshAgent;
        private ActionScheduler _actionScheduler;
        
        private Health _currentTarget;
        private CompanionData _companionData;
        private string _companionID;

        private enum CompanionState
        {
            Following,
            Combat,
            Idle
        }

        private CompanionState _currentState = CompanionState.Following;

        private void Awake()
        {
            _fighter = GetComponent<Fighter>();
            _health = GetComponent<Health>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _actionScheduler = GetComponent<ActionScheduler>();
        }

        private void Start()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
            else
            {
                Debug.LogError("CompanionController: Игрок не найден!");
            }
        }

        public void Initialize(CompanionData data)
        {
            _companionData = data;
            _companionID = data.CompanionID;
            
            followDistance = data.FollowDistance;
            combatRange = data.CombatRange;
            
            // Меняем тег чтобы враги атаковали
            if (!gameObject.CompareTag("Companion"))
            {
                gameObject.tag = "Player";
            }
            
            // Включаем NavMeshAgent
            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = true;
            }
            
            // Регистрируем в менеджере
            CompanionManager.Instance.RegisterActiveCompanion(_companionID, this);
        }

        private void Update()
        {
            if (_health.IsDead()) return;
            if (_player == null) return;

            switch (_currentState)
            {
                case CompanionState.Following:
                    FollowBehaviour();
                    CheckForEnemies();
                    break;
                    
                case CompanionState.Combat:
                    CombatBehaviour();
                    break;
                    
                case CompanionState.Idle:
                    IdleBehaviour();
                    break;
            }
        }

        private void FollowBehaviour()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
            
            if (distanceToPlayer > followDistance)
            {
                // Двигаемся к игроку
                _navMeshAgent.SetDestination(_player.position);
                _navMeshAgent.isStopped = false;
            }
            else if (distanceToPlayer < followStopDistance)
            {
                // Слишком близко - останавливаемся
                _navMeshAgent.isStopped = true;
            }
        }

        private void CheckForEnemies()
        {
            // Проверяем есть ли враги рядом с игроком
            Health playerTarget = null;
            
            // Сначала проверяем цель игрока
            Fighter playerFighter = _player.GetComponent<Fighter>();
            if (playerFighter != null)
            {
                playerTarget = playerFighter.GetTarget();
            }

            if (playerTarget != null && !playerTarget.IsDead())
            {
                float distanceToTarget = Vector3.Distance(transform.position, playerTarget.transform.position);
                if (distanceToTarget <= combatRange)
                {
                    _currentTarget = playerTarget;
                    _currentState = CompanionState.Combat;
                    return;
                }
            }

            // Ищем ближайшего врага в радиусе
            Health nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                _currentTarget = nearestEnemy;
                _currentState = CompanionState.Combat;
            }
        }

        private void CombatBehaviour()
        {
            if (_currentTarget == null || _currentTarget.IsDead())
            {
                _currentTarget = null;
                _currentState = CompanionState.Following;
                _fighter.Cancel();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            // Если цель слишком далеко от игрока - прекращаем бой
            if (distanceToTarget > combatRange || distanceToPlayer > combatRange * 1.5f)
            {
                _currentTarget = null;
                _currentState = CompanionState.Following;
                _fighter.Cancel();
                return;
            }

            // Атакуем цель
            _fighter.Attack(_currentTarget.gameObject);
        }

        private void IdleBehaviour()
        {
            _navMeshAgent.isStopped = true;
            CheckForEnemies();
        }

        private Health FindNearestEnemy()
        {
            Health nearest = null;
            float nearestDistance = Mathf.Infinity;

            Collider[] hits = Physics.OverlapSphere(transform.position, combatRange);
            foreach (Collider hit in hits)
            {
                Health health = hit.GetComponent<Health>();
                if (health == null) continue;
                if (health.IsDead()) continue;
                if (health.gameObject == gameObject) continue;
                if (health.gameObject.CompareTag("Player")) continue;
                if (health.gameObject.CompareTag("Companion")) continue;

                // Проверяем что это враг (есть AIController или CombatTarget)
                if (hit.GetComponent<RPG.Control.AIController>() == null && 
                    hit.GetComponent<CombatTarget>() == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearest = health;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        public void OnEnable()
        {
            // При активации компаньона
            if (_health != null)
            {
                _health.onDie.AddListener(OnCompanionDeath);
            }
        }

        public void OnDisable()
        {
            // При деактивации
            if (_health != null)
            {
                _health.onDie.RemoveListener(OnCompanionDeath);
            }
        }

        private void OnCompanionDeath()
        {
            // При смерти компаньона
            if (!string.IsNullOrEmpty(_companionID))
            {
                CompanionManager.Instance.DismissCompanion(_companionID);
            }
        }

        private void OnDestroy()
        {
            // Удаляем регистрацию при уничтожении
            if (!string.IsNullOrEmpty(_companionID))
            {
                CompanionManager.Instance.UnregisterActiveCompanion(_companionID);
            }
        }
        
        public void Deactivate()
        {
            // Возвращаем обычный тег
            gameObject.tag = "Untagged";
            
            // Отключаем NavMeshAgent
            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = false;
            }
            
            // Сбрасываем состояние
            _currentState = CompanionState.Idle;
            _currentTarget = null;
            
            if (_fighter != null)
            {
                _fighter.Cancel();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Визуализация радиусов
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, followDistance);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, combatRange);
        }
    }
}
