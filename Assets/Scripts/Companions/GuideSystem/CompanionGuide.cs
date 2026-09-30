using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RPG.SceneManagement;

namespace RPG.Companions
{
    /// <summary>
    /// Аддон поверх CompanionController: собака ведёт игрока к цели (в т.ч. через несколько сцен).
    /// Лежит на собаке рядом с CompanionController. Запуск/отмена — через диалог (UnityEvent) или из кода:
    ///   GuideToCurrentQuestTarget(), GuideTo(string targetId), StopGuide().
    /// </summary>
    [RequireComponent(typeof(CompanionController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CompanionGuide : MonoBehaviour, ICompanionFollowOverride
    {
        [Header("Данные")]
        [SerializeField] private CompanionData companionData;
        [SerializeField] private GuideMap map;
        [Tooltip("MonoBehaviour, реализующий IGuideTargetSource (адаптер квестов). Нужен только для GuideToCurrentQuestTarget().")]
        [SerializeField] private MonoBehaviour currentTargetSource;

        [Header("Движение")]
        [Tooltip("Запасная скорость. Используется, если на собаке нет AnimalTraining или для неё не задан Stat.GuideMoveSpeed в Progression.")]
        [SerializeField] private float guideSpeed = 3.5f;
        [Tooltip("На каком расстоянии от финальной цели собака считается дошедшей.")]
        [SerializeField] private float arriveDistance = 1.5f;
        [SerializeField] private float repathInterval = 0.3f;
        [Tooltip("Сколько секунд ждать, пока цель/портал зарегистрируются в сцене, прежде чем считать это ошибкой.")]
        [SerializeField] private float resolveTimeout = 2f;

        [Header("Ожидание игрока")]
        [Tooltip("Игрок дальше этого — собака останавливается.")]
        [SerializeField] private float waitForPlayerDistance = 10f;
        [Tooltip("Игрок ближе этого — собака продолжает путь.")]
        [SerializeField] private float resumeDistance = 5f;
        [SerializeField] private bool facePlayerWhileWaiting = true;

        [Header("Animator (bool-параметры, пусто = не использовать)")]
        [Tooltip("True, пока собака ведёт игрока (переключай locomotion на 'нюх на ходу').")]
        [SerializeField] private string guidingBoolParam = "isGuiding";
        [Tooltip("True, пока собака стоит и ждёт игрока / стоит у цели или портала.")]
        [SerializeField] private string waitingBoolParam = "isGuideWaiting";

        [Header("События")]
        public UnityEvent onGuideStarted;
        public UnityEvent onWaitingForPlayer;
        public UnityEvent onArrived;
        public UnityEvent onGuideStopped;
        public UnityEvent onGuideFailed;

        private enum Phase { Joining, Moving, WaitingForPlayer, WaitingAtPortal, WaitingAtTarget }

        private CompanionController _controller;
        private NavMeshAgent _agent;
        private Animator _animator;
        private IGuideTargetSource _source;
        private AnimalTraining _training;
        private float _cachedGuideSpeed;
        private float _nextSpeedRefreshTime;
        private int _guidingHash, _waitingHash;

        private Transform _player;
        private GuideTarget _goalTarget;
        private Portal _goalPortal;
        private string _targetId;
        private Phase _phase;

        private bool  _guiding;
        private bool  _engaged;            // реально ли мы сейчас рулим агентом (false после боя)
        private bool  _destinationIssued;
        private float _savedStoppingDistance;
        private float _nextRepathTime;
        private float _resolveStarted = -1f;
        private bool  _quitting;

        public bool IsGuiding => _guiding;
        bool ICompanionFollowOverride.IsOverriding => _guiding;

        public bool CanGuideToCurrentTarget =>
            _source != null && _source.TryGetCurrentTargetId(out string id) && IsReachable(id);

        private string CompanionId => companionData != null ? companionData.CompanionID : null;

        // ── lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _controller = GetComponent<CompanionController>();
            _agent      = GetComponent<NavMeshAgent>();
            _animator   = GetComponentInChildren<Animator>();
            _training   = GetComponent<AnimalTraining>();
            _source     = currentTargetSource as IGuideTargetSource;

            if (currentTargetSource != null && _source == null)
                Debug.LogError("[CompanionGuide] currentTargetSource не реализует IGuideTargetSource", this);

            _guidingHash = ValidateParam(guidingBoolParam);
            _waitingHash = ValidateParam(waitingBoolParam);

            _controller.SetFollowOverride(this);
        }

        private void OnValidate()
        {
            resumeDistance = Mathf.Min(resumeDistance, waitForPlayerDistance - 0.5f);
        }

        private void Update()
        {
            if (!_guiding)
            {
                // Собаку пересоздали в новой сцене, а сессия жива — продолжаем.
                if (_controller.IsActive && GuideSession.TryGet(CompanionId, out string id))
                {
                    _targetId = id;
                    BeginLocal(resumed: true);
                }
                return;
            }

            // Кто-то вызвал CompanionController.Deactivate() ("подожди здесь", смерть и т.д.)
            if (!_controller.IsActive) EndGuide(clearSession: true);
        }

        private void OnApplicationQuit() => _quitting = true;

        private void OnDestroy()
        {
            // Смена сцены через Portal или выгрузка сцены (scene.isLoaded == false) —
            // сессию НЕ трогаем, собака продолжит в новой сцене.
            // Ручной Destroy (например SendToBase) — сессию отменяем.
            if (_guiding && !_quitting && !Portal.IsTransitioning && gameObject.scene.isLoaded)
                GuideSession.Clear(CompanionId);
        }

        // ── публичный API (для UnityEvent / диалога) ─────────────────────

        public void GuideToCurrentQuestTarget()
        {
            if (_source != null && _source.TryGetCurrentTargetId(out string id)) GuideTo(id);
            else Fail("нет источника цели или у квеста нет текущей цели");
        }

        /// <summary>
        /// Вызывать из onEnterActions "ноды-продолжения" после того, как игрок выбрал квест
        /// в динамическом списке (см. CompanionQuestGuideChoiceProvider / QuestGuideSelection).
        /// </summary>
        public void GuideToPendingSelection()
        {
            string id = QuestGuideSelection.PendingTargetId;
            QuestGuideSelection.Clear();

            if (string.IsNullOrEmpty(id)) { Fail("нет выбранной через диалог цели"); return; }
            GuideTo(id);
        }

        public void GuideTo(string targetId)
        {
            if (!_controller.IsActive) { Fail("компаньон не активен"); return; }
            if (CompanionId == null)   { Fail("не назначен companionData"); return; }
            if (!IsReachable(targetId)) { Fail($"цель '{targetId}' неизвестна или до неё нет маршрута"); return; }

            _targetId = targetId;
            GuideSession.Begin(CompanionId, targetId);
            BeginLocal(resumed: false);
            onGuideStarted.Invoke();
        }

        /// <summary>Доступна ли сейчас цель с таким id (для CompanionQuestGuideChoiceProvider).</summary>
        public bool IsGuideTargetReachable(string targetId) => IsReachable(targetId);

        /// <summary>Отказ от помощи — собака возвращается к обычному следованию.</summary>
        public void StopGuide() => EndGuide(clearSession: true);

        // ── ICompanionFollowOverride ─────────────────────────────────────

        public void OnInterrupted() => Disengage();

        public void Tick()
        {
            if (_player == null)
            {
                GameObject p = GameObject.FindWithTag("Player");
                if (p == null) return;
                _player = p.transform;
            }

            if (!_engaged)
            {
                _engaged = true;
                _savedStoppingDistance = _agent.stoppingDistance;
                _nextRepathTime = 0f;
            }

            if (!HasGoal && !TryResolveGoal()) return;

            Vector3 goal = GetGoalPosition();
            float distToPlayer = Vector3.Distance(transform.position, _player.position);

            switch (_phase)
            {
                case Phase.Joining:          TickJoining(distToPlayer);            break;
                case Phase.Moving:           TickMoving(goal, distToPlayer);       break;
                case Phase.WaitingForPlayer: TickWaitingForPlayer(distToPlayer);   break;
                case Phase.WaitingAtPortal:  Hold();                               break;
                case Phase.WaitingAtTarget:  TickWaitingAtTarget(distToPlayer);    break;
            }
        }

        // ── фазы ─────────────────────────────────────────────────────────

        private void TickJoining(float distToPlayer)
        {
            // После смены сцены собака могла появиться далеко от игрока — сначала подбегает к нему.
            if (distToPlayer <= resumeDistance) { SetPhase(Phase.Moving); return; }
            MoveTo(_player.position, resumeDistance * 0.5f);
        }

        private void TickMoving(Vector3 goal, float distToPlayer)
        {
            if (HasArrived(goal))
            {
                if (_goalPortal != null)
                {
                    SetPhase(Phase.WaitingAtPortal);
                    Hold();
                }
                else
                {
                    SetPhase(Phase.WaitingAtTarget);
                    Hold();
                    onArrived.Invoke();
                }
                return;
            }

            if (distToPlayer > waitForPlayerDistance)
            {
                SetPhase(Phase.WaitingForPlayer);
                Hold();
                onWaitingForPlayer.Invoke();
                return;
            }

            MoveTo(goal, _goalPortal != null ? 0.1f : arriveDistance);
        }

        private void TickWaitingForPlayer(float distToPlayer)
        {
            Hold();
            if (distToPlayer <= resumeDistance) SetPhase(Phase.Moving);
        }

        private void TickWaitingAtTarget(float distToPlayer)
        {
            Hold();
            // Довели — как только игрок подошёл, помощь заканчивается.
            if (distToPlayer <= resumeDistance) EndGuide(clearSession: true);
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _destinationIssued = false;
            _nextRepathTime = 0f;
        }

        // ── движение ─────────────────────────────────────────────────────

        private void MoveTo(Vector3 destination, float stoppingDistance)
        {
            _agent.isStopped = false;
            _agent.speed = GetGuideSpeed();
            _agent.stoppingDistance = stoppingDistance;

            if (!_destinationIssued || Time.time >= _nextRepathTime)
            {
                _agent.SetDestination(destination);
                _destinationIssued = true;
                _nextRepathTime = Time.time + repathInterval;
            }

            SetAnim(guiding: true, waiting: false);
        }

        /// <summary>
        /// Скорость движения к GuideTarget: Stat.GuideMoveSpeed (Progression + навыки у дрессировщика).
        /// Значение кэшируется на полсекунды — GetStat каждый кадр не нужен.
        /// </summary>
        private float GetGuideSpeed()
        {
            if (_training == null) return guideSpeed;

            if (Time.time >= _nextSpeedRefreshTime)
            {
                _cachedGuideSpeed = _training.GetGuideMoveSpeed(guideSpeed);
                _nextSpeedRefreshTime = Time.time + 0.5f;
            }
            return _cachedGuideSpeed;
        }

        private void Hold()
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            SetAnim(guiding: false, waiting: true);

            if (!facePlayerWhileWaiting || _player == null) return;

            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }

        private bool HasArrived(Vector3 goal)
        {
            if (_goalPortal != null)
            {
                // Для портала — "внутри триггера".
                Collider col = _goalPortal.GetComponent<Collider>();
                if (col != null && (col.ClosestPoint(transform.position) - transform.position).sqrMagnitude < 0.0001f)
                    return true;
            }
            else if (PlanarDistance(transform.position, goal) <= arriveDistance)
            {
                return true;
            }

            // Фолбэк: агент дошёл до конца пути (точка вне navmesh, частичный путь и т.п.)
            return _destinationIssued
                && !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance + 0.15f
                && _agent.velocity.sqrMagnitude < 0.05f;
        }

        // ── цель / маршрут ───────────────────────────────────────────────

        private bool HasGoal => _goalTarget != null || _goalPortal != null;

        private Vector3 GetGoalPosition()
        {
            Vector3 raw = _goalTarget != null ? _goalTarget.Position : _goalPortal.transform.position;
            return NavMesh.SamplePosition(raw, out NavMeshHit hit, 4f, NavMesh.AllAreas) ? hit.position : raw;
        }

        /// <summary>Цель в этой сцене → GuideTarget, иначе → портал на следующую сцену по графу.</summary>
        private bool TryResolveGoal()
        {
            if (_resolveStarted < 0f) _resolveStarted = Time.time;

            if (map == null || !map.TryGetTargetScene(_targetId, out int targetScene))
            {
                Fail($"цель '{_targetId}' не найдена в GuideMap");
                return false;
            }

            int current = SceneManager.GetActiveScene().buildIndex;

            if (current == targetScene)
            {
                if (GuideTarget.TryGetLoaded(_targetId, out GuideTarget t))
                {
                    _goalTarget = t; _goalPortal = null; _resolveStarted = -1f;
                    return true;
                }
            }
            else
            {
                int next = map.GetNextScene(current, targetScene);
                if (next < 0) { Fail("нет маршрута до сцены цели"); return false; }

                Portal portal = FindPortalTo(next);
                if (portal != null)
                {
                    _goalPortal = portal; _goalTarget = null; _resolveStarted = -1f;
                    return true;
                }
            }

            if (Time.time - _resolveStarted > resolveTimeout)
                Fail("цель/портал не найдены в текущей сцене (портал закрыт?)");

            return false;
        }

        private Portal FindPortalTo(int sceneIndex)
        {
            Portal best = null;
            float bestDist = float.MaxValue;

            foreach (Portal p in FindObjectsOfType<Portal>())
            {
                if (p.SceneToLoad != sceneIndex || !p.IsAvailable) continue;

                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < bestDist) { best = p; bestDist = d; }
            }
            return best;
        }

        private bool IsReachable(string targetId)
        {
            if (map == null || string.IsNullOrEmpty(targetId)) return false;
            if (!map.TryGetTargetScene(targetId, out int targetScene)) return false;

            int current = SceneManager.GetActiveScene().buildIndex;
            return current == targetScene || map.GetNextScene(current, targetScene) >= 0;
        }

        // ── старт / конец ────────────────────────────────────────────────

        private void BeginLocal(bool resumed)
        {
            _guiding = true;
            _engaged = false;
            _goalTarget = null;
            _goalPortal = null;
            _resolveStarted = -1f;
            _phase = resumed ? Phase.Joining : Phase.Moving;
            _destinationIssued = false;
        }

        private void EndGuide(bool clearSession, bool notify = true)
        {
            if (!_guiding) return;

            _guiding = false;
            Disengage();
            _goalTarget = null;
            _goalPortal = null;

            if (clearSession) GuideSession.Clear(CompanionId);
            if (notify) onGuideStopped.Invoke();
        }

        private void Disengage()
        {
            if (!_engaged) return;

            _engaged = false;
            _agent.stoppingDistance = _savedStoppingDistance;
            SetAnim(false, false);
        }

        private void Fail(string reason)
        {
            Debug.LogWarning($"[CompanionGuide] {reason}", this);
            EndGuide(clearSession: true, notify: false);
            onGuideFailed.Invoke();
        }

        // ── утилиты ──────────────────────────────────────────────────────

        private int ValidateParam(string paramName)
        {
            if (string.IsNullOrEmpty(paramName) || _animator == null) return 0;

            foreach (var p in _animator.parameters)
                if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool)
                    return p.nameHash;

            Debug.LogWarning($"[CompanionGuide] В Animator нет bool-параметра '{paramName}'", this);
            return 0;
        }

        private void SetAnim(bool guiding, bool waiting)
        {
            if (_animator == null) return;
            if (_guidingHash != 0) _animator.SetBool(_guidingHash, guiding);
            if (_waitingHash != 0) _animator.SetBool(_waitingHash, waiting);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, waitForPlayerDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, resumeDistance);
        }
    }

    /// <summary>
    /// Состояние "ведения", живущее вне собаки (её пересоздают при каждой загрузке сцены).
    /// Не сохраняется в сейвы: после загрузки сохранения собака ведёт себя как обычно.
    /// </summary>
    public static class GuideSession
    {
        private static string _companionId;
        private static string _targetId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => ClearAll();

        public static void Begin(string companionId, string targetId)
        {
            _companionId = companionId;
            _targetId = targetId;
        }

        public static bool TryGet(string companionId, out string targetId)
        {
            targetId = _targetId;
            return !string.IsNullOrEmpty(companionId) && companionId == _companionId && !string.IsNullOrEmpty(_targetId);
        }

        public static void Clear(string companionId)
        {
            if (companionId == _companionId) ClearAll();
        }

        public static void ClearAll()
        {
            _companionId = null;
            _targetId = null;
        }
    }
}
