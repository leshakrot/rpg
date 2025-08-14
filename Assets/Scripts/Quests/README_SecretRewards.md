# Система тайных наград для квестов

## Обзор

Система тайных наград позволяет создавать квесты с сюрпризами - наградами, которые скрыты от игрока до завершения квеста. Это делает прохождение более интересным и непредсказуемым.

## Основные возможности

- ✅ Отметка наград как тайных в Inspector
- ✅ Поддержка в кастомном редакторе QuestEditor
- ✅ Автоматическое разделение видимых и тайных наград
- ✅ UI компоненты для отображения наград
- ✅ Методы для работы с тайными наградами в коде

## Как использовать

### 1. Создание квеста с тайными наградами

#### В Inspector:
1. Откройте ScriptableObject квеста
2. В секции "Rewards" добавьте награды
3. Отметьте галочкой "Тайная награда" для скрытых наград

#### В Quest Editor:
1. Откройте Window → RPG Tools → Quest Editor
2. Выберите квест или создайте новый
3. В секции Rewards добавьте награды
4. Отметьте "Тайная награда" для нужных наград

### 2. Отображение наград в UI

#### До завершения квеста (показываем только видимые):
```csharp
// В QuestTooltipUI автоматически показываются только видимые награды + подсказка "???"
foreach(var reward in quest.GetVisibleRewards())
{
    // Отображаем только видимые награды
}

if(QuestRewardHelper.HasSecretRewards(quest))
{
    // Показываем намек: "???"
}
```

#### При завершении квеста (показываем все):
```csharp
// Используйте QuestCompletionRewardUI для полного отображения
questCompletionUI.ShowCompletionRewards(quest);

// Или получите все награды вручную:
foreach(var reward in quest.GetRewards())
{
    // Выдаем все награды игроку
}
```

### 3. Программная работа с наградами

```csharp
// Получить все награды
var allRewards = quest.GetRewards();

// Получить только видимые награды (для предпросмотра)
var visibleRewards = quest.GetVisibleRewards();

// Получить только тайные награды
var secretRewards = quest.GetSecretRewards();

// Проверить наличие тайных наград
bool hasSecrets = QuestRewardHelper.HasSecretRewards(quest);

// Получить количество тайных наград
int secretCount = QuestRewardHelper.GetSecretRewardCount(quest);

// Получить текст-подсказку
string hint = QuestRewardHelper.GetSecretRewardHint(quest);
// Возвращает: "У этого квеста есть тайная награда!" или
//           "У этого квеста есть 3 тайных наград!"
```

### 4. Интеграция с QuestGiver

```csharp
public class MyQuestGiver : MonoBehaviour
{
    private QuestGiver questGiver;
    
    void Start()
    {
        questGiver = GetComponent<QuestGiver>();
    }
    
    // Показать превью квеста (только видимые награды)
    public void ShowQuestPreview(int questIndex)
    {
        string previewText = questGiver.GetQuestPreviewText(questIndex);
        Debug.Log(previewText);
        // Выводит награды + намек на тайные если есть
    }
    
    // Показать результат завершения (все награды)
    public void ShowQuestCompletion(int questIndex)
    {
        string completionText = questGiver.GetQuestCompletionText(questIndex);
        Debug.Log(completionText);
        // Выводит все награды включая бонусные
    }
}
```

## Компоненты системы

### Quest.Reward
```csharp
[System.Serializable]
public class Reward
{
    public int number;
    public InventoryItem item;
    public bool isSecret = false; // НОВОЕ: флаг тайной награды
}
```

### QuestRewardHelper
Утилитарный класс со статическими методами для работы с наградами.

### QuestCompletionRewardUI
UI компонент для красивого отображения всех наград при завершении квеста.

### RewardPropertyDrawer
Обновленный PropertyDrawer с поддержкой чекбокса "Тайная награда".

## Примеры сценариев использования

### 1. Квест с сюрпризом
- Видимая награда: 50 золота
- Тайная награда: Редкий меч
- Игрок видит только золото, но получает и меч

### 2. Цепочка квестов
- Первый квест: обычные награды
- Последний квест: много тайных наград как финальный сюрприз

### 3. Исследовательские квесты
- Видимая награда: Опыт
- Тайные награды: Карта, Ключ, Магический свиток

## Миграция существующих квестов

Используйте инструмент: Window → RPG Tools → Quest Secret Reward Migrator

Все существующие награды остаются видимыми по умолчанию (isSecret = false).

## Лучшие практики

1. **Баланс**: Не делайте ВСЕ награды тайными
2. **Намеки**: Используйте подсказки о наличии тайных наград
3. **Значимость**: Тайные награды должны быть особенными
4. **Уведомления**: Четко показывайте бонусные награды при завершении
5. **Документация**: Записывайте какие квесты имеют тайные награды

## Техническая информация

- Обратная совместимость: ✅ (старые квесты работают без изменений)
- Поддержка Undo/Redo: ✅ 
- Сериализация: ✅ (новое поле автоматически сериализуется)
- Performance: ✅ (минимальное влияние на производительность)