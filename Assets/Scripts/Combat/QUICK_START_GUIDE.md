# Быстрый старт: Динамический спавн врагов

## Подготовка

1. Создайте на сцене пустой GameObject с именем `SpawnHelpers`
2. Добавьте на него компонент `DynamicSpawnHelper`

Этот объект будет использоваться для всех условий спавна и настроек компонентов.

## Сценарий 1: Квестовый враг

**Цель**: Враг спавнится только при активном квесте и при смерти добавляет прогресс.

### Настройка:

1. **На SpawnPoint**:
   - ✓ Use Dynamic Spawn
   
2. **Spawn Condition**:
   - Нажмите `+`
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.CheckQuestActive`
   - Параметры:
     - `QuestList`: перетащите ваш QuestList со сцены
     - `Quest`: выберите нужный Quest asset
   
3. **Components To Add**:
   - Нажмите `+`
   - Component Type Name: `QuestProgress`
   
4. **On Component Added**:
   - Нажмите `+` дважды
   
   **Первый слушатель** (настройка QuestProgress):
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.SetupQuestProgress`
   - Параметры:
     - `questList`: ваш QuestList
     - `questReference`: ваш Quest
     - `objectiveReference`: ID цели (например, "kill_enemies")
     - `amount`: 1
   
   **Второй слушатель** (подключение к Health):
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.ConnectQuestProgressToHealth`

✅ Готово! Враг теперь спавнится только при активном квесте и добавляет прогресс при смерти.

## Сценарий 2: Зона врагов, доступная после завершения квеста

**Цель**: Вся зона спавна активируется только после завершения определенного квеста.

### Настройка:

1. **На SpawnZone**:
   - ✓ Use Conditional Spawn
   
2. **Zone Spawn Condition**:
   - Нажмите `+`
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.CheckQuestCondition`
   - Параметры:
     - `QuestList`: ваш QuestList
     - `Quest`: квест, который должен быть завершен
     - `objectiveReference`: "" (пусто для проверки всего квеста)
     - `amount`: 0 (не используется)

✅ Готово! Зона активируется только когда квест завершен.

## Сценарий 3: Враг, который НЕ спавнится если квест активен

**Цель**: Обычный враг исчезает, когда игрок берет определенный квест.

### Настройка:

1. **На SpawnPoint**:
   - ✓ Use Dynamic Spawn
   
2. **Spawn Condition** (два слушателя):
   
   **Первый слушатель**:
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.CheckQuestActive`
   - Параметры:
     - `QuestList`: ваш QuestList
     - `Quest`: квест
   
   **Второй слушатель**:
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.InvertCondition`
   - (Это инвертирует результат первой проверки)

✅ Готово! Враг спавнится только если квест НЕ активен.

## Сценарий 4: Босс появляется когда убиты все враги

**Цель**: Босс спавнится только когда цель квеста "убить 10 врагов" выполнена.

### Настройка:

1. **На SpawnPoint босса**:
   - ✓ Use Dynamic Spawn
   
2. **Spawn Condition** (два слушателя):
   
   **Первый слушатель**:
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.CheckQuestCondition`
   - Параметры:
     - `QuestList`: ваш QuestList
     - `Quest`: квест
     - `objectiveReference`: "kill_enemies"
     - `amount`: 0
   
   **Второй слушатель**:
   - Object: `SpawnHelpers`
   - Function: `DynamicSpawnHelper.InvertCondition`

✅ Готово! Босс появится только когда цель выполнена (т.к. CheckQuestCondition вернет true когда цель НЕ выполнена, а InvertCondition инвертирует это).

## Сценарий 5: Простой враг без условий, но с компонентом

**Цель**: Враг спавнится всегда, но с QuestProgress компонентом.

### Настройка:

1. **На SpawnPoint**:
   - ✓ Use Dynamic Spawn
   
2. **Spawn Condition**:
   - Оставьте пустым ИЛИ используйте `AlwaysSpawn`
   
3. **Components To Add**:
   - Component Type Name: `QuestProgress`
   - On Component Added: настройте как в Сценарии 1

✅ Готово!

## Отладка

Если что-то не работает:

1. **Включите отладку в EnemySpawner**:
   - Найдите EnemySpawner на сцене
   - ✓ Show Debug Info
   
2. **Проверьте консоль**:
   - Увидите сообщения вида: `[EnemySpawner] Точка X пропущена - условия спавна не выполнены`

3. **Проверьте настройки**:
   - Убедитесь что SpawnHelpers присутствует на сцене
   - Проверьте что все параметры в UnityEvent заполнены
   - Убедитесь что имя компонента написано правильно (например, `QuestProgress`, а не `questProgress`)

## Полезные методы DynamicSpawnHelper

- `CheckQuestActive` - проверка активности квеста
- `CheckQuestNotStarted` - квест еще не взят
- `CheckQuestCondition` - проверка выполнения цели квеста
- `InvertCondition` - инвертировать результат
- `AlwaysSpawn` - всегда спавнить
- `NeverSpawn` - никогда не спавнить
- `SetupQuestProgress` - настроить QuestProgress
- `ConnectQuestProgressToHealth` - подключить к событию смерти

## Производительность

✓ Все проверки выполняются только при спавне  
✓ Никаких проверок в Update()  
✓ Мертвые враги автоматически удаляются из памяти  
✓ Компоненты добавляются эффективно через рефлексию  

## Дополнительно

Подробную документацию смотрите в `DYNAMIC_SPAWN_README.md`.
