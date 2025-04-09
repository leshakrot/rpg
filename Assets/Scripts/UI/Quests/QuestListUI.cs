using RPG.Quests;
using UnityEngine;
using UnityEngine.UI; // Убедись, что это пространство имен подключено

namespace RPG.UI.Quests
{
	public class QuestListUI : MonoBehaviour
	{
		[SerializeField] private QuestItemUI _questPrefab;
		[SerializeField] private Transform _activeQuestsContainer;
		[SerializeField] private Transform _completedQuestsContainer;

		// Ссылки для переключения видимости
		[SerializeField] private Button _activeQuestsToggleButton;
		[SerializeField] private Button _completedQuestsToggleButton;
		[SerializeField] private GameObject _activeQuestsListObject;
		[SerializeField] private GameObject _completedQuestsListObject;

		// --- НОВОЕ: Ссылки для стрелок ---
		[Header("Header Arrows")] // Добавим заголовок для удобства в инспекторе
		[SerializeField] private Image _activeArrowImage;         // Image для стрелки активных квестов
		[SerializeField] private Image _completedArrowImage;      // Image для стрелки выполненных квестов
		[SerializeField] private Sprite _arrowCollapsedSprite;  // Спрайт для свернутого состояния (например, вправо ▶)
		[SerializeField] private Sprite _arrowExpandedSprite;   // Спрайт для развернутого состояния (например, вниз ▼)
		// --- Конец НОВОГО ---


		private QuestList _questList;

		private void Awake()
		{
			if (_activeQuestsToggleButton != null && _activeQuestsListObject != null)
			{
				_activeQuestsToggleButton.onClick.AddListener(ToggleActiveQuests);
			}
			if (_completedQuestsToggleButton != null && _completedQuestsListObject != null)
			{
				_completedQuestsToggleButton.onClick.AddListener(ToggleCompletedQuests);
			}

			// Установим начальное состояние (например, активные развернуты, выполненные свернуты)
			bool initialActiveState = true;  // Можешь поменять на false, если нужно
			bool initialCompletedState = false; // Можешь поменять на true, если нужно

			if(_activeQuestsListObject != null) _activeQuestsListObject.SetActive(initialActiveState);
			if(_completedQuestsListObject != null) _completedQuestsListObject.SetActive(initialCompletedState);

			// --- НОВОЕ: Установка начальных спрайтов стрелок ---
			UpdateArrowSprite(_activeArrowImage, initialActiveState);
			UpdateArrowSprite(_completedArrowImage, initialCompletedState);
			// --- Конец НОВОГО ---
		}

		private void Start()
		{
			_questList = GameObject.FindGameObjectWithTag("Player").GetComponent<QuestList>();
			if (_questList != null)
			{
				_questList.onUpdate += Redraw;
				Redraw();
			}
			else
			{
				Debug.LogError("QuestList не найден на объекте с тегом Player!");
			}
		}

		private void OnDestroy()
		{
			if (_questList != null)
			{
				_questList.onUpdate -= Redraw;
			}
			if (_activeQuestsToggleButton != null)
			{
				_activeQuestsToggleButton.onClick.RemoveListener(ToggleActiveQuests);
			}
			if (_completedQuestsToggleButton != null)
			{
				_completedQuestsToggleButton.onClick.RemoveListener(ToggleCompletedQuests);
			}
		}

		private void Redraw()
		{
			// Очищаем оба контейнера перед перерисовкой
			// (код очистки контейнеров...)
			foreach (Transform item in _activeQuestsContainer)
			{
				Destroy(item.gameObject);
			}
			foreach (Transform item in _completedQuestsContainer)
			{
				Destroy(item.gameObject);
			}

			if (_questList == null) return;

			// Распределяем квесты по контейнерам
			// (код распределения квестов...)
			foreach (QuestStatus status in _questList.GetStatuses())
			{
				QuestItemUI uiInstance;
				if (status.IsComplete())
				{
					uiInstance = Instantiate<QuestItemUI>(_questPrefab, _completedQuestsContainer);
				}
				else
				{
					uiInstance = Instantiate<QuestItemUI>(_questPrefab, _activeQuestsContainer);
				}
				uiInstance.Setup(status);
			}
		}

		public void ToggleActiveQuests()
		{
			if (_activeQuestsListObject != null)
			{
				bool newState = !_activeQuestsListObject.activeSelf;
				_activeQuestsListObject.SetActive(newState);
				// --- НОВОЕ: Обновляем спрайт стрелки ---
				UpdateArrowSprite(_activeArrowImage, newState);
				// --- Конец НОВОГО ---
			}
		}

		public void ToggleCompletedQuests()
		{
			if (_completedQuestsListObject != null)
			{
				bool newState = !_completedQuestsListObject.activeSelf;
				_completedQuestsListObject.SetActive(newState);
				// --- НОВОЕ: Обновляем спрайт стрелки ---
				UpdateArrowSprite(_completedArrowImage, newState);
				// --- Конец НОВОГО ---
			}
		}

		// --- НОВОЕ: Хелпер-метод для обновления спрайта ---
		private void UpdateArrowSprite(Image arrowImage, bool isExpanded)
		{
			if (arrowImage == null) return; // Проверка на случай, если Image не назначен

			if (isExpanded)
			{
				if (_arrowExpandedSprite != null) // Проверка, назначен ли спрайт
				{
					arrowImage.sprite = _arrowExpandedSprite;
				}
				else { Debug.LogWarning("Спрайт для развернутого состояния (Arrow Expanded Sprite) не назначен в QuestListUI!"); }
			}
			else
			{
				if (_arrowCollapsedSprite != null) // Проверка, назначен ли спрайт
				{
					arrowImage.sprite = _arrowCollapsedSprite;
				}
				else { Debug.LogWarning("Спрайт для свернутого состояния (Arrow Collapsed Sprite) не назначен в QuestListUI!"); }
			}
		}
		// --- Конец НОВОГО ---
	}
}