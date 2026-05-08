# WebGL Save System Fix - Bugfix Design

## Overview

Система сохранений в WebGL билде не работает корректно из-за отсутствия кастомных JSON конвертеров для Unity типов (Vector3, Quaternion, Color) в WebSavingAdapter. Это приводит к неправильной десериализации данных при работе через YandexSDK. Дополнительно, ObjectStateSaver создает отдельное сохранение вместо интеграции с основным файлом, а CompanionManager не использует JsonSerializerSettings при десериализации через JObject.

Подход к исправлению: добавить кастомные JSON конвертеры во все точки сериализации/десериализации, интегрировать ObjectStateSaver с основной системой через ISaveable, и обеспечить единообразную обработку типов данных во всех компонентах.

## Glossary

- **Bug_Condition (C)**: Условие, при котором система сохранений работает некорректно - когда WebSavingAdapter сериализует/десериализует данные без кастомных JSON конвертеров для Unity типов
- **Property (P)**: Желаемое поведение - все Unity типы (Vector3, Quaternion, Color) должны корректно сериализоваться и десериализоваться в WebGL билде с использованием тех же конвертеров что и в редакторе
- **Preservation**: Существующая функциональность сохранений в редакторе Unity (локальные файлы) должна остаться неизменной
- **WebSavingAdapter**: Класс в `Assets/Scripts/Saving/WebSavingAdapter.cs` который адаптирует систему сохранений для работы с YandexSDK в WebGL билдах
- **SavingSystem**: Основной класс системы сохранений в `Assets/Scripts/Saving/SavingSystem.cs` который управляет сохранением/загрузкой состояния игры
- **ObjectStateSaver**: Компонент в `Assets/Scripts/Saving/ObjectStateSaver.cs` который управляет видимостью объектов и их состоянием
- **CompanionManager**: Менеджер компаньонов в `Assets/Scripts/Companions/CompanionManager.cs` который сохраняет информацию о нанятых компаньонах
- **JsonSerializerSettings**: Настройки Newtonsoft.Json которые определяют как сериализовать/десериализовать объекты, включая кастомные конвертеры
- **isWebPlatform**: Свойство в SavingSystem которое определяет запущена ли игра в WebGL билде

## Bug Details

### Bug Condition

Баг проявляется когда WebSavingAdapter сериализует или десериализует данные игры для YandexSDK в WebGL билде. Методы `SaveGameData` и `LoadGameData` используют JsonConvert без указания кастомных конвертеров для Unity типов, что приводит к неправильной десериализации Vector3, Quaternion и Color. Дополнительно, ObjectStateSaver создает отдельный файл сохранения "objectStates" вместо интеграции с основным файлом, а CompanionManager использует `JObject.ToObject<SaveData>()` без JsonSerializerSettings.

**Formal Specification:**
```
FUNCTION isBugCondition(input)
  INPUT: input of type SaveOperation
  OUTPUT: boolean
  
  RETURN (input.platform == WebGL)
         AND (input.adapter == WebSavingAdapter)
         AND (input.converters NOT CONTAINS [Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter])
         AND (input.data CONTAINS [Vector3 OR Quaternion OR Color types])
         AND deserialization_fails(input.data)
END FUNCTION
```

### Examples

- **Пример 1**: CompanionManager сохраняет позицию компаньона `waitPosition = Vector3(10.5, 2.0, 15.3)`. В редакторе загружается корректно как Vector3. В WebGL билде десериализуется как JObject с полями x, y, z типа double, что вызывает ошибки при попытке использовать как Vector3.

- **Пример 2**: ObjectStateSaver вызывает `savingSystem.Save("objectStates")`, создавая отдельное сохранение. При переключении сцен основное сохранение загружается через `LoadLastScene("save")`, но состояния объектов из "objectStates" не загружаются, так как это другой файл.

- **Пример 3**: Игрок сохраняет игру с компаньоном на позиции Vector3(5, 0, 10) в WebGL. При загрузке CompanionManager получает JObject вместо SaveData, вызывает `jo.ToObject<SaveData>()` без JsonSerializerSettings, и waitPosition десериализуется некорректно.

- **Edge case**: Если в сохранении нет Unity типов (только примитивы string, int, bool), баг не проявляется и сохранение работает корректно даже без конвертеров.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Сохранение и загрузка в редакторе Unity через локальные файлы должны продолжать работать с существующими JsonSerializerSettings
- SaveableEntity должен продолжать захватывать и восстанавливать состояние через ISaveable интерфейс
- Горячие клавиши (L - загрузка, S - сохранение, Delete - удаление) должны продолжать работать
- Логика определения платформы через isWebPlatform должна продолжать корректно различать WebGL и редактор
- Обработка таймаута при загрузке YandexSDK должна продолжать работать с предупреждением

**Scope:**
Все операции сохранения/загрузки которые НЕ включают Unity типы (Vector3, Quaternion, Color) в WebGL билде должны быть полностью не затронуты этим исправлением. Это включает:
- Сохранение примитивных типов (int, float, string, bool)
- Сохранение коллекций примитивов (List<string>, Dictionary<string, int>)
- Работу в редакторе Unity (не WebGL)

## Hypothesized Root Cause

На основе анализа кода, наиболее вероятные причины:

1. **Отсутствие JsonSerializerSettings в WebSavingAdapter**: Методы `SaveGameData` и `LoadGameData` создают JsonSerializerSettings с `TypeNameHandling = TypeNameHandling.Auto`, но не добавляют кастомные конвертеры (Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter). В результате Unity типы сериализуются в стандартный JSON формат, который при десериализации превращается в JObject вместо Unity типов.

2. **Отдельный файл сохранения в ObjectStateSaver**: Метод `SaveStates()` вызывает `savingSystem.Save("objectStates")` вместо интеграции с основным файлом сохранения. Это создает отдельное сохранение которое не синхронизируется с основным и не загружается автоматически при `LoadLastScene("save")`.

3. **Отсутствие JsonSerializerSettings в CompanionManager**: В методе `RestoreState` используется `jo.ToObject<SaveData>()` без передачи JsonSerializerSettings. Это означает что даже если данные были сохранены с конвертерами, при загрузке они десериализуются без них, теряя информацию о типах.

4. **Несоответствие типов при десериализации**: JSON по умолчанию десериализует числа как `double`, но многие Unity компоненты ожидают `float` или `int`. Без явного указания конвертеров или использования JsonSaveHelper могут возникать ошибки приведения типов.

## Correctness Properties

Property 1: Bug Condition - Unity Types Serialization in WebGL

_For any_ save operation in WebGL build where game data contains Unity types (Vector3, Quaternion, Color), the WebSavingAdapter SHALL use the same custom JSON converters (Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter) and JsonSerializerSettings as SavingSystem, ensuring correct serialization and deserialization of Unity types through YandexSDK.

**Validates: Requirements 2.1**

Property 2: Bug Condition - ObjectStateSaver Integration

_For any_ save operation triggered by ObjectStateSaver, the system SHALL use the main save file instead of creating a separate "objectStates" file, integrating with the general saving system through the ISaveable interface and ensuring object states are saved and loaded together with all other game data.

**Validates: Requirements 2.2**

Property 3: Bug Condition - CompanionManager Deserialization

_For any_ load operation in CompanionManager where state data is provided as JObject, the RestoreState method SHALL use JsonSerializerSettings with custom converters when calling `JObject.ToObject<SaveData>()`, ensuring correct deserialization of Unity types in companion data.

**Validates: Requirements 2.3**

Property 4: Bug Condition - Numeric Type Handling

_For any_ deserialization operation where JSON contains numeric values, the system SHALL correctly handle type conversion through JsonSaveHelper or use proper JsonSerializerSettings, ensuring that double values from JSON are correctly converted to float or int as expected by Unity components.

**Validates: Requirements 2.4**

Property 5: Preservation - Editor Save System

_For any_ save/load operation in Unity Editor (not WebGL), the system SHALL produce the same result as the original system, preserving local file saving with existing JsonSerializerSettings and custom converters.

**Validates: Requirements 3.1, 3.2**

Property 6: Preservation - ISaveable Interface

_For any_ SaveableEntity capturing or restoring component state, the system SHALL produce the same result as the original system, preserving the ISaveable interface workflow without changes.

**Validates: Requirements 3.3**

Property 7: Preservation - YandexSDK Timeout Handling

_For any_ initialization where YandexSDK is not loaded or unavailable, the system SHALL produce the same result as the original system, correctly handling timeout and continuing with a warning.

**Validates: Requirements 3.4**

Property 8: Preservation - Hotkey Functionality

_For any_ user input using hotkeys (L - load, S - save, Delete - delete), the system SHALL produce the same result as the original system, executing corresponding operations.

**Validates: Requirements 3.5**

Property 9: Preservation - Platform Detection

_For any_ platform detection through isWebPlatform property, the system SHALL produce the same result as the original system, correctly distinguishing WebGL build from editor.

**Validates: Requirements 3.6**

## Fix Implementation

### Changes Required

Assuming our root cause analysis is correct:

**File 1**: `Assets/Scripts/Saving/WebSavingAdapter.cs`

**Changes**:
1. **Добавить кастомные JSON конвертеры в SaveGameData**: В методе `SaveGameData` добавить Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter в массив Converters в JsonSerializerSettings
   - Создать массив конвертеров: `new JsonConverter[] { new Vector3JsonConverter(), new QuaternionJsonConverter(), new ColorJsonConverter() }`
   - Добавить в существующий JsonSerializerSettings

2. **Добавить кастомные JSON конвертеры в LoadGameData**: В методе `LoadGameData` добавить те же конвертеры в JsonSerializerSettings при десериализации
   - Использовать тот же массив конвертеров что и в SaveGameData
   - Обеспечить симметричность сериализации/десериализации

3. **Создать вспомогательный метод для JsonSerializerSettings**: Создать приватный статический метод `GetJsonSettings()` который возвращает настроенный JsonSerializerSettings с конвертерами
   - Избежать дублирования кода
   - Обеспечить единообразие настроек

**File 2**: `Assets/Scripts/Saving/ObjectStateSaver.cs`

**Changes**:
1. **Удалить методы SaveStates и LoadStates**: Удалить методы которые создают отдельное сохранение "objectStates"
   - Удалить `SaveStates()` который вызывает `savingSystem.Save("objectStates")`
   - Удалить `LoadStates()` который вызывает `savingSystem.Load("objectStates")`
   - Удалить вызов `LoadStates()` из `Start()`

2. **Удалить поле autoLoadOnStart**: Удалить поле `[SerializeField] private bool autoLoadOnStart = true` так как автозагрузка теперь не нужна
   - ObjectStateSaver будет работать только через ISaveable интерфейс
   - SaveableEntity автоматически вызовет RestoreState при загрузке

3. **Удалить поле autoSaveOnStateChange**: Удалить поле `[SerializeField] private bool autoSaveOnStateChange = true` и все проверки на него
   - Удалить вызовы `SaveStates()` из `ShowAllObjects()`, `HideAllObjects()`, `SetObjectVisibility()`
   - Сохранение будет происходить через основную систему когда игрок явно сохраняет игру

4. **Обновить контекстные меню**: Удалить `[ContextMenu("Save Current States")]` и `[ContextMenu("Load Saved States")]`
   - Оставить только методы управления видимостью объектов
   - Сохранение/загрузка будет через основную систему

5. **Удалить поле savingSystem**: Удалить `private SavingSystem savingSystem` и метод `Awake()` так как прямое взаимодействие с SavingSystem больше не требуется
   - ObjectStateSaver теперь полностью пассивный компонент
   - Работает только через ISaveable интерфейс

**File 3**: `Assets/Scripts/Companions/CompanionManager.cs`

**Changes**:
1. **Добавить using для JsonConverters**: Добавить `using GameDevTV.Saving;` в начало файла для доступа к кастомным конвертерам

2. **Обновить RestoreState для использования JsonSerializerSettings**: В методе `RestoreState` при вызове `jo.ToObject<SaveData>()` передать JsonSerializerSettings с кастомными конвертерами
   - Создать JsonSerializerSettings с TypeNameHandling.Auto
   - Добавить массив конвертеров: Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter
   - Передать settings в `jo.ToObject<SaveData>(settings)`

3. **Создать вспомогательный метод GetJsonSettings**: Создать приватный статический метод который возвращает настроенный JsonSerializerSettings
   - Избежать создания settings каждый раз при вызове RestoreState
   - Обеспечить единообразие с другими компонентами системы сохранений

**File 4**: `Assets/Scripts/Saving/JsonSaveHelper.cs` (если существует)

**Changes**:
1. **Проверить наличие методов приведения типов**: Убедиться что существуют методы `ToInt()`, `ToFloat()` для безопасного приведения типов
   - Если методов нет, создать их
   - Методы должны корректно обрабатывать приведение double к int/float

## Testing Strategy

### Validation Approach

Стратегия тестирования следует двухфазному подходу: сначала выявить контрпримеры которые демонстрируют баг на неисправленном коде, затем проверить что исправление работает корректно и сохраняет существующее поведение.

### Exploratory Bug Condition Checking

**Goal**: Выявить контрпримеры которые демонстрируют баг ДО внедрения исправления. Подтвердить или опровергнуть анализ первопричины. Если опровергнем, потребуется пересмотреть гипотезу.

**Test Plan**: Написать тесты которые сохраняют и загружают данные с Unity типами в WebGL билде через WebSavingAdapter. Запустить эти тесты на НЕИСПРАВЛЕННОМ коде чтобы наблюдать ошибки и понять первопричину.

**Test Cases**:
1. **WebGL Vector3 Serialization Test**: Сохранить данные с Vector3(10.5f, 2.0f, 15.3f) через WebSavingAdapter, затем загрузить и проверить что значение корректно десериализовалось как Vector3 (будет падать на неисправленном коде)
2. **WebGL Quaternion Serialization Test**: Сохранить данные с Quaternion.Euler(45, 90, 0) через WebSavingAdapter, затем загрузить и проверить корректность (будет падать на неисправленном коде)
3. **ObjectStateSaver Integration Test**: Сохранить состояния объектов через ObjectStateSaver, затем загрузить основное сохранение через LoadLastScene и проверить что состояния объектов восстановились (будет падать на неисправленном коде - состояния не загрузятся)
4. **CompanionManager JObject Test**: Создать JObject с данными компаньона включая Vector3 позицию, вызвать RestoreState и проверить что waitPosition корректно десериализовался (может падать на неисправленном коде)

**Expected Counterexamples**:
- Vector3/Quaternion/Color десериализуются как JObject вместо Unity типов
- ObjectStateSaver создает отдельный файл "objectStates" который не загружается с основным сохранением
- CompanionManager получает некорректные данные при десериализации через JObject.ToObject без settings
- Возможные причины: отсутствие кастомных конвертеров в WebSavingAdapter, отдельный файл сохранения в ObjectStateSaver, отсутствие JsonSerializerSettings в CompanionManager

### Fix Checking

**Goal**: Проверить что для всех входных данных где выполняется условие бага, исправленная функция производит ожидаемое поведение.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := WebSavingAdapter_fixed.SaveGameData(input)
  loaded := WebSavingAdapter_fixed.LoadGameData(input.saveFile)
  ASSERT loaded.unityTypes == input.unityTypes
  ASSERT typeof(loaded.vector3Field) == Vector3
  ASSERT typeof(loaded.quaternionField) == Quaternion
  ASSERT typeof(loaded.colorField) == Color
END FOR
```

### Preservation Checking

**Goal**: Проверить что для всех входных данных где условие бага НЕ выполняется, исправленная функция производит тот же результат что и оригинальная функция.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT SavingSystem_original(input) = SavingSystem_fixed(input)
END FOR
```

**Testing Approach**: Property-based тестирование рекомендуется для проверки сохранения поведения потому что:
- Оно автоматически генерирует множество тестовых случаев по всему домену входных данных
- Оно ловит граничные случаи которые могут пропустить ручные unit тесты
- Оно предоставляет сильные гарантии что поведение не изменилось для всех не-багованных входных данных

**Test Plan**: Наблюдать поведение на НЕИСПРАВЛЕННОМ коде сначала для сохранений в редакторе и примитивных типов, затем написать property-based тесты захватывающие это поведение.

**Test Cases**:
1. **Editor Save Preservation**: Наблюдать что сохранение в редакторе работает корректно на неисправленном коде, затем написать тест чтобы проверить что это продолжает работать после исправления
2. **Primitive Types Preservation**: Наблюдать что сохранение примитивных типов (int, string, bool) работает в WebGL на неисправленном коде, затем написать тест чтобы проверить что это продолжает работать
3. **ISaveable Interface Preservation**: Наблюдать что SaveableEntity корректно вызывает CaptureState/RestoreState на неисправленном коде, затем написать тест чтобы проверить что это продолжает работать
4. **Hotkey Preservation**: Наблюдать что горячие клавиши работают на неисправленном коде, затем написать тест чтобы проверить что они продолжают работать

### Unit Tests

- Тест сериализации Vector3 через WebSavingAdapter в WebGL
- Тест десериализации Quaternion через WebSavingAdapter в WebGL
- Тест сериализации Color через WebSavingAdapter в WebGL
- Тест что ObjectStateSaver использует ISaveable интерфейс без создания отдельного файла
- Тест что CompanionManager корректно десериализует JObject с Unity типами
- Тест граничных случаев (пустые сохранения, null значения, отсутствующие поля)

### Property-Based Tests

- Генерировать случайные игровые состояния с Unity типами и проверять что они корректно сохраняются/загружаются в WebGL
- Генерировать случайные конфигурации ObjectStateSaver и проверять что состояния сохраняются в основном файле
- Генерировать случайные данные компаньонов и проверять что десериализация через JObject работает корректно
- Тестировать что все не-WebGL операции продолжают работать одинаково до и после исправления

### Integration Tests

- Полный цикл сохранения/загрузки игры в WebGL билде с компаньонами и объектами
- Переключение между сценами с сохранением состояний объектов
- Проверка что YandexSDK корректно получает и возвращает данные с Unity типами
- Проверка что горячие клавиши работают в WebGL билде после исправления
