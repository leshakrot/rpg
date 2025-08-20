# Система мирных жителей и NPC

## Обзор

Создана гибкая система для мирных жителей и NPC с поддержкой профессий, расписания дня, кастомных действий и миграции между сценами.

## Компоненты системы

### 1. PeacefulNPC - основной компонент
Главный компонент для мирных жителей с поддержкой:
- Системы профессий
- Распорядка дня с интеграцией в DayNightSystem
- Кастомных действий через UnityEvents
- Сохранения состояния (ISaveable)
- Миграции между сценами

### 2. NPCProfession - ScriptableObject для профессий
Определяет профессию NPC:
- Название и описание профессии
- Стандартное расписание дня
- Анимации для работы
- Звуки для активностей

### 3. SpecificNPCActions - конкретные действия
Компонент с готовыми действиями для специализированных NPC:
- Рыбак: рыбалка с удочкой и анимациями
- Кузнец: ковка с молотом и искрами
- Торговец: торговля с показом товаров
- Фермер: работа в поле и уход за животными
- Стражник: патрулирование с оружием

### 4. NPCMigrationManager - управление миграцией
Singleton для управления перемещением NPC между сценами с сохранением состояния.

### 5. NPCScheduleBuilder - конструктор расписаний
Утилита для создания готовых шаблонов расписания дня.

## Как использовать

### Настройка мирного NPC

1. **Добавьте компоненты к GameObject:**
   ```
   - PeacefulNPC (основной компонент)
   - AIController (установите isHostile = false)
   - Mover (для передвижения)
   - NavMeshAgent (для навигации)
   - Animator (для анимаций)
   ```

2. **Опциональные компоненты:**
   ```
   - Health (если NPC может умереть)
   - Fighter (если NPC может сражаться при необходимости)
   - SpecificNPCActions (для специализированных действий)
   ```

### Создание профессии

1. **Через Editor Tool:**
   ```
   RPG Tools → Create NPC Professions
   ```
   
2. **Программно:**
   ```csharp
   var profession = ScriptableObject.CreateInstance<NPCProfession>();
   profession.professionName = "Рыбак";
   profession.workDescription = "Ловит рыбу с помощью удочки";
   ```

### Настройка кастомных действий

#### Способ 1: Через UnityEvents в инспекторе
```
В компоненте PeacefulNPC:
- Custom Actions → On Work Action → добавить метод
- Например: SpecificNPCActions.StartFishing()
```

#### Способ 2: Через код
```csharp
peacefulNPC._onWorkAction.AddListener(() => {
    Debug.Log("Рыбак начинает ловить рыбу!");
    specificActions.StartFishing();
});
```

#### Способ 3: Через активности
```csharp
var activity = new DailyActivity
{
    activityName = "Ловит рыбу",
    activityType = ActivityType.Work,
    onWorkAction = new UnityEvent()
};
activity.onWorkAction.AddListener(() => specificActions.StartFishing());
```

### Пример настройки рыбака

```csharp
// 1. Создать GameObject с нужными компонентами
// 2. Настроить PeacefulNPC:
var npc = GetComponent<PeacefulNPC>();
var actions = GetComponent<SpecificNPCActions>();

// 3. Настроить профессию (через инспектор или код)
// 4. Настроить кастомные действия:
npc._onWorkAction.AddListener(actions.StartFishing);

// 5. Создать расписание или использовать из профессии
```

## Готовые профессии

### Рыбак (Fisherman)
- **Работа:** Ловит рыбу с 7:00 до 18:00
- **Действия:** StartFishing(), StopFishing(), CatchFish()
- **Анимации:** StartFishing trigger

### Кузнец (Blacksmith)  
- **Работа:** Кует с 8:00 до 18:00
- **Действия:** StartForging(), StopForging(), HitAnvil()
- **Эффекты:** Искры от наковальни

### Торговец (Merchant)
- **Работа:** Торгует с 9:00 до 19:00  
- **Действия:** StartTrading(), StopTrading(), ShowGoods()
- **Особенности:** Показ/скрытие товаров

### Фермер (Farmer)
- **Работа:** С 5:00 до 19:00 с перерывами
- **Действия:** StartFarming(), TendCrops(), HarvestCrops()
- **Инструменты:** Показ фермерских инструментов

### Стражник (Guard)
- **Работа:** Смены по 12 часов
- **Действия:** StartPatrolling(), AlertMode(), Salute()
- **Оружие:** Активация/деактивация оружия

## Система сохранения

Автоматически сохраняется:
- Позиция и поворот NPC
- Текущая сцена
- Выполненные активности
- Последнее действие

```csharp
// Данные сохраняются автоматически через ISaveable
public object CaptureState() { /* автоматически */ }
public void RestoreState(object state) { /* автоматически */ }
```

## Миграция между сценами

```csharp
// Программная миграция
NPCMigrationManager.Instance.MigrateNPC(npcId, targetSceneIndex, targetPosition);

// Миграция по имени
NPCMigrationManager.Instance.MigrateNPCByName("Рыбак Иван", 2, newPosition);

// Ручная миграция
peacefulNPC.MigrateToScene("MainTown");
```

## Интеграция с DayNightSystem

Активности автоматически регистрируются в TimeEvents:
```csharp
// В Start() автоматически:
_dayNightSystem.AddTimeEvent(activity.hour, activity.minute, activityEvent);
```

## Отладка

### Debug методы:
```csharp
// Принудительный запуск активности
npc.ForceStartActivity("Ловит рыбу");

// Сброс расписания
npc.ResetDailySchedule();

// Показать миграции
NPCMigrationManager.Instance.DebugShowMigrationData();
```

### Консольные сообщения:
- `"Рыбак Иван начинает активность: Ловит рыбу в 07:00"`
- `"Рыбак Иван выполняет работу: Ловит рыбу с помощью удочки"`

## Лучшие практики

1. **Используйте профессии** для создания переиспользуемых типов NPC
2. **Настраивайте кастомные действия** через UnityEvents для гибкости
3. **Добавляйте SpecificNPCActions** только специализированным NPC
4. **Тестируйте миграцию** между сценами для критичных NPC
5. **Используйте уникальные ID** для каждого NPC

## Расширение системы

### Добавление новой профессии:
1. Создать ScriptableObject через Editor Tool
2. Настроить расписание и описания
3. Добавить специфические методы в SpecificNPCActions
4. Создать анимации и звуки

### Добавление новых типов активностей:
1. Расширить enum ActivityType
2. Добавить обработку в ExecuteActivityByType()
3. Создать соответствующий Perform***Action() метод

Система спроектирована для легкого расширения и кастомизации под нужды конкретного проекта.