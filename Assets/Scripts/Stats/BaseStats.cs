using GameDevTV.Utils;
using System;
using UnityEngine;
using GameDevTV.Saving; // <-- 1. ДОБАВЛЕНО

namespace RPG.Stats
{
	// 2. ДОБАВЛЕН ИНТЕРФЕЙС ISaveable
	public class BaseStats : MonoBehaviour, ISaveable 
	{
		[Range(1, 99)]
		[SerializeField] private int _startingLevel = 1;
		[SerializeField] private CharacterClass _characterClass;
		[SerializeField] private Progression _progression = null;
		[SerializeField] private GameObject _levelUpParticleEffect = null;
		[SerializeField] private bool _shouldUseModifiers = false;

		private Experience _experience;

		// События для уведомления об изменениях
		public event Action onLevelUp;
		public event Action<Stat> onStatChanged;

		// Ленивая инициализация уровня
		LazyValue<int> _currentLevel;

		private void Awake()
		{
			_experience = GetComponent<Experience>();
			_currentLevel = new LazyValue<int>(CalculateLevel);
		}

		private void Start()
		{
			_currentLevel.ForceInit();
            
			// Подписываемся на событие изменения прогрессии
			if (_progression != null)
			{
				_progression.OnStatProgressionChanged += OnProgressionChanged;
			}
		}

		private void OnEnable()
		{
			if (_experience != null)
			{
				_experience.onExperienceGained += UpdateLevel;
			}
		}

		private void OnDisable()
		{
			if (_experience != null)
			{
				_experience.onExperienceGained -= UpdateLevel;
			}
            
			// Отписываемся от события изменения прогрессии
			if (_progression != null)
			{
				_progression.OnStatProgressionChanged -= OnProgressionChanged;
			}
		}

		/// <summary>
		/// Обработчик события изменения прогрессии
		/// </summary>
		private void OnProgressionChanged(CharacterClass characterClass, Stat stat)
		{
			// Реагируем только на изменения, относящиеся к нашему классу персонажа
			if (characterClass == _characterClass)
			{
				// Уведомляем об изменении статистики
				onStatChanged?.Invoke(stat);
			}
		}

		/// <summary>
		/// Обновляет уровень персонажа при получении опыта
		/// </summary>
		private void UpdateLevel()
		{
			int newLevel = CalculateLevel();
			if (newLevel > _currentLevel.value)
			{
				_currentLevel.value = newLevel;
				LevelUpEffect();
				onLevelUp?.Invoke();
                
				// Уведомляем об изменении всех статистик при повышении уровня
				NotifyAllStatsChanged();
			}
		}
        
		/// <summary>
		/// Уведомляет об изменении всех доступных статистик для данного класса
		/// </summary>
		private void NotifyAllStatsChanged()
		{
			if (_progression == null) return;
            
			foreach (Stat stat in _progression.GetAvailableStats(_characterClass))
			{
				onStatChanged?.Invoke(stat);
			}
		}

		/// <summary>
		/// Создает эффект повышения уровня
		/// </summary>
		private void LevelUpEffect()
		{
			if (_levelUpParticleEffect != null)
			{
				Instantiate(_levelUpParticleEffect, transform);
			}
		}

		/// <summary>
		/// Получает итоговое значение статистики с учетом модификаторов
		/// </summary>
		/// <param name="stat">Тип статистики</param>
		/// <returns>Итоговое значение статистики</returns>
		public float GetStat(Stat stat)
		{
			return (GetBaseStat(stat) + GetAdditiveModifier(stat)) * (1 + GetPercentageModifiers(stat) / 100);
		}

		/// <summary>
		/// Получает базовое значение статистики из таблицы прогрессии
		/// </summary>
		private float GetBaseStat(Stat stat)
		{
			if (_progression == null) return 0;
            
			// FIX: Суммируем очки характеристик со всех уровней для корректного накопления.
			if (stat == Stat.TotalTraitPoints)
			{
				float totalPoints = 0;
				for (int level = 1; level <= GetLevel(); level++)
				{
					totalPoints += _progression.GetStat(Stat.TotalTraitPoints, _characterClass, level);
				}
				return totalPoints;
			}

			return _progression.GetStat(stat, _characterClass, GetLevel());
		}

		/// <summary>
		/// Получает аддитивные модификаторы статистики
		/// </summary>
		private float GetAdditiveModifier(Stat stat)
		{
			if (!_shouldUseModifiers) return 0;
            
			float total = 0;
			foreach (IModifierProvider provider in GetComponents<IModifierProvider>())
			{
				foreach(float modifier in provider.GetAdditiveModifiers(stat))
				{
					total += modifier;
				}
			}
			return total;
		}

		/// <summary>
		/// Получает процентные модификаторы статистики
		/// </summary>
		private float GetPercentageModifiers(Stat stat)
		{
			if (!_shouldUseModifiers) return 0;
            
			float total = 0;
			foreach (IModifierProvider provider in GetComponents<IModifierProvider>())
			{
				foreach (float modifier in provider.GetPercentageModifiers(stat))
				{
					total += modifier;
				}
			}
			return total;
		}

		/// <summary>
		/// Получает текущий уровень персонажа
		/// </summary>
		public int GetLevel()
		{
			return _currentLevel.value;
		}
        
		/// <summary>
		/// Получает общий опыт, необходимый для достижения указанного уровня
		/// </summary>
		/// <param name="level">Целевой уровень</param>
		/// <returns>Необходимое количество опыта или 0, если опыт не требуется</returns>
		public float GetXPToLevelUp(int level)
		{
			if (level <= 1) return 0; // Первый уровень не требует опыта
            
			if (_progression != null)
			{
				return _progression.GetStat(Stat.ExperienceToLevelUp, _characterClass, level - 1);
			}
            
			return 0;
		}
        
		/// <summary>
		/// Получает общее количество опыта, необходимое для перехода от текущего до указанного уровня
		/// </summary>
		/// <param name="targetLevel">Целевой уровень</param>
		/// <returns>Количество опыта, требуемое для достижения целевого уровня</returns>
		public float GetXPRequiredForLevelRange(int targetLevel)
		{
			if (targetLevel <= GetLevel()) return 0;
            
			float xpForCurrentLevel = GetXPToLevelUp(GetLevel());
			float xpForTargetLevel = GetXPToLevelUp(targetLevel);
            
			return xpForTargetLevel - xpForCurrentLevel;
		}

		/// <summary>
		/// Рассчитывает текущий уровень на основе полученного опыта
		/// </summary>
		private int CalculateLevel()
		{
			Experience experience = GetComponent<Experience>();
			if (experience == null) return _startingLevel;    

			float currentXP = experience.GetPoints();
			int penultimateLevel = 0;
            
			if (_progression != null)
			{
				penultimateLevel = _progression.GetLevels(Stat.ExperienceToLevelUp, _characterClass);
			}
            
			// Если нет данных о прогрессии, возвращаем начальный уровень
			if (penultimateLevel == 0) return _startingLevel;
            
			for (int level = 1; level <= penultimateLevel; level++)
			{
				float XPToLevelUp = _progression.GetStat(Stat.ExperienceToLevelUp, _characterClass, level);
				if (XPToLevelUp > currentXP)
				{
					return level;
				}
			}

			// Если опыт превышает последний известный уровень, возвращаем следующий
			return penultimateLevel + 1;
		}

		/// <summary>
		/// Принудительно обновляет расчетные характеристики и уведомляет UI.
		/// Вызывается после загрузки сохранения.
		/// </summary>
		public void RefreshStats()
		{
			// Принудительно пересчитываем уровень на основе восстановленного опыта.
			_currentLevel.value = CalculateLevel();
			
			// Вызываем событие onLevelUp.
			// Это событие - сигнал для всех UI-элементов (здоровье, мана, опыт, уровень)
			// о необходимости обновить свои значения.
			onLevelUp?.Invoke();
		}
		
		// 3. РЕАЛИЗАЦИЯ ISaveable
		public object CaptureState()
		{
			// Уровень является производным от опыта, поэтому нам не нужно сохранять здесь какое-либо состояние.
			return null;
		}

		public void RestoreState(object state)
		{
			// Этот метод вызывается системой сохранения ПОСЛЕ того, как все данные были загружены.
			// К этому моменту компонент Experience уже должен был восстановить свое значение опыта.
			
			// Принудительно пересчитываем уровень на основе восстановленного опыта.
			_currentLevel.value = CalculateLevel();
			
			// Вызываем событие onLevelUp. Хоть уровень и не "повысился" только что,
			// это событие - идеальный способ уведомить все UI-элементы (здоровье, мана, опыт, уровень)
			// о необходимости обновить свои значения.
			onLevelUp?.Invoke();
		}
	}
}