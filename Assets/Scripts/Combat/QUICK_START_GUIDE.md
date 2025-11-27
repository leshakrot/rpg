# Быстрый старт: Условный спавн врагов

## ✅ Что изменилось

### Обновлённые файлы:
1. **SpawnPoint.cs** - добавлены поля для условного спавна
2. **EnemySpawner.cs** - добавлена логика обработки условий и компонентов
3. **SpawnPointEditor.cs** - обновлён инспектор для отображения новых полей

## 📋 Новые поля в SpawnPoint

После обновления Unity и перекомпиляции вы должны увидеть в Inspector:

### Раздел "Условный спавн и динамические компоненты"
Этот раздел появится в кастомном инспекторе SpawnPoint. Если вы его не видите:

1. **Проверьте компиляцию**: нет ли ошибок в консоли
2. **Перезапустите Unity**: иногда требуется перезапуск для обновления inspector
3. **Удалите и добавьте компонент**: удалите SpawnPoint с объекта и добавьте заново

### Поля в разделе:

#### 1. **Условия спавна** (Spawn Conditions)
- Тип: `Condition` (из GameDevTV.Utils)
- Назначение: Условия для спавна врага (квесты, диалоги, etc)
- Оставьте пустым для безусловного спавна

#### 2. **Динамические компоненты** (Components To Add)
- Тип: `List<ComponentToAdd>`
- Назначение: Компоненты, которые будут добавлены на врага

Каждый элемент списка содержит:

**Component Type Name** (строка)
- Полное имя типа компонента, например: `QuestProgress`
- Должно совпадать с именем класса компонента

**Component Source Prefab** (GameObject, опционально)
- Префаб с настроенным компонентом
- Поля будут скопированы из этого префаба на нового врага
- Оставьте пустым, если не нужно копировать настройки

**Event Binding** (Настройка события)
- **Event Type**: Тип события (None, OnDie)
- **Method Name**: Имя метода для вызова (например, `AddProgress`)

## 🎯 Пример использования: Квестовый враг

### Шаг 1: Настройка SpawnPoint

1. Выберите GameObject с компонентом `SpawnPoint`
2. Найдите раздел "Условный спавн и динамические компоненты"
3. Нажмите "+" в списке "Динамические компоненты"

### Шаг 2: Добавление QuestProgress

В новом элементе списка:

**Component Type Name**: `QuestProgress`  
**Component Source Prefab**: (перетащите префаб с настроенным QuestProgress, если есть)  
**Event Type**: `OnDie`  
**Method Name**: `AddProgress`

### Шаг 3: Настройка условий (опционально)

В поле "Условия спавна":
- Добавьте условия через систему Condition/Predicate
- Например, "Квест X активен" или "Игрок в локации Y"

## ⚡ Программный спавн (опционально)

```csharp
using RPG.Combat;

EnemySpawner spawner = FindObjectOfType<EnemySpawner>();

// Простой динамический спавн
spawner.SpawnDynamicEnemy(position, enemyPrefab);

// Спавн с компонентами
List<ComponentToAdd> components = new List<ComponentToAdd>
{
    new ComponentToAdd 
    {
        componentTypeName = "QuestProgress",
        eventBinding = new ComponentEventBinding 
        {
            eventType = ComponentEventBinding.EventType.OnDie,
            methodName = "AddProgress"
        }
    }
};

spawner.SpawnDynamicEnemyWithComponents(position, enemyPrefab, components);
```

## 🔧 Устранение проблем

### Не вижу новые поля в инспекторе

1. **Проверьте консоль** на ошибки компиляции
2. **Перезапустите Unity Editor**
3. **Попробуйте удалить и заново добавить компонент SpawnPoint**
4. **Убедитесь, что используется кастомный инспектор** (SpawnPointEditor.cs)

### Компонент не добавляется на врага

1. Проверьте **Component Type Name** - должно быть точное имя класса
2. Включите `showDebugInfo` в EnemySpawner для логов
3. Проверьте, что метод существует и public

### Метод не вызывается при смерти

1. Проверьте **Method Name** - должно совпадать с именем метода
2. Метод должен быть `public` и без параметров
3. Проверьте, что у врага есть компонент Health с событием onDie

## 📚 Дополнительная информация

См. также:
- `README_ConditionalSpawning.txt` - полная документация
- `DynamicEnemySpawnExample.cs` - примеры кода
- `RandomEncounterTrigger.cs` - готовый компонент для случайных встреч
