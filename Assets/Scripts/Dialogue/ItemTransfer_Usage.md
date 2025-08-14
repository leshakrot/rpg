# Система передачи предметов - Правильная архитектура

## Обзор
Функциональность передачи предметов реализована через расширение класса `Inventory`. Никаких дополнительных скриптов не требуется.

## Новые методы в Inventory

### Основные методы передачи:
- `TransferItem(InventoryItem item, int quantity)` - передать предмет
- `TransferItemByID(string itemID, int quantity)` - передать предмет по ID

### Методы для UnityEvent (рекомендуется):
- `GiveItemToNPC(string itemID, int quantity)` - передать предмет по ID 
- `GiveItemToNPC(string itemID)` - передать 1 предмет по ID

### Методы проверки:
- `CanTransferItem(InventoryItem item, int quantity)` - проверить возможность передачи
- `CanTransferItemByID(string itemID, int quantity)` - проверить по ID
- `HasItemForTransfer(string itemID, int quantity)` - проверить для UnityEvent
- `HasItemForTransfer(string itemID)` - проверить 1 предмет для UnityEvent

## Настройка в DialogueTrigger

### Пример 1: Передача веток (рекомендуемый способ)
1. В DialogueTrigger добавьте действие:
   - **Action Name**: "TransferBranches"
   - **On Trigger**: выберите GameObject игрока → Inventory → `GiveItemToNPC(string, int)`
   - **Параметры**: 
     - itemID: "your-branch-item-id" 
     - quantity: 5

### Пример 2: Передача одного предмета
1. В DialogueTrigger добавьте действие:
   - **Action Name**: "TransferOneHerb" 
   - **On Trigger**: выберите GameObject игрока → Inventory → `GiveItemToNPC(string)`
   - **Параметры**:
     - itemID: "herb-item-id"

### Пример 3: Альтернативный способ
1. В DialogueTrigger добавьте действие:
   - **Action Name**: "TransferItems"
   - **On Trigger**: выберите GameObject игрока → Inventory → `TransferItemByID(string, int)`
   - **Параметры**:
     - itemID: "item-id"
     - quantity: 3

## Проверка предметов в диалогах

Используйте существующий предикат в системе диалогов:
```
HasInventoryItem itemID quantity
```

Например: `HasInventoryItem branch-item-id 5`

## Алгоритм использования:

1. **Настройка условия в диалоге**: Используйте предикат `HasInventoryItem` для проверки наличия предметов перед показом опции
2. **Настройка действия в DialogueTrigger**: Добавьте действие с методом `GiveItemToNPC`
3. **Запуск из диалога**: Укажите имя действия в поле Action диалогового узла

## Пример полного процесса:

**В диалоге:**
```
NPC: "Мне нужно 5 веток для забора"
Опция игрока: "Вот ваши ветки" 
  - Условие: HasInventoryItem branch-item-id 5
  - Action: "TransferBranches"
```

**В DialogueTrigger NPC:**
- Action Name: "TransferBranches"
- On Trigger: Player → Inventory → GiveItemToNPC("branch-item-id", 5)

## Почему GiveItemToNPC вместо TransferItemByID?

- `GiveItemToNPC` - это обертка специально для UnityEvent, гарантированно работает в Inspector
- `TransferItemByID` - основной метод, но может не отображаться в некоторых версиях Unity
- Оба метода делают одно и то же, просто `GiveItemToNPC` более совместим с Unity Inspector

## Безопасность:

✅ Автоматическая проверка количества  
✅ Корректная обработка стакируемых предметов  
✅ Безопасное удаление из нескольких слотов  
✅ Сохранение остатков предметов  
✅ Подробное логирование операций

Теперь система инвентаря корректно отвечает за передачу предметов, а DialogueTrigger только запускает нужные методы!