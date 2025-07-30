using UnityEngine;
using UnityEditor;
using RPG.Control;

namespace RPG.Harvesting
{
    /// <summary>
    /// Редакторский скрипт для настройки системы событий анимации добычи
    /// </summary>
    public class SetupHarvestingAnimationEvents : EditorWindow
    {
        [MenuItem("Tools/Harvesting/Setup Animation Events")]
        public static void SetupAnimationEvents()
        {
            // Находим игрока
            var player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("Игрок не найден на сцене!");
                return;
            }
            
            // Добавляем компонент событий анимации
            var animationEvents = player.GetComponent<HarvestingAnimationEvents>();
            if (animationEvents == null)
            {
                animationEvents = player.gameObject.AddComponent<HarvestingAnimationEvents>();
                Debug.Log("Добавлен компонент HarvestingAnimationEvents на игрока");
            }
            
            // Убеждаемся что есть AudioManager
            var audioManager = player.GetComponent<HarvestingAudioManager>();
            if (audioManager == null)
            {
                audioManager = player.gameObject.AddComponent<HarvestingAudioManager>();
                Debug.Log("Добавлен компонент HarvestingAudioManager на игрока");
            }
            
            // Убеждаемся что есть VFX Manager
            var vfxManager = FindObjectOfType<HarvestingVFXManager>();
            if (vfxManager == null)
            {
                var vfxManagerGO = new GameObject("HarvestingVFXManager");
                vfxManager = vfxManagerGO.AddComponent<HarvestingVFXManager>();
                Debug.Log("Создан HarvestingVFXManager в сцене");
            }
            
            // Настраиваем ссылки в AnimationEvents
            var serializedObject = new SerializedObject(animationEvents);
            var audioManagerProperty = serializedObject.FindProperty("audioManager");
            var vfxManagerProperty = serializedObject.FindProperty("vfxManager");
            
            if (audioManagerProperty.objectReferenceValue == null)
            {
                audioManagerProperty.objectReferenceValue = audioManager;
            }
            
            if (vfxManagerProperty.objectReferenceValue == null)
            {
                vfxManagerProperty.objectReferenceValue = vfxManager;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log("Система событий анимации добычи настроена!");
            Debug.Log("Теперь добавьте AnimationEvent в анимацию добычи:");
            Debug.Log("- OnHarvestStart() - в начале анимации");
            Debug.Log("- OnHarvestHit() - в момент удара");
            Debug.Log("- OnHarvestEnd() - в конце анимации");
            Debug.Log("- OnResourceDepleted() - при истощении ресурса");
        }
        
        [MenuItem("Tools/Harvesting/Show Animation Events Info")]
        public static void ShowAnimationEventsInfo()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("Игрок не найден на сцене!");
                return;
            }
            
            var animationEvents = player.GetComponent<HarvestingAnimationEvents>();
            if (animationEvents == null)
            {
                Debug.LogWarning("Компонент HarvestingAnimationEvents не найден на игроке!");
                return;
            }
            
            Debug.Log("=== Информация о системе событий анимации ===");
            Debug.Log($"Текущий инструмент: {animationEvents.GetCurrentToolType()}");
            Debug.Log($"Текущий ресурс: {animationEvents.GetCurrentResource()?.ResourceName ?? "Нет"}");
            Debug.Log($"Позиция добычи: {animationEvents.GetHarvestPosition()}");
            Debug.Log("");
            Debug.Log("Доступные методы для AnimationEvent:");
            Debug.Log("- OnHarvestStart() - звук начала добычи");
            Debug.Log("- OnHarvestHit() - звук удара/добычи ресурса");
            Debug.Log("- OnHarvestEnd() - звук завершения добычи");
            Debug.Log("- OnResourceDepleted() - звук истощения ресурса");
        }
    }
} 