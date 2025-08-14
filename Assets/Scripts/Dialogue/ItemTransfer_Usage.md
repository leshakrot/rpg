# Система передачи предметов - Правильная архитектура

## Обзор
Функциональность передачи предметов реализована через расширение класса `Inventory`. Никаких дополнительных скриптов не требуется.

## Новые методы в Inventory

### Методы передачи:
- `TransferItem(InventoryItem item, int quantity)` - передать предмет
- `TransferItemByID(string itemID, int quantity)` - передать предмет по ID

### Методы проверки:
- `CanTransferItem(InventoryItem item, int quantity)` - проверить возможность передачи
- `CanTransferItemByID(string itemID, int quantity)` - проверить по ID

## Настройка в DialogueTrigger

### Пример 1: Передача веток
1. В DialogueTrigger добавьте действие:
   - **Action Name**: "TransferBranches"
   - **On Trigger**: выберите GameObject игрока → Inventory → `TransferItemByID(string, int)`
   - **Параметры**: 
     - itemID: "your-branch-item-id" 
     - quantity: 5

### Пример 2: Передача предмета через ScriptableObject
1. В DialogueTrigger добавьте действие:
   - **Action Name**: "TransferHerbs" 
   - **On Trigger**: выберите GameObject игрока → Inventory → `TransferItem(InventoryItem, int)`
   - **Параметры**:
     - item: перетащите HerbItem ScriptableObject
     - quantity: 3

## Проверка предметов в диалогах

Используйте существующий предикат в системе диалогов:
```
HasInventoryItem itemID quantity
```

Например: `HasInventoryItem branch-item-id 5`

## Алгоритм использования:

1. **Настройка условия в диалоге**: Используйте предикат `HasInventoryItem` для проверки наличия предметов перед показом опции
2. **Настройка действия в DialogueTrigger**: Добавьте действие с методом `TransferItem` или `TransferItemByID`
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
- On Trigger: Player → Inventory → TransferItemByID("branch-item-id", 5)

## Безопасность:

✅ Автоматическая проверка количества  
✅ Корректная обработка стакируемых предметов  
✅ Безопасное удаление из нескольких слотов  
✅ Сохранение остатков предметов  
✅ Подробное логирование операций

Теперь система инвентаря корректно отвечает за передачу предметов, а DialogueTrigger только запускает нужные методы!