# Bugfix Requirements Document

## Introduction

В WebGL билде на Яндекс Играх система сохранений не работает корректно, хотя в редакторе Unity все функционирует идеально. Проблема затрагивает три основных компонента: основную систему сохранений через YandexSDK, систему восстановления состояний объектов (ObjectStateSaver) и систему сохранения компаньонов (CompanionManager). Основная причина — отсутствие кастомных JSON конвертеров для Unity типов (Vector3, Quaternion, Color) в WebSavingAdapter, что приводит к неправильной десериализации данных в WebGL билде.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN WebSavingAdapter сериализует/десериализует данные для YandexSDK THEN система НЕ использует кастомные JSON конвертеры (Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter), что приводит к неправильной десериализации Unity типов

1.2 WHEN ObjectStateSaver сохраняет состояния объектов THEN система вызывает `savingSystem.Save("objectStates")` вместо использования основного файла сохранения, создавая отдельное сохранение которое не синхронизируется с основным

1.3 WHEN CompanionManager десериализует данные через JObject THEN система использует `JObject.ToObject<SaveData>()` без указания JsonSerializerSettings, что может терять информацию о типах и не использовать кастомные конвертеры

1.4 WHEN JSON десериализует числовые значения THEN система получает `double` вместо ожидаемых `float` или `int`, что может вызывать ошибки приведения типов в некоторых компонентах

### Expected Behavior (Correct)

2.1 WHEN WebSavingAdapter сериализует/десериализует данные для YandexSDK THEN система SHALL использовать те же кастомные JSON конвертеры (Vector3JsonConverter, QuaternionJsonConverter, ColorJsonConverter) и JsonSerializerSettings что и SavingSystem для обеспечения корректной обработки Unity типов

2.2 WHEN ObjectStateSaver сохраняет состояния объектов THEN система SHALL использовать основной файл сохранения вместо создания отдельного файла "objectStates", интегрируясь с общей системой сохранений через ISaveable интерфейс

2.3 WHEN CompanionManager десериализует данные через JObject THEN система SHALL использовать JsonSerializerSettings с кастомными конвертерами при вызове `JObject.ToObject<SaveData>()` для корректной десериализации Unity типов

2.4 WHEN JSON десериализует числовые значения THEN система SHALL корректно обрабатывать приведение типов через JsonSaveHelper или использовать правильные JsonSerializerSettings для всех операций десериализации

### Unchanged Behavior (Regression Prevention)

3.1 WHEN система сохранений работает в редакторе Unity (не WebGL) THEN система SHALL CONTINUE TO использовать локальное сохранение в файлы с существующей логикой

3.2 WHEN SavingSystem сохраняет/загружает данные в редакторе THEN система SHALL CONTINUE TO использовать кастомные JSON конвертеры и корректно обрабатывать Unity типы

3.3 WHEN SaveableEntity захватывает и восстанавливает состояние компонентов THEN система SHALL CONTINUE TO работать через ISaveable интерфейс без изменений

3.4 WHEN YandexSDK не загружен или недоступен THEN система SHALL CONTINUE TO корректно обрабатывать таймаут и продолжать работу с предупреждением

3.5 WHEN пользователь использует горячие клавиши (L - загрузка, S - сохранение, Delete - удаление) THEN система SHALL CONTINUE TO выполнять соответствующие операции

3.6 WHEN система определяет текущую платформу через isWebPlatform THEN логика SHALL CONTINUE TO корректно различать WebGL билд и редактор
