using GameDevTV.Utils;
using System;
using UnityEngine;
using GameDevTV.Saving;
using Newtonsoft.Json;

namespace RPG.Stats
{
	public class BaseStats : MonoBehaviour, ISaveable 
	{
		[Range(1, 99)]
		[SerializeField] private int _startingLevel = 1;
		[SerializeField] private CharacterClass _characterClass;
		[SerializeField] private Progression _progression = null;
		[SerializeField] private GameObject _levelUpParticleEffect = null;
		[SerializeField] private bool _shouldUseModifiers = false;

		private Experience _experience;

		// Вызывается только при реальном повышении уровня (регенерирует здоровье)
		public event Action onLevelUp;
		// Вызывается при загрузке/обновлении UI без регенерации здоровья
		public event Action onStatsRefreshed;
		public event Action<Stat> onStatChanged;

		LazyValue<int> _currentLevel;

		private void Awake()
		{
			_experience = GetComponent<Experience>();
			_currentLevel = new LazyValue<int>(CalculateLevel);
		}

		private void Start()
		{
			_currentLevel.ForceInit();
            
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
            
			if (_progression != null)
			{
				_progression.OnStatProgressionChanged -= OnProgressionChanged;
			}
		}

		private void OnProgressionChanged(CharacterClass characterClass, Stat stat)
		{
			if (characterClass == _characterClass)
			{
				onStatChanged?.Invoke(stat);
			}
		}

		private void UpdateLevel()
		{
			int newLevel = CalculateLevel();
			if (newLevel > _currentLevel.value)
			{
				_currentLevel.value = newLevel;
				LevelUpEffect();
				onLevelUp?.Invoke(); // реальный левел-ап — регенерация здоровья уместна
                
				NotifyAllStatsChanged();
			}
		}
        
		private void NotifyAllStatsChanged()
		{
			if (_progression == null) return;
            
			foreach (Stat stat in _progression.GetAvailableStats(_characterClass))
			{
				onStatChanged?.Invoke(stat);
			}
		}

		private void LevelUpEffect()
		{
			if (_levelUpParticleEffect != null)
			{
				Instantiate(_levelUpParticleEffect, transform);
			}
		}

		public float GetStat(Stat stat)
		{
			return (GetBaseStat(stat) + GetAdditiveModifier(stat)) * (1 + GetPercentageModifiers(stat) / 100);
		}

		private float GetBaseStat(Stat stat)
		{
			if (_progression == null) return 0;
            
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

		public int GetLevel()
		{
			return _currentLevel.value;
		}
        
		public float GetXPToLevelUp(int level)
		{
			if (level <= 1) return 0;
            
			if (_progression != null)
			{
				return _progression.GetStat(Stat.ExperienceToLevelUp, _characterClass, level - 1);
			}
            
			return 0;
		}
        
		public float GetXPRequiredForLevelRange(int targetLevel)
		{
			if (targetLevel <= GetLevel()) return 0;
            
			float xpForCurrentLevel = GetXPToLevelUp(GetLevel());
			float xpForTargetLevel = GetXPToLevelUp(targetLevel);
            
			return xpForTargetLevel - xpForCurrentLevel;
		}

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
            
			if (penultimateLevel == 0) return _startingLevel;
            
			for (int level = 1; level <= penultimateLevel; level++)
			{
				float XPToLevelUp = _progression.GetStat(Stat.ExperienceToLevelUp, _characterClass, level);
				if (XPToLevelUp > currentXP)
				{
					return level;
				}
			}

			return penultimateLevel + 1;
		}

		/// <summary>
		/// Вызывается после загрузки сохранения.
		/// Пересчитывает уровень и уведомляет UI через onStatsRefreshed
		/// БЕЗ регенерации здоровья (не вызывает onLevelUp).
		/// </summary>
		public void RefreshStats()
		{
			_currentLevel.value = CalculateLevel();
			onStatsRefreshed?.Invoke(); // только UI, здоровье не трогаем
		}
		
		public object CaptureState()
		{
			return null;
		}

		public void RestoreState(object state)
		{
			// Пересчитываем уровень на основе восстановленного опыта.
			// onStatsRefreshed уведомляет UI, но НЕ регенерирует здоровье.
			_currentLevel.value = CalculateLevel();
			onStatsRefreshed?.Invoke();
		}
	}
}
