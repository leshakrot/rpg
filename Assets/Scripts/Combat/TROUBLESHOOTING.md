# Устранение проблем с условным спавном

## 🔴 Проблема: Не вижу новые поля в Inspector

### Симптомы:
- В компоненте `SpawnPoint` нет раздела "Условный спавн и динамические компоненты"
- Поля `spawnConditions` и `componentsToAdd` не отображаются
- Inspector показывает только старые поля

### Причина:
Unity кэширует метаданные компонентов. После добавления новых полей в существующий компонент, Unity может не обновить Inspector автоматически.

### ✅ Решение (выберите одно):

#### Метод 1: Перезапуск Unity (Рекомендуется - 95% успеха)
```
1. File → Save Project (или Ctrl+S)
2. File → Exit (закройте Unity полностью)
3. Откройте проект заново через Unity Hub
4. Откройте сцену с SpawnPoint
5. Выберите GameObject → новые поля появятся
```

#### Метод 2: Пересоздание компонента (100% гарантия)
```
1. Выберите GameObject с SpawnPoint
2. Сделайте скриншот/запомните настройки
3. Inspector → SpawnPoint → ⋮ → Remove Component
4. Add Component → поиск "Spawn Point" → добавить
5. Восстановите настройки из скриншота
6. Новые поля доступны!
```

#### Метод 3: Force Reserialize
```
1. Assets → Force Reserialize Assets...
2. Select: Assets/Scenes
3. Reserialize
4. Дождитесь завершения
```

---

## 🔴 Проблема: Компонент не добавляется на врага

### Симптомы:
- Враг спавнится, но без нужного компонента
- В консоли нет ошибок
- QuestProgress (или другой компонент) не найден на враге

### Диагностика:

#### Шаг 1: Включите Debug логи
```csharp
// В EnemySpawner найдите и включите:
[SerializeField] private bool showDebugInfo = true;
```

#### Шаг 2: Проверьте Component Type Name
Должно быть **ТОЧНОЕ** имя класса:
- ✅ Правильно: `QuestProgress`
- ❌ Неправильно: `questProgress`, `Quest Progress`, `QuestProgressComponent`

Для компонентов в namespace используйте полное имя:
- ✅ Правильно: `MyGame.Quests.QuestProgress`
- ❌ Неправильно: `QuestProgress` (если в namespace)

#### Шаг 3: Проверьте Console при спавне
Должно быть сообщение:
```
[EnemySpawner] Добавлен компонент QuestProgress на Enemy(Clone)
```

Если видите:
```
[EnemySpawner] Не удалось найти тип QuestProgress
```
→ Проверьте имя класса и namespace

---

## 🔴 Проблема: Метод не вызывается при смерти

### Симптомы:
- Компонент добавлен на врага
- Враг умирает
- Метод не вызывается (квест не обновляется)

### Чек-лист:

#### 1. Проверьте имя метода
```csharp
// В SpawnPoint → Event Binding → Method Name должно быть:
"AddProgress"  // точно как в коде класса

// В классе QuestProgress метод должен быть:
public void AddProgress()  // точное совпадение!
{
    // код
}
```

#### 2. Метод должен быть PUBLIC
```csharp
// ✅ Правильно:
public void AddProgress() { }

// ❌ Неправильно:
private void AddProgress() { }
void AddProgress() { }  // internal по умолчанию
```

#### 3. Метод должен быть БЕЗ параметров
```csharp
// ✅ Правильно:
public void AddProgress() { }

// ❌ Неправильно:
public void AddProgress(int amount) { }
public void AddProgress(GameObject enemy) { }
```

#### 4. У врага должен быть Health компонент
```csharp
// Проверьте в префабе врага:
GetComponent<Health>() != null

// И у Health должно быть событие onDie (UnityEvent)
```

---

## 🔴 Проблема: Враг не спавнится

### Диагностика:

#### 1. Проверьте Spawn Chance
```
SpawnPoint → Шанс спавна = 0.7  (70%)
Попробуйте установить 1.0 для теста
```

#### 2. Проверьте Condition (если используется)
```
1. Откройте SpawnPoint → Условия спавна
2. Временно удалите Condition (сделайте null)
3. Попробуйте заспавнить
4. Если заработало → проверьте предикаты в Condition
```

#### 3. Проверьте Enemy Prefabs
```
SpawnPoint → Список врагов → должен быть хотя бы 1 префаб
```

#### 4. Проверьте лимит врагов
```
EnemySpawner → Max Enemies Per Scene
Убедитесь что лимит не достигнут
```

---

## 🔴 Проблема: Утечка памяти / враги не удаляются

### Симптомы:
- Мертвые враги остаются в сцене
- Количество объектов растет
- FPS падает со временем

### Решение:

#### 1. Включите auto-remove corpses
```csharp
// В EnemySpawner:
[SerializeField] private bool autoRemoveCorpses = true;
[SerializeField] private float corpseRemovalTime = 5f;
```

#### 2. Проверьте что враг имеет Health
Условные враги должны иметь компонент `Health` с событием `onDie`

#### 3. Не удаляйте врагов вручную
```csharp
// ❌ Неправильно:
Destroy(enemy);

// ✅ Правильно:
// Ничего не делайте - система сама очистит
```

---

## 🔴 Проблема: Ошибка компиляции

### Распространенные ошибки:

#### "Type ComponentToAdd not found"
→ Используйте: `using RPG.Combat;`

#### "Type Condition not found"
→ Используйте: `using GameDevTV.Utils;`

#### "componentPrefab does not exist"
→ Обновите код: замените `componentPrefab` на `componentTypeName` (строка)

---

## 📞 Получить помощь

Если проблема не решается:

1. **Проверьте Console** - включите Error Pause
2. **Включите Debug логи** - `showDebugInfo = true` в EnemySpawner
3. **Проверьте документацию**:
   - `/Assets/Scripts/Combat/QUICK_START_GUIDE.md`
   - `/Assets/Scripts/Combat/README_ConditionalSpawning.txt`
4. **Примеры кода**:
   - `/Assets/Scripts/Combat/DynamicEnemySpawnExample.cs`
   - `/Assets/Scripts/Combat/RandomEncounterTrigger.cs`

---

## ✅ Проверочный чеклист

Перед тем как запустить систему, проверьте:

- [ ] Unity перезапущен после обновления скриптов
- [ ] В SpawnPoint видны новые поля
- [ ] Component Type Name заполнен правильно
- [ ] Event Type установлен (если нужна привязка)
- [ ] Method Name указан правильно
- [ ] Метод public и без параметров
- [ ] У врага есть Health компонент
- [ ] showDebugInfo включен для тестирования

Если все пункты отмечены ✅ → система должна работать!
