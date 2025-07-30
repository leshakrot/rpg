# Система Добычи Ресурсов (Harvesting System)

Полная модульная система добычи ресурсов для RPG игры, включающая поддержку анимаций, звуков и VFX эффектов.

## 🎯 Основные возможности

- **Модульная архитектура** - легко расширяется для новых типов ресурсов
- **Система инструментов** - топоры, кирки, серпы и ручная добыча
- **Уровневая система** - требования по уровню игрока и инструментам
- **Прогресс-бар** - визуальное отображение процесса добычи
- **Анимации игрока** - поддержка анимаций добычи с разными инструментами
- **Звуковая система** - звуки инструментов и ресурсов с рандомизацией
- **VFX эффекты** - частицы для разных типов ресурсов
- **Респавн системы** - автоматическое восстановление ресурсов

## 🏗️ Архитектура системы

### Основные компоненты:

1. **HarvestingManager** - центральный менеджер системы
2. **HarvestableSource** - абстрактный базовый класс для всех добываемых объектов
3. **TreeSource** - конкретная реализация для деревьев
4. **HarvestingAnimationController** - управление анимациями игрока
5. **HarvestingAudioManager** - управление звуками добычи
6. **HarvestingVFXManager** - управление визуальными эффектами
7. **HarvestBar** - UI компонент прогресс-бара

### ScriptableObjects:

- **HarvestableResource** - определение ресурса
- **HarvestingTool** - определение инструмента

## 🎮 Быстрый старт

### 1. Настройка игрока

```csharp
// Добавьте компоненты на игрока
player.AddComponent<HarvestingAnimationController>();
player.AddComponent<HarvestingAudioManager>();

// Или используйте редакторское меню:
// Tools -> Harvesting -> Setup Player for Harvesting
```

### 2. Создание ресурса

```csharp
// Создайте ScriptableObject ресурса
var resource = ScriptableObject.CreateInstance<HarvestableResource>();
resource.ResourceName = "Берёза";
resource.MinimumLevel = 1;
resource.RequiredToolType = HarvestingToolType.Axe;
resource.ResourceTier = 1;
```

### 3. Создание дерева

```csharp
// Добавьте TreeSource на GameObject дерева
var treeSource = treeGO.AddComponent<TreeSource>();
treeSource.Resource = resource;
```

### 4. Настройка UI

```csharp
// Создайте HarvestBar в Canvas
// Tools -> Harvesting -> Create HarvestBar HUD Prefab
```

## 🎬 Система анимаций

### Параметры аниматора:

- `harvesting` (bool) - активна ли добыча
- `harvestingSpeed` (float) - скорость анимации добычи
- `toolType` (int) - тип инструмента (0=None, 1=Axe, 2=Pickaxe, 3=Sickle)

### Настройка аниматора:

```csharp
// Создайте аниматор контроллер
// Tools -> Harvesting -> Create Harvesting Animation Controller
```

### Состояния анимации:

1. **Idle** - обычное состояние
2. **Harvesting** - состояние добычи

## 🔊 Звуковая система

### Типы звуков:

- **Звуки инструментов** - топор, кирка, серп, руки
- **Звуки ресурсов** - дерево, камень, металл, трава
- **Звуки завершения** - истощение ресурса, завершение добычи

### Настройка звуков:

```csharp
var audioManager = player.GetComponent<HarvestingAudioManager>();
audioManager.WoodHarvestingSounds = woodSounds;
audioManager.AxeSounds = axeSounds;
```

### Особенности:

- **Рандомизация** - случайный pitch и volume
- **3D звук** - пространственное воспроизведение
- **Автоматическое воспроизведение** - по событиям системы

## ✨ VFX система

### Типы эффектов:

- **Эффекты инструментов** - специфичные для каждого инструмента
- **Эффекты ресурсов** - деревянная стружка, каменная пыль, металлические искры
- **Эффекты завершения** - истощение и завершение добычи

### Создание эффектов:

```csharp
// Создайте примеры эффектов
// Tools -> Harvesting -> Create Example VFX Effects
```

### Настройка эффектов:

```csharp
var vfxManager = FindObjectOfType<HarvestingVFXManager>();
vfxManager.WoodHarvestingEffects = woodEffects;
vfxManager.AxeEffects = axeEffects;
```

## 🛠️ Инструменты

### Типы инструментов:

- **None** - ручная добыча (только T1 ресурсы)
- **Axe** - топор (дерево)
- **Pickaxe** - кирка (камень, руда)
- **Sickle** - серп (трава, цветы)

### Создание инструмента:

```csharp
var axe = ScriptableObject.CreateInstance<HarvestingTool>();
axe.ToolType = HarvestingToolType.Axe;
axe.ToolTier = 1;
axe.SpeedMultiplier = 1.5f;
axe.MaxResourceTier = 3;
```

## 📊 Система уровней

### Требования:

- **Уровень игрока** - минимальный уровень для добычи
- **Тип инструмента** - необходимый инструмент
- **Тир инструмента** - минимальный тир инструмента

### Примеры:

- **Берёза** - уровень 1+, любой топор
- **Сосна** - уровень 5+, топор T1+
- **Кедр** - уровень 10+, топор T2+

## 🎨 UI система

### HarvestBar:

- **Screen Space HUD** - отображается в интерфейсе
- **Плавная анимация** - интерполяция прогресса
- **Автоматическое скрытие** - после завершения добычи
- **Настройка цветов** - для разных типов ресурсов

### Создание UI:

```csharp
// Создайте HarvestBar
// Tools -> Harvesting -> Create HarvestBar HUD Prefab
```

## 🔧 Редакторские инструменты

### Доступные меню:

- **Tools -> Harvesting -> Create Harvesting Animation Controller** - создание аниматора
- **Tools -> Harvesting -> Setup Player for Harvesting** - настройка игрока
- **Tools -> Harvesting -> Create Example VFX Effects** - создание эффектов
- **Tools -> Harvesting -> Create HarvestBar HUD Prefab** - создание UI

## 🐛 Устранение неполадок

### Проблемы с анимациями:

1. **Проверьте аниматор** - есть ли параметры `harvesting`, `harvestingSpeed`, `toolType`
2. **Проверьте компонент** - добавлен ли `HarvestingAnimationController` на игрока
3. **Проверьте события** - подписаны ли компоненты на события `HarvestingManager`

### Проблемы со звуками:

1. **Проверьте AudioSource** - есть ли компонент на игроке
2. **Проверьте звуковые файлы** - назначены ли в `HarvestingAudioManager`
3. **Проверьте громкость** - не отключен ли звук в настройках

### Проблемы с VFX:

1. **Проверьте VFX Manager** - есть ли в сцене `HarvestingVFXManager`
2. **Проверьте эффекты** - назначены ли префабы эффектов
3. **Проверьте материалы** - есть ли материалы для ParticleSystem

### Проблемы с UI:

1. **Проверьте Canvas** - есть ли Canvas в сцене
2. **Проверьте HarvestBar** - добавлен ли компонент в Canvas
3. **Проверьте ссылки** - назначены ли UI элементы в компоненте

## 📝 Примеры использования

### Создание нового типа ресурса:

```csharp
[CreateAssetMenu(fileName = "New Resource", menuName = "Harvesting/Resource")]
public class CustomResource : HarvestableResource
{
    // Дополнительная логика
}
```

### Создание нового источника ресурса:

```csharp
public class CustomSource : HarvestableSource
{
    protected override void StartHarvestAnimation()
    {
        // Специфичная анимация
    }
    
    protected override void StopHarvestAnimation()
    {
        // Остановка анимации
    }
}
```

### Настройка событий:

```csharp
HarvestingManager.Instance.OnHarvestingStarted += (harvestable, player) =>
{
    Debug.Log($"Начата добыча: {harvestable.GetResource().ResourceName}");
};

HarvestingManager.Instance.OnResourceHarvested += (resource, amount) =>
{
    Debug.Log($"Добыто: {amount} {resource.ResourceName}");
};
```

## 🔄 Расширение системы

### Добавление нового типа инструмента:

1. Добавьте значение в enum `HarvestingToolType`
2. Обновите `HarvestingAnimationController`
3. Обновите `HarvestingAudioManager`
4. Обновите `HarvestingVFXManager`

### Добавление нового типа ресурса:

1. Создайте новый класс ресурса (наследуйте от `HarvestableResource`)
2. Создайте новый класс источника (наследуйте от `HarvestableSource`)
3. Добавьте звуки и эффекты в соответствующие менеджеры

## 📋 Чек-лист настройки

- [ ] Создан `HarvestingManager` в сцене
- [ ] Добавлен `HarvestingAnimationController` на игрока
- [ ] Добавлен `HarvestingAudioManager` на игрока
- [ ] Создан `HarvestingVFXManager` в сцене
- [ ] Настроен аниматор с параметрами добычи
- [ ] Создан `HarvestBar` в Canvas
- [ ] Назначены звуки в `HarvestingAudioManager`
- [ ] Назначены эффекты в `HarvestingVFXManager`
- [ ] Созданы ресурсы и инструменты
- [ ] Настроены источники ресурсов в сцене

## 🎯 Заключение

Система добычи ресурсов предоставляет полный набор инструментов для создания увлекательного геймплея добычи. Модульная архитектура позволяет легко расширять и настраивать систему под нужды проекта.

Для получения дополнительной помощи обратитесь к примерам в папке `Assets/Scripts/Harvesting/Examples/`. 