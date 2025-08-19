using UnityEngine;
using RPG.Stats;
using System;
using System.Collections;
using UnityEngine.UI;

namespace RPG.UI
{
    public class PlayerExperienceBar : StatusBar
    {
        [SerializeField] private float levelUpAnimationDuration = 0.5f;
        [SerializeField] private Color levelUpFlashColor = Color.yellow;
        
	    [SerializeField] private Experience playerExperience;
	    [SerializeField] private BaseStats playerStats;
        
        private Color originalColor;
        private Coroutine levelUpAnimationCoroutine;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Опыт";
            barColor = new Color(0.8f, 0.8f, 0.2f); // Золотисто-желтый цвет
            originalColor = barColor;
            
            // Автоматически находим компоненты игрока
            FindPlayerComponents();
        }
        
        private void FindPlayerComponents()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (playerExperience == null)
                    playerExperience = player.GetComponent<Experience>();
                if (playerStats == null)
                    playerStats = player.GetComponent<BaseStats>();
            }
            else
            {
                Debug.LogWarning("PlayerExperienceBar: Игрок не найден! Убедитесь, что игрок имеет тег 'Player'.");
            }
        }

        private void OnEnable()
        {
            if (playerExperience != null)
            {
                playerExperience.onExperienceGained += UpdateExperienceBar;
            }
            
            if (playerStats != null)
            {
                playerStats.onLevelUp += OnLevelUp;
                playerStats.onStatChanged += OnStatChanged;
            }
        }
        
        private void OnDisable()
        {
            if (playerExperience != null)
            {
                playerExperience.onExperienceGained -= UpdateExperienceBar;
            }
            
            if (playerStats != null)
            {
                playerStats.onLevelUp -= OnLevelUp;
                playerStats.onStatChanged -= OnStatChanged;
            }
            
            if (levelUpAnimationCoroutine != null)
            {
                StopCoroutine(levelUpAnimationCoroutine);
                levelUpAnimationCoroutine = null;
            }
        }
        
        // Реакция на изменение статистики
        private void OnStatChanged(Stat stat)
        {
            // Нас интересуют только изменения в статистике опыта
            if (stat == Stat.ExperienceToLevelUp)
            {
                UpdateExperienceBar();
            }
        }
        
        private void OnLevelUp()
        {
            // Принудительно обновляем шкалу опыта с небольшой задержкой
            // Это гарантирует, что мы получим актуальные данные после обновления уровня
            StartCoroutine(UpdateAfterLevelUp());
        }
        
        private IEnumerator UpdateAfterLevelUp()
        {
            // Небольшая задержка, чтобы убедиться, что все значения уровня обновились
            yield return new WaitForEndOfFrame();
            
            // Обновляем отображение шкалы
            UpdateExperienceBar();
            
            // Запускаем анимацию повышения уровня
            if (levelUpAnimationCoroutine != null)
            {
                StopCoroutine(levelUpAnimationCoroutine);
            }
            levelUpAnimationCoroutine = StartCoroutine(PlayLevelUpAnimation());
        }
        
        private IEnumerator PlayLevelUpAnimation()
        {
            // Находим Image компонент для изменения цвета
            var barImage = foreground.GetComponent<UnityEngine.UI.Image>();
            if (barImage == null) yield break;
            
            // Запоминаем оригинальный цвет
            Color startColor = barImage.color;
            
            // Анимация мигания полосы опыта
            float elapsed = 0f;
            while (elapsed < levelUpAnimationDuration)
            {
                // Пульсирующий эффект, переходящий между исходным и целевым цветом
                float t = Mathf.PingPong(elapsed * 4f, 1f);
                barImage.color = Color.Lerp(startColor, levelUpFlashColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Возвращаем оригинальный цвет
            barImage.color = startColor;
            levelUpAnimationCoroutine = null;
        }
        
        private void UpdateExperienceBar()
        {
            // Обновление шкалы опыта при получении опыта или повышении уровня
            if (playerExperience != null && playerStats != null)
            {
                UpdateExperienceDisplay();
            }
        }
        
        // Публичный метод для принудительного обновления UI
        public void ForceRefresh()
        {
            // Повторно находим компоненты игрока на случай, если они изменились
            FindPlayerComponents();
            UpdateExperienceBar();
        }
        
        protected override void Update()
        {
            if (playerExperience != null && playerStats != null)
            {
                UpdateExperienceDisplay();
            }
            
            base.Update();
        }
        
        // Обновление отображения опыта на основе текущего состояния
        private void UpdateExperienceDisplay()
        {
            try
            {
                // Получаем текущий уровень и текущий опыт
                int currentLevel = playerStats.GetLevel();
                float currentXP = playerExperience.GetPoints();
                
                // Получаем значения опыта для текущего и следующего уровня
                float xpForCurrentLevel = playerStats.GetXPToLevelUp(currentLevel);
                float xpForNextLevel = playerStats.GetXPToLevelUp(currentLevel + 1);
                
                // Вычисляем оставшийся опыт и требуемый опыт для следующего уровня
                float remainingXP = currentXP - xpForCurrentLevel;
                float requiredXP = xpForNextLevel - xpForCurrentLevel;
                
                // Если вдруг есть проблема с делением на ноль
                if (requiredXP <= 0)
                {
                    // Максимальный уровень или ошибка в данных прогрессии
                    requiredXP = 1.0f;
                    remainingXP = 1.0f; // Для максимального уровня показываем полную полосу
                }
                
                // Расчет прогресса до следующего уровня
                float levelProgress = remainingXP / requiredXP;
                
                // Устанавливаем заполнение полосы опыта
                SetFraction(Mathf.Clamp01(levelProgress));
                
                // Обновляем текст шкалы: текущий опыт из необходимого для следующего уровня
                SetValueText(String.Format("{0:0}/{1:0}", 
                    Mathf.FloorToInt(remainingXP), 
                    Mathf.FloorToInt(requiredXP)));
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при обновлении шкалы опыта: {e.Message}");
            }
        }
    }
} 