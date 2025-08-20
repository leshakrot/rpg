using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace RPG.Control
{
    /// <summary>
    /// Утилитарный класс для создания расписания дня для мирных NPC
    /// Можно использовать как компонент или для программной настройки
    /// </summary>
    public class NPCScheduleBuilder : MonoBehaviour
    {
        [Header("Target NPC")]
        [SerializeField] private PeacefulNPC _targetNPC;

        [Header("Schedule Templates")]
        [SerializeField] private bool _useVillagerSchedule = false;
        [SerializeField] private bool _useShopkeeperSchedule = false;
        [SerializeField] private bool _useGuardSchedule = false;
        [SerializeField] private bool _useFarmerSchedule = false;

        [Header("Custom Locations")]
        [SerializeField] private Transform _workLocation;
        [SerializeField] private Transform _homeLocation;
        [SerializeField] private Transform _socialLocation;
        [SerializeField] private Transform _restLocation;

        private void Start()
        {
            if (_targetNPC == null)
            {
                _targetNPC = GetComponent<PeacefulNPC>();
            }

            if (_targetNPC != null)
            {
                ApplyScheduleTemplate();
            }
        }

        [ContextMenu("Apply Schedule Template")]
        public void ApplyScheduleTemplate()
        {
            if (_targetNPC == null) return;

            if (_useVillagerSchedule)
            {
                CreateVillagerSchedule();
            }
            else if (_useShopkeeperSchedule)
            {
                CreateShopkeeperSchedule();
            }
            else if (_useGuardSchedule)
            {
                CreateGuardSchedule();
            }
            else if (_useFarmerSchedule)
            {
                CreateFarmerSchedule();
            }
        }

        private void CreateVillagerSchedule()
        {
            var schedule = new List<DailyActivity>();

            // 6:00 - Просыпается
            schedule.Add(CreateActivity("Просыпается", 6, 0, _homeLocation, () => {
                Debug.Log($"{_targetNPC.NPCName} просыпается");
            }));

            // 7:00 - Завтрак
            schedule.Add(CreateActivity("Завтракает", 7, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 8:00 - Идет на работу
            schedule.Add(CreateActivity("Идет на работу", 8, 0, _workLocation, () => {
                _targetNPC.StartWorking();
            }));

            // 12:00 - Обед
            schedule.Add(CreateActivity("Обедает", 12, 0, _socialLocation, () => {
                _targetNPC.StartEating();
            }));

            // 13:00 - Продолжает работать
            schedule.Add(CreateActivity("Работает", 13, 0, _workLocation, () => {
                _targetNPC.StartWorking();
            }));

            // 18:00 - Общение
            schedule.Add(CreateActivity("Общается", 18, 0, _socialLocation, () => {
                _targetNPC.StartSocializing();
            }));

            // 20:00 - Ужин
            schedule.Add(CreateActivity("Ужинает", 20, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 22:00 - Спать
            schedule.Add(CreateActivity("Идет спать", 22, 0, _homeLocation, () => {
                _targetNPC.StartSleeping();
            }));

            ApplyScheduleToNPC(schedule);
        }

        private void CreateShopkeeperSchedule()
        {
            var schedule = new List<DailyActivity>();

            // 7:00 - Просыпается
            schedule.Add(CreateActivity("Просыпается", 7, 0, _homeLocation));

            // 8:00 - Открывает магазин
            schedule.Add(CreateActivity("Открывает магазин", 8, 0, _workLocation, () => {
                Debug.Log($"{_targetNPC.NPCName} открывает магазин");
            }));

            // 12:00 - Обеденный перерыв
            schedule.Add(CreateActivity("Обеденный перерыв", 12, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 13:00 - Продолжает работать
            schedule.Add(CreateActivity("Работает в магазине", 13, 0, _workLocation, () => {
                _targetNPC.StartWorking();
            }));

            // 19:00 - Закрывает магазин
            schedule.Add(CreateActivity("Закрывает магазин", 19, 0, _workLocation, () => {
                Debug.Log($"{_targetNPC.NPCName} закрывает магазин");
            }));

            // 20:00 - Ужин
            schedule.Add(CreateActivity("Ужинает", 20, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 23:00 - Спать
            schedule.Add(CreateActivity("Идет спать", 23, 0, _homeLocation, () => {
                _targetNPC.StartSleeping();
            }));

            ApplyScheduleToNPC(schedule);
        }

        private void CreateGuardSchedule()
        {
            var schedule = new List<DailyActivity>();

            // 6:00 - Смена караула
            schedule.Add(CreateActivity("Заступает на дежурство", 6, 0, _workLocation, () => {
                _targetNPC.StartPatrolling();
            }));

            // 12:00 - Обед
            schedule.Add(CreateActivity("Обедает", 12, 0, _restLocation, () => {
                _targetNPC.StartEating();
            }));

            // 13:00 - Продолжает патрулирование
            schedule.Add(CreateActivity("Патрулирует", 13, 0, _workLocation, () => {
                _targetNPC.StartPatrolling();
            }));

            // 18:00 - Окончание смены
            schedule.Add(CreateActivity("Окончание смены", 18, 0, _homeLocation, () => {
                Debug.Log($"{_targetNPC.NPCName} заканчивает дежурство");
            }));

            // 19:00 - Ужин
            schedule.Add(CreateActivity("Ужинает", 19, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 22:00 - Спать
            schedule.Add(CreateActivity("Идет спать", 22, 0, _homeLocation, () => {
                _targetNPC.StartSleeping();
            }));

            ApplyScheduleToNPC(schedule);
        }

        private void CreateFarmerSchedule()
        {
            var schedule = new List<DailyActivity>();

            // 5:00 - Раннее утро, работа на ферме
            schedule.Add(CreateActivity("Утренние работы", 5, 0, _workLocation, () => {
                _targetNPC.StartWorking();
            }));

            // 7:00 - Завтрак
            schedule.Add(CreateActivity("Завтракает", 7, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 8:00 - Основная работа
            schedule.Add(CreateActivity("Работает на ферме", 8, 0, _workLocation, () => {
                _targetNPC.StartWorking();
            }));

            // 12:00 - Обед
            schedule.Add(CreateActivity("Обедает", 12, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 14:00 - Послеобеденная работа
            schedule.Add(CreateActivity("Работает на ферме", 14, 0, _workLocation, () => {
                _targetNPC.StartWorking();
            }));

            // 17:00 - Уход за животными
            schedule.Add(CreateActivity("Ухаживает за животными", 17, 0, _workLocation, () => {
                Debug.Log($"{_targetNPC.NPCName} ухаживает за животными");
            }));

            // 19:00 - Ужин
            schedule.Add(CreateActivity("Ужинает", 19, 0, _homeLocation, () => {
                _targetNPC.StartEating();
            }));

            // 21:00 - Спать
            schedule.Add(CreateActivity("Идет спать", 21, 0, _homeLocation, () => {
                _targetNPC.StartSleeping();
            }));

            ApplyScheduleToNPC(schedule);
        }

        private DailyActivity CreateActivity(string name, int hour, int minute, Transform location, UnityAction action = null)
        {
            var activity = new DailyActivity
            {
                hour = hour,
                minute = minute,
                activityName = name,
                onActivityStart = new UnityEvent(),
                targetLocation = location,
                moveToLocation = location != null,
                durationMinutes = 60f, // По умолчанию 1 час
                hasExecuted = false
            };

            if (action != null)
            {
                activity.onActivityStart.AddListener(action);
            }

            return activity;
        }

        private void ApplyScheduleToNPC(List<DailyActivity> schedule)
        {
            // Эта функция должна применить расписание к NPC
            // В реальной реализации нужно будет добавить метод SetSchedule в PeacefulNPC
            Debug.Log($"Применено расписание из {schedule.Count} активностей для {_targetNPC.NPCName}");
        }

        // Публичные методы для создания кастомных активностей
        public static DailyActivity CreateWorkActivity(int hour, int minute, Transform workLocation)
        {
            return new DailyActivity
            {
                hour = hour,
                minute = minute,
                activityName = "Работает",
                targetLocation = workLocation,
                moveToLocation = true,
                durationMinutes = 480f, // 8 часов
                onActivityStart = new UnityEvent()
            };
        }

        public static DailyActivity CreateEatActivity(int hour, int minute, Transform eatLocation)
        {
            return new DailyActivity
            {
                hour = hour,
                minute = minute,
                activityName = "Ест",
                targetLocation = eatLocation,
                moveToLocation = true,
                durationMinutes = 30f,
                onActivityStart = new UnityEvent()
            };
        }

        public static DailyActivity CreateSleepActivity(int hour, int minute, Transform sleepLocation)
        {
            return new DailyActivity
            {
                hour = hour,
                minute = minute,
                activityName = "Спит",
                targetLocation = sleepLocation,
                moveToLocation = true,
                durationMinutes = 480f, // 8 часов
                onActivityStart = new UnityEvent()
            };
        }
    }
}