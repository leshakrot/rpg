using UnityEngine;
using UnityEngine.UI;

namespace RPG.Harvesting
{
    public class CreateHarvestBarHUD : MonoBehaviour
    {
        [ContextMenu("Создать HarvestBar HUD")]
        public void CreateHarvestBar()
        {
            // Создаем основной GameObject для HarvestBar
            GameObject harvestBarGO = new GameObject("HarvestBarHUD");
            
            // Добавляем CanvasGroup для управления видимостью
            CanvasGroup canvasGroup = harvestBarGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            // Добавляем RectTransform
            RectTransform rectTransform = harvestBarGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.1f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.1f);
            rectTransform.sizeDelta = new Vector2(300, 50);
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Добавляем Image для фона
            Image backgroundImage = harvestBarGO.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.7f);
            
            // Создаем foreground для прогресс-бара
            GameObject foregroundGO = new GameObject("Foreground");
            foregroundGO.transform.SetParent(harvestBarGO.transform);
            
            RectTransform foregroundRect = foregroundGO.AddComponent<RectTransform>();
            foregroundRect.anchorMin = Vector2.zero;
            foregroundRect.anchorMax = new Vector2(1f, 1f);
            foregroundRect.sizeDelta = Vector2.zero;
            foregroundRect.anchoredPosition = Vector2.zero;
            
            Image foregroundImage = foregroundGO.AddComponent<Image>();
            foregroundImage.color = Color.green;
            
            // Создаем текст для названия ресурса
            GameObject resourceNameGO = new GameObject("ResourceName");
            resourceNameGO.transform.SetParent(harvestBarGO.transform);
            
            RectTransform resourceNameRect = resourceNameGO.AddComponent<RectTransform>();
            resourceNameRect.anchorMin = new Vector2(0, 0.5f);
            resourceNameRect.anchorMax = new Vector2(1f, 1f);
            resourceNameRect.sizeDelta = Vector2.zero;
            resourceNameRect.anchoredPosition = Vector2.zero;
            
            Text resourceNameText = resourceNameGO.AddComponent<Text>();
            resourceNameText.text = "Ресурс";
            resourceNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            resourceNameText.fontSize = 14;
            resourceNameText.color = Color.white;
            resourceNameText.alignment = TextAnchor.MiddleCenter;
            
            // Создаем текст для прогресса
            GameObject progressGO = new GameObject("Progress");
            progressGO.transform.SetParent(harvestBarGO.transform);
            
            RectTransform progressRect = progressGO.AddComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0, 0);
            progressRect.anchorMax = new Vector2(1f, 0.5f);
            progressRect.sizeDelta = Vector2.zero;
            progressRect.anchoredPosition = Vector2.zero;
            
            Text progressText = progressGO.AddComponent<Text>();
            progressText.text = "100%";
            progressText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            progressText.fontSize = 12;
            progressText.color = Color.white;
            progressText.alignment = TextAnchor.MiddleCenter;
            
            // Добавляем компонент HarvestBar
            HarvestBar harvestBar = harvestBarGO.AddComponent<HarvestBar>();
            
            // Настраиваем ссылки через reflection
            var foregroundField = typeof(HarvestBar).GetField("foreground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resourceNameTextField = typeof(HarvestBar).GetField("resourceNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var progressTextField = typeof(HarvestBar).GetField("progressText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var backgroundImageField = typeof(HarvestBar).GetField("backgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canvasGroupField = typeof(HarvestBar).GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (foregroundField != null) foregroundField.SetValue(harvestBar, foregroundRect);
            if (resourceNameTextField != null) resourceNameTextField.SetValue(harvestBar, resourceNameText);
            if (progressTextField != null) progressTextField.SetValue(harvestBar, progressText);
            if (backgroundImageField != null) backgroundImageField.SetValue(harvestBar, backgroundImage);
            if (canvasGroupField != null) canvasGroupField.SetValue(harvestBar, canvasGroup);
            
            Debug.Log("✅ HarvestBar HUD создан! Добавьте его в Canvas.");
            Debug.Log("💡 Перетащите HarvestBarHUD в Canvas и настройте позицию");
            Debug.Log("💡 Убедитесь, что Canvas имеет Screen Space - Overlay режим");
        }
    }
} 