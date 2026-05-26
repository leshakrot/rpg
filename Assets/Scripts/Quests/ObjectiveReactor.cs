using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RPG.Quests;

public class ObjectiveReactor : MonoBehaviour
{
	[Header("Что за цель должна быть выполнена")]
	public string questName;
	public string objectiveReference;

	[Header("Что произойдет при выполнении")]
	public UnityEvent onObjectiveCompleted;

	[Header("Идентификатор реактора")]
	[SerializeField] private string reactorId;

	private const string QUEST_LIST_ERROR = "QuestList не найден в сцене!";
	private const string QUEST_NOT_FOUND_ERROR = "Квест '{0}' не найден!";
	
	private bool hasFired = false;
	private bool isInitialized = false;
	private QuestList questList;
	private Quest targetQuest;

	private void Awake()
	{
		if (string.IsNullOrEmpty(reactorId))
		{
			reactorId = GenerateReactorId();
		}
	}

	private void Start()
	{
		Initialize();
	}

	private void OnEnable()
    {
        if (!isInitialized || hasFired) return;

        // Переподписываемся, но сначала проверяем текущее состояние —
        // задание могло выполниться пока реактор был вне сцены / неактивен
        if (CheckIfAlreadyFired())
        {
            hasFired = true;
            return;
        }

        if (CheckIfObjectiveAlreadyComplete())
        {
            FireReactor();
            return;
        }

        SubscribeToEvents();
    }

	private void OnDisable()
	{
		UnsubscribeFromEvents();
	}

	private void Initialize()
    {
        if (isInitialized) return;
    
        if (!ValidateComponents())
            return;
    
        if (CheckIfAlreadyFired())
        {
            hasFired = true;
            isInitialized = true;
            return;
        }
    
        isInitialized = true;
    
        if (CheckIfObjectiveAlreadyComplete())
        {
            FireReactor();
            return;
        }
    
        SubscribeToEvents();
    }

	private bool ValidateComponents()
	{
		questList = FindObjectOfType<QuestList>();
		if (questList == null)
		{
			Debug.LogError($"ObjectiveReactor на {gameObject.name}: {QUEST_LIST_ERROR}", this);
			return false;
		}

		targetQuest = Quest.GetByName(questName);
		if (targetQuest == null)
		{
			Debug.LogError($"ObjectiveReactor на {gameObject.name}: {string.Format(QUEST_NOT_FOUND_ERROR, questName)}", this);
			return false;
		}

		return true;
	}

	private bool CheckIfAlreadyFired()
	{
		QuestStatus status = questList.GetQuestStatus(targetQuest);
		if (status != null && status.IsReactorFired(reactorId))
		{
			Debug.Log($"ObjectiveReactor на {gameObject.name}: Реактор {reactorId} уже срабатывал ранее.", this);
			return true;
		}
		return false;
	}

	private bool CheckIfObjectiveAlreadyComplete()
	{
		QuestStatus status = questList.GetQuestStatus(targetQuest);
		return status != null && status.IsObjectiveComplete(objectiveReference);
	}

	private void SubscribeToEvents()
	{
		if (hasFired || !isInitialized) return;

		if (questList != null)
		{
			questList.onUpdate += OnQuestUpdate;
		}

		if (QuestEvents.Instance != null)
		{
			QuestEvents.Instance.onObjectiveCompleted += OnObjectiveCompleted;
		}
	}

	private void UnsubscribeFromEvents()
	{
		if (questList != null)
		{
			questList.onUpdate -= OnQuestUpdate;
		}

		if (QuestEvents.Instance != null)
		{
			QuestEvents.Instance.onObjectiveCompleted -= OnObjectiveCompleted;
		}
	}

	private void OnQuestUpdate()
	{
		if (hasFired || targetQuest == null) return;

		QuestStatus status = questList.GetQuestStatus(targetQuest);
		if (status == null) return;

		if (status.IsReactorFired(reactorId))
		{
			hasFired = true;
			UnsubscribeFromEvents();
			return;
		}

		if (status.IsObjectiveComplete(objectiveReference))
		{
			FireReactor();
		}
	}

	private void OnObjectiveCompleted(Quest quest, string objRef)
	{
		if (hasFired) return;
		if (quest != targetQuest || objRef != objectiveReference) return;

		QuestStatus status = questList.GetQuestStatus(quest);
		if (status != null && !status.IsReactorFired(reactorId))
		{
			FireReactor();
		}
	}

	private void FireReactor()
	{
		if (hasFired) return;
		
		hasFired = true;

		MarkReactorAsFired();
		
		onObjectiveCompleted?.Invoke();
		
		Debug.Log($"ObjectiveReactor на {gameObject.name}: Цель '{objectiveReference}' квеста '{questName}' выполнена! ID реактора: {reactorId}", this);
		
		UnsubscribeFromEvents();
	}

	private void MarkReactorAsFired()
	{
		if (targetQuest != null && questList != null)
		{
			QuestStatus status = questList.GetQuestStatus(targetQuest);
			status?.MarkReactorFired(reactorId);
		}
	}

	private string GenerateReactorId()
	{
		string sceneName = SceneManager.GetActiveScene().name;
		return $"{sceneName}_{gameObject.name}_{questName}_{objectiveReference}";
	}

#if UNITY_EDITOR
	[ContextMenu("Сгенерировать новый ID реактора")]
	private void RegenerateReactorId()
	{
		reactorId = GenerateReactorId();
		Debug.Log($"Новый ID реактора: {reactorId}", this);
	}
#endif
}
