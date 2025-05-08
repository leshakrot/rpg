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
        
        private Experience playerExperience;
        private BaseStats playerStats;
        private Color originalColor;
        private Coroutine levelUpAnimationCoroutine;
        
        protected override void Awake()
        {
            base.Awake();
            barTitle = "Опыт";
            barColor = new Color(0.8f, 0.8f, 0.2f); // Золотисто-желтый цвет
            originalColor = barColor;
            
            // Находим компоненты игрока
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerExperience = player.GetComponent<Experience>();
                playerStats = player.GetComponent<BaseStats>();
                
                if (playerExperience == null || playerStats == null)
                {
                    Debug.LogError("PlayerExperienceBar: Не удалось найти необходимые компоненты у игрока!");
                }
            }
            else
            {
                Debug.LogError("PlayerExperienceBar: Не удалось найти объект игрока!");
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
            }
            
            if (levelUpAnimationCoroutine != null)
            {
                StopCoroutine(levelUpAnimationCoroutine);
                levelUpAnimationCoroutine = null;
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
            
            // Дополнительная проверка и переопределение значений шкалы для гарантии корректного отображения
            if (playerStats != null && playerExperience != null)
            {
                int currentLevel = playerStats.GetLevel();
                float currentXP = playerExperience.GetPoints();
                
                // ИСПРАВЛЕНО: Корректно получаем значения опыта для уровней
                float xpForCurrentLevel = GetTotalXPForLevel(currentLevel);
                float xpForNextLevel = GetTotalXPForLevel(currentLevel + 1);
                
                float remainingXP = currentXP - xpForCurrentLevel;
                float requiredXP = xpForNextLevel - xpForCurrentLevel;
                
                if (requiredXP > 0)
                {
                    float levelProgress = remainingXP / requiredXP;
                    currentFraction = Mathf.Clamp01(levelProgress);
                    targetFraction = currentFraction;
                    
                    // Мгновенное обновление заполнения шкалы (без анимации)
                    if (foreground != null)
                    {
                        foreground.localScale = new Vector3(currentFraction, 1, 1);
                    }
                    
                    // Принудительное обновление текста
                    SetValueText(String.Format("{0:0}/{1:0}", 
                        Mathf.FloorToInt(remainingXP), 
                        Mathf.FloorToInt(requiredXP)));
                }
            }
            
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
        
        protected override void Update()
        {
            if (playerExperience != null && playerStats != null)
            {
                UpdateExperienceDisplay();
            }
            
            base.Update();
        }
        
        // ИСПРАВЛЕНО: Новый метод для получения ОБЩЕГО опыта, необходимого для достижения уровня
        private float GetTotalXPForLevel(int level)
        {
            if (level <= 1) return 0; // Для 1-го уровня требуется 0 опыта
            
            // Непосредственно получаем значение из Progression через BaseStats
            return playerStats.GetXPToLevelUp(level);
        }
        
        // Обновление отображения опыта на основе текущего состояния
        private void UpdateExperienceDisplay()
        {
            try
            {
                // Получаем текущий уровень и текущий опыт
                int currentLevel = playerStats.GetLevel();
                float currentXP = playerExperience.GetPoints();
                
                // ИСПРАВЛЕНО: Корректно получаем значения опыта для уровней
                float xpForCurrentLevel = GetTotalXPForLevel(currentLevel);
                float xpForNextLevel = GetTotalXPForLevel(currentLevel + 1);
                
                // Вычисляем оставшийся опыт и требуемый опыт для следующего уровня
                float remainingXP = currentXP - xpForCurrentLevel;
                float requiredXP = xpForNextLevel - xpForCurrentLevel;
                
                // Если вдруг есть проблема с делением на ноль
                if (requiredXP <= 0)
                {
                    Debug.LogWarning($"PlayerExperienceBar: requiredXP равен {requiredXP}, что может вызвать деление на ноль. Используем 1.0 вместо этого.");
                    requiredXP = 1.0f;
                }
                
                // Расчет прогресса до следующего уровня
                float levelProgress = remainingXP / requiredXP;
                
                // Принудительно устанавливаем значение (без сглаживания)
                currentFraction = Mathf.Clamp01(levelProgress);
                targetFraction = currentFraction;
                
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