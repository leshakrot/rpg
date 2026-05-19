using UnityEngine;
using UnityEditor;
using System.IO;

namespace RPG.HomesteadTemplate.Editor
{
    /// <summary>
    /// Дополнительные пункты меню для работы с шаблоном Homestead.
    /// </summary>
    public static class HomesteadTemplateMenu
    {
        [MenuItem("Tools/Homestead/Template/Open Documentation", priority = 100)]
        public static void OpenDocumentation()
        {
            string path = "Assets/Scripts/HomesteadTemplate/README.md";
            if (File.Exists(path))
            {
                Application.OpenURL("file://" + Path.GetFullPath(path));
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Документация не найдена",
                    "Сначала создайте шаблон через:\nTools → Homestead → Create Template Scene",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Homestead/Template/Open Quick Start", priority = 101)]
        public static void OpenQuickStart()
        {
            string path = "Assets/Scripts/HomesteadTemplate/QUICKSTART.md";
            if (File.Exists(path))
            {
                Application.OpenURL("file://" + Path.GetFullPath(path));
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Quick Start не найден",
                    "Сначала создайте шаблон через:\nTools → Homestead → Create Template Scene",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Homestead/Template/Open Template Scene", priority = 102)]
        public static void OpenTemplateScene()
        {
            string scenePath = "Assets/HomesteadTemplate/Scenes/HomesteadTemplate.unity";
            if (File.Exists(scenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Сцена не найдена",
                    "Сначала создайте шаблон через:\nTools → Homestead → Create Template Scene",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Homestead/Template/Open Template Folder", priority = 103)]
        public static void OpenTemplateFolder()
        {
            string folderPath = "Assets/HomesteadTemplate";
            if (Directory.Exists(folderPath))
            {
                EditorUtility.RevealInFinder(folderPath);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Папка не найдена",
                    "Сначала создайте шаблон через:\nTools → Homestead → Create Template Scene",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/Homestead/Template/Show Template Info", priority = 104)]
        public static void ShowTemplateInfo()
        {
            string folderPath = "Assets/HomesteadTemplate";
            
            if (!Directory.Exists(folderPath))
            {
                EditorUtility.DisplayDialog(
                    "Шаблон не создан",
                    "Создайте шаблон через:\nTools → Homestead → Create Template Scene",
                    "OK"
                );
                return;
            }

            // Подсчитываем файлы
            int buildingCount = Directory.GetFiles(Path.Combine(folderPath, "Buildings"), "*.asset").Length;
            int requirementCount = Directory.GetFiles(Path.Combine(folderPath, "Requirements"), "*.asset").Length;
            int prefabCount = Directory.GetFiles(Path.Combine(folderPath, "Prefabs"), "*.prefab").Length;
            int materialCount = Directory.GetFiles(Path.Combine(folderPath, "Materials"), "*.mat").Length;

            string info = $"Homestead Template - Информация\n\n" +
                         $"📁 Структура:\n" +
                         $"  • Зданий: {buildingCount}\n" +
                         $"  • Требований: {requirementCount}\n" +
                         $"  • Префабов: {prefabCount}\n" +
                         $"  • Материалов: {materialCount}\n\n" +
                         $"🎯 Возможности:\n" +
                         $"  • Строительство и улучшение\n" +
                         $"  • Бесплатная реконструкция\n" +
                         $"  • Система пререквизитов\n" +
                         $"  • Лимиты на количество\n" +
                         $"  • Логические операторы\n" +
                         $"  • Интеграция с уровнями\n\n" +
                         $"📖 Документация:\n" +
                         $"  Tools → Homestead → Template → Open Documentation";

            EditorUtility.DisplayDialog("Homestead Template", info, "OK");
        }

        // Валидация пунктов меню
        [MenuItem("Tools/Homestead/Template/Open Documentation", true)]
        [MenuItem("Tools/Homestead/Template/Open Quick Start", true)]
        [MenuItem("Tools/Homestead/Template/Open Template Scene", true)]
        [MenuItem("Tools/Homestead/Template/Open Template Folder", true)]
        [MenuItem("Tools/Homestead/Template/Show Template Info", true)]
        public static bool ValidateTemplateExists()
        {
            // Эти пункты всегда доступны, но покажут сообщение если шаблон не создан
            return true;
        }
    }
}
