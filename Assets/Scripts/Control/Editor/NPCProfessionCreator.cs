using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace RPG.Control.Editor
{
    public class NPCProfessionCreator : EditorWindow
    {
        [MenuItem("RPG Tools/Create NPC Professions")]
        public static void ShowWindow()
        {
            GetWindow<NPCProfessionCreator>("NPC Profession Creator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Создание профессий NPC", EditorStyles.boldLabel);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Создать все базовые профессии"))
            {
                CreateAllBasicProfessions();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Создать профессию рыбака"))
            {
                CreateFishermanProfession();
            }
            
            if (GUILayout.Button("Создать профессию кузнеца"))
            {
                CreateBlacksmithProfession();
            }
            
            if (GUILayout.Button("Создать профессию торговца"))
            {
                CreateMerchantProfession();
            }
            
            if (GUILayout.Button("Создать профессию фермера"))
            {
                CreateFarmerProfession();
            }
            
            if (GUILayout.Button("Создать профессию стражника"))
            {
                CreateGuardProfession();
            }
        }

        private void CreateAllBasicProfessions()
        {
            CreateFishermanProfession();
            CreateBlacksmithProfession();
            CreateMerchantProfession();
            CreateFarmerProfession();
            CreateGuardProfession();
            
            Debug.Log("Все базовые профессии созданы!");
        }

        private void CreateFishermanProfession()
        {
            var profession = CreateProfessionAsset("Fisherman", "Рыбак", 
                "Ловит рыбу на пруду, поставляет свежую рыбу в рестораны и на рынок",
                "Ловит рыбу", "Ловит рыбу с помощью удочки и сетей");

            // Расписание рыбака
            profession.defaultSchedule = new List<ProfessionActivity>
            {
                new ProfessionActivity { activityName = "Просыпается", startHour = 5, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Sleep, description = "Ранний подъем для рыбалки" },
                new ProfessionActivity { activityName = "Подготовка снаряжения", startHour = 6, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Проверяет снасти и приманки" },
                new ProfessionActivity { activityName = "Утренняя рыбалка", startHour = 7, startMinute = 0, durationMinutes = 300, activityType = ActivityType.Work, description = "Основное время рыбалки" },
                new ProfessionActivity { activityName = "Обед", startHour = 12, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Обеденный перерыв" },
                new ProfessionActivity { activityName = "Послеобеденная рыбалка", startHour = 13, startMinute = 0, durationMinutes = 300, activityType = ActivityType.Work, description = "Продолжает ловить рыбу" },
                new ProfessionActivity { activityName = "Общается с рыбаками", startHour = 18, startMinute = 0, durationMinutes = 120, activityType = ActivityType.Socialize, description = "Обменивается опытом с другими рыбаками" },
                new ProfessionActivity { activityName = "Ужин", startHour = 20, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Ужинает дома" },
                new ProfessionActivity { activityName = "Спит", startHour = 22, startMinute = 0, durationMinutes = 420, activityType = ActivityType.Sleep, description = "Отдыхает перед новым днем" }
            };

            profession.workAnimationTrigger = "StartFishing";
            
            EditorUtility.SetDirty(profession);
            AssetDatabase.SaveAssets();
        }

        private void CreateBlacksmithProfession()
        {
            var profession = CreateProfessionAsset("Blacksmith", "Кузнец", 
                "Кует оружие и доспехи, ремонтирует инструменты",
                "Кует металл", "Работает с молотом и наковальней, создает металлические изделия");

            profession.defaultSchedule = new List<ProfessionActivity>
            {
                new ProfessionActivity { activityName = "Просыпается", startHour = 6, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Sleep, description = "Подъем" },
                new ProfessionActivity { activityName = "Разжигает горн", startHour = 7, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Подготовка кузницы" },
                new ProfessionActivity { activityName = "Утренняя ковка", startHour = 8, startMinute = 0, durationMinutes = 240, activityType = ActivityType.Work, description = "Основная работа" },
                new ProfessionActivity { activityName = "Обед", startHour = 12, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Обеденный перерыв" },
                new ProfessionActivity { activityName = "Дневная ковка", startHour = 13, startMinute = 0, durationMinutes = 300, activityType = ActivityType.Work, description = "Продолжает работу" },
                new ProfessionActivity { activityName = "Закрывает кузницу", startHour = 18, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Уборка и закрытие" },
                new ProfessionActivity { activityName = "Ужин", startHour = 19, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Ужинает" },
                new ProfessionActivity { activityName = "Отдых", startHour = 20, startMinute = 0, durationMinutes = 120, activityType = ActivityType.Socialize, description = "Отдыхает в таверне" },
                new ProfessionActivity { activityName = "Спит", startHour = 22, startMinute = 0, durationMinutes = 480, activityType = ActivityType.Sleep, description = "Сон" }
            };

            profession.workAnimationTrigger = "StartForging";
        }

        private void CreateMerchantProfession()
        {
            var profession = CreateProfessionAsset("Merchant", "Торговец", 
                "Продает товары в своем магазине, торгуется с покупателями",
                "Торгует", "Показывает товары, ведет переговоры с покупателями");

            profession.defaultSchedule = new List<ProfessionActivity>
            {
                new ProfessionActivity { activityName = "Просыпается", startHour = 7, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Sleep, description = "Подъем" },
                new ProfessionActivity { activityName = "Открывает магазин", startHour = 8, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Подготовка к торговле" },
                new ProfessionActivity { activityName = "Утренняя торговля", startHour = 9, startMinute = 0, durationMinutes = 180, activityType = ActivityType.Work, description = "Обслуживает покупателей" },
                new ProfessionActivity { activityName = "Обед", startHour = 12, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Обеденный перерыв" },
                new ProfessionActivity { activityName = "Дневная торговля", startHour = 13, startMinute = 0, durationMinutes = 360, activityType = ActivityType.Work, description = "Продолжает торговлю" },
                new ProfessionActivity { activityName = "Закрывает магазин", startHour = 19, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Подсчет выручки" },
                new ProfessionActivity { activityName = "Ужин", startHour = 20, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Ужинает" },
                new ProfessionActivity { activityName = "Спит", startHour = 23, startMinute = 0, durationMinutes = 480, activityType = ActivityType.Sleep, description = "Сон" }
            };

            profession.workAnimationTrigger = "StartTrading";
        }

        private void CreateFarmerProfession()
        {
            var profession = CreateProfessionAsset("Farmer", "Фермер", 
                "Выращивает урожай, ухаживает за животными",
                "Работает на ферме", "Пашет землю, сажает семена, ухаживает за животными");

            profession.defaultSchedule = new List<ProfessionActivity>
            {
                new ProfessionActivity { activityName = "Просыпается", startHour = 5, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Sleep, description = "Ранний подъем" },
                new ProfessionActivity { activityName = "Уход за животными", startHour = 6, startMinute = 0, durationMinutes = 120, activityType = ActivityType.Work, description = "Кормит и поит животных" },
                new ProfessionActivity { activityName = "Завтрак", startHour = 8, startMinute = 0, durationMinutes = 30, activityType = ActivityType.Eat, description = "Завтракает" },
                new ProfessionActivity { activityName = "Полевые работы", startHour = 8, startMinute = 30, durationMinutes = 210, activityType = ActivityType.Work, description = "Работает в поле" },
                new ProfessionActivity { activityName = "Обед", startHour = 12, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Обеденный перерыв" },
                new ProfessionActivity { activityName = "Послеобеденные работы", startHour = 13, startMinute = 0, durationMinutes = 240, activityType = ActivityType.Work, description = "Продолжает работу" },
                new ProfessionActivity { activityName = "Вечерний уход за животными", startHour = 17, startMinute = 0, durationMinutes = 120, activityType = ActivityType.Work, description = "Вечернее кормление" },
                new ProfessionActivity { activityName = "Ужин", startHour = 19, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Ужинает" },
                new ProfessionActivity { activityName = "Спит", startHour = 21, startMinute = 0, durationMinutes = 480, activityType = ActivityType.Sleep, description = "Сон" }
            };

            profession.workAnimationTrigger = "StartFarming";
        }

        private void CreateGuardProfession()
        {
            var profession = CreateProfessionAsset("Guard", "Стражник", 
                "Охраняет город, патрулирует улицы",
                "Патрулирует", "Следит за порядком, патрулирует территорию");

            profession.defaultSchedule = new List<ProfessionActivity>
            {
                new ProfessionActivity { activityName = "Смена караула", startHour = 6, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Заступает на дежурство" },
                new ProfessionActivity { activityName = "Утреннее патрулирование", startHour = 7, startMinute = 0, durationMinutes = 300, activityType = ActivityType.Patrol, description = "Патрулирует район" },
                new ProfessionActivity { activityName = "Обед", startHour = 12, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Обеденный перерыв" },
                new ProfessionActivity { activityName = "Дневное дежурство", startHour = 13, startMinute = 0, durationMinutes = 300, activityType = ActivityType.Patrol, description = "Продолжает патрулирование" },
                new ProfessionActivity { activityName = "Конец смены", startHour = 18, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Work, description = "Передает дежурство" },
                new ProfessionActivity { activityName = "Ужин", startHour = 19, startMinute = 0, durationMinutes = 60, activityType = ActivityType.Eat, description = "Ужинает" },
                new ProfessionActivity { activityName = "Отдых", startHour = 20, startMinute = 0, durationMinutes = 120, activityType = ActivityType.Socialize, description = "Отдыхает" },
                new ProfessionActivity { activityName = "Спит", startHour = 22, startMinute = 0, durationMinutes = 480, activityType = ActivityType.Sleep, description = "Сон" }
            };

            profession.workAnimationTrigger = "StartPatrol";
        }

        private NPCProfession CreateProfessionAsset(string fileName, string professionName, string description, 
            string workActivityName, string workDescription)
        {
            // Создаем папку если её нет
            string folderPath = "Assets/Resources/NPC";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "NPC");
            }
            
            folderPath = "Assets/Resources/NPC/Professions";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources/NPC", "Professions");
            }

            // Создаем ScriptableObject
            var profession = ScriptableObject.CreateInstance<NPCProfession>();
            profession.professionName = professionName;
            profession.description = description;
            profession.workActivityName = workActivityName;
            profession.workDescription = workDescription;

            // Сохраняем как asset
            string assetPath = $"{folderPath}/{fileName}.asset";
            AssetDatabase.CreateAsset(profession, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Создана профессия: {professionName} в {assetPath}");
            
            return profession;
        }
    }
}