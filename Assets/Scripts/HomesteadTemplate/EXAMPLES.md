# Примеры использования Homestead Template

## 📖 Содержание

1. [Создание собственного здания](#создание-собственного-здания)
2. [Создание кастомных требований](#создание-кастомных-требований)
3. [Настройка слотов](#настройка-слотов)
4. [Программное управление](#программное-управление)
5. [Интеграция с игровыми системами](#интеграция-с-игровыми-системами)

---

## Создание собственного здания

### Пример 1: Простое здание

```csharp
// 1. Создайте BuildingData через меню
// Create → Homestead → Building Data

// 2. Настройте в Inspector:
Building ID: "my_tavern"
Display Name: "Таверна"
Description: "Место для отдыха и торговли"
Category: "Коммерческие здания"
Max Level: 2
Max Instances: 1

// 3. Создайте префабы для каждого уровня
Level Prefabs:
  [0] Tavern_Level1.prefab
  [1] Tavern_Level2.prefab

// 4. Настройте требования
Level Requirements:
  Level 1:
    - Wood x15
    - Gold 200
  Level 2:
    - Wood x30
    - Stone x20
    - Gold 500
```

### Пример 2: Здание с пререквизитами

```csharp
// Здание, которое требует другое здание

Building ID: "blacksmith"
Display Name: "Кузница"
Prerequisites: Workshop (Level 2+)

Level Requirements:
  Level 1:
    - Prerequisite: Workshop (Level 2)
    - Iron x20
    - Gold 400
```

### Пример 3: Здание с логическими требованиями

```csharp
// Здание с выбором ресурсов

Building ID: "garden"
Display Name: "Сад"

Level Requirements:
  Level 1:
    - OR:
        - Seeds x50
        - Saplings x10
    - Gold 150
```

---

## Создание кастомных требований

### Пример 1: Требование к времени суток

```csharp
using UnityEngine;
using RPG.Homestead;

[CreateAssetMenu(menuName = "Homestead/Requirements/Time of Day")]
public class TimeOfDayRequirement : BuildingRequirement
{
    [SerializeField] private int requiredHour = 12; // Полдень
    
    public override bool Validate(bool skipResourceChecks = false)
    {
        // Получаем текущее время из вашей системы времени
        TimeManager timeManager = FindObjectOfType<TimeManager>();
        if (timeManager == null) return false;
        
        return timeManager.CurrentHour == requiredHour;
    }
    
    public override string GetDescription()
    {
        return $"Можно строить только в {requiredHour}:00";
    }
    
    public override string GetMissingInfo()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();
        if (timeManager == null) return "Time manager not found";
        
        return $"Текущее время: {timeManager.CurrentHour}:00 (нужно: {requiredHour}:00)";
    }
}
```

### Пример 2: Требование к репутации

```csharp
using UnityEngine;
using RPG.Homestead;

[CreateAssetMenu(menuName = "Homestead/Requirements/Reputation")]
public class ReputationRequirement : BuildingRequirement
{
    [SerializeField] private string factionName;
    [SerializeField] private int requiredReputation = 100;
    
    public override bool Validate(bool skipResourceChecks = false)
    {
        ReputationSystem repSystem = FindObjectOfType<ReputationSystem>();
        if (repSystem == null) return false;
        
        return repSystem.GetReputation(factionName) >= requiredReputation;
    }
    
    public override string GetDescription()
    {
        return $"Репутация с {factionName}: {requiredReputation}+";
    }
    
    public override string GetMissingInfo()
    {
        ReputationSystem repSystem = FindObjectOfType<ReputationSystem>();
        if (repSystem == null) return "Reputation system not found";
        
        int current = repSystem.GetReputation(factionName);
        int missing = Mathf.Max(0, requiredReputation - current);
        
        return $"Нужно еще {missing} репутации (текущая: {current}/{requiredReputation})";
    }
}
```

### Пример 3: Требование к достижениям

```csharp
using UnityEngine;
using RPG.Homestead;

[CreateAssetMenu(menuName = "Homestead/Requirements/Achievement")]
public class AchievementRequirement : BuildingRequirement
{
    [SerializeField] private string achievementId;
    
    public override bool Validate(bool skipResourceChecks = false)
    {
        AchievementManager achievements = FindObjectOfType<AchievementManager>();
        if (achievements == null) return false;
        
        return achievements.IsUnlocked(achievementId);
    }
    
    public override string GetDescription()
    {
        return $"Достижение: {achievementId}";
    }
    
    public override string GetMissingInfo()
    {
        return $"Достижение '{achievementId}' не получено";
    }
}
```

---

## Настройка слотов

### Пример 1: Специализированный слот

```csharp
// В Inspector BuildingSlot:

Slot ID: "commercial_district_1"
Slot Category: "Коммерческий район"
Allowed Buildings:
  - Tavern
  - Shop
  - Market
Interaction Radius: 5.0
```

### Пример 2: Программное создание слота

```csharp
using UnityEngine;
using RPG.Homestead;

public class SlotCreator : MonoBehaviour
{
    public BuildingData[] allowedBuildings;
    
    void CreateSlot(Vector3 position)
    {
        GameObject slotObj = new GameObject("BuildingSlot_Dynamic");
        slotObj.transform.position = position;
        
        BuildingSlot slot = slotObj.AddComponent<BuildingSlot>();
        
        // Используем рефлексию для установки полей
        var slotIdField = typeof(BuildingSlot).GetField("slotId", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        slotIdField?.SetValue(slot, System.Guid.NewGuid().ToString());
        
        var allowedField = typeof(BuildingSlot).GetField("allowedBuildings", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        allowedField?.SetValue(slot, allowedBuildings);
    }
}
```

---

## Программное управление

### Пример 1: Построить здание программно

```csharp
using UnityEngine;
using RPG.Homestead;

public class BuildingController : MonoBehaviour
{
    public void BuildHouse(BuildingSlot slot)
    {
        // Получаем BuildingData
        BuildingData houseData = Resources.Load<BuildingData>("Buildings/House_Basic");
        
        // Строим через EstateManager
        bool success = EstateManager.Instance.ConstructBuilding(slot, houseData);
        
        if (success)
        {
            Debug.Log("Дом построен!");
        }
        else
        {
            Debug.Log("Не удалось построить дом");
        }
    }
}
```

### Пример 2: Улучшить все здания типа

```csharp
using UnityEngine;
using RPG.Homestead;
using System.Collections.Generic;

public class MassUpgrader : MonoBehaviour
{
    public void UpgradeAllHouses()
    {
        // Получаем все дома
        List<BuildingInstance> houses = EstateManager.Instance
            .GetBuildingsOfType("house_basic");
        
        foreach (var house in houses)
        {
            // Находим слот
            BuildingSlot slot = FindSlotById(house.slotId);
            
            if (slot != null && house.CanUpgrade())
            {
                EstateManager.Instance.UpgradeBuilding(slot);
            }
        }
    }
    
    private BuildingSlot FindSlotById(string slotId)
    {
        BuildingSlot[] allSlots = FindObjectsOfType<BuildingSlot>();
        foreach (var slot in allSlots)
        {
            if (slot.GetSlotId() == slotId)
                return slot;
        }
        return null;
    }
}
```

### Пример 3: Проверка условий

```csharp
using UnityEngine;
using RPG.Homestead;

public class BuildingChecker : MonoBehaviour
{
    public bool CanBuildWorkshop()
    {
        // Проверяем, есть ли дом
        bool hasHouse = EstateManager.Instance.HasBuilding("house_basic");
        
        // Проверяем, есть ли дом нужного уровня
        bool hasHouseLevel2 = EstateManager.Instance
            .HasBuildingAtLevel("house_basic", 2);
        
        // Считаем количество мастерских
        int workshopCount = EstateManager.Instance
            .GetBuildingCount("workshop");
        
        return hasHouse && workshopCount < 2;
    }
}
```

---

## Интеграция с игровыми системами

### Пример 1: Награда за строительство

```csharp
using UnityEngine;
using RPG.Homestead;

public class BuildingRewards : MonoBehaviour
{
    void Start()
    {
        // Подписываемся на событие строительства
        EstateManager.Instance.OnBuildingConstructed += OnBuildingBuilt;
    }
    
    void OnBuildingBuilt(BuildingInstance building)
    {
        // Даем награду за строительство
        GiveReward(building.buildingData.BuildingId);
        
        // Показываем уведомление
        ShowNotification($"Построено: {building.buildingData.DisplayName}!");
        
        // Даем опыт
        GiveExperience(100);
    }
    
    void GiveReward(string buildingId)
    {
        switch (buildingId)
        {
            case "house_basic":
                // Награда за дом
                GiveGold(50);
                break;
            case "workshop":
                // Награда за мастерскую
                UnlockRecipe("advanced_crafting");
                break;
        }
    }
}
```

### Пример 2: Квестовая интеграция

```csharp
using UnityEngine;
using RPG.Homestead;
using RPG.Quests;

public class HomesteadQuests : MonoBehaviour
{
    void Start()
    {
        EstateManager.Instance.OnBuildingConstructed += CheckQuestProgress;
    }
    
    void CheckQuestProgress(BuildingInstance building)
    {
        QuestList questList = GameObject.FindGameObjectWithTag("Player")
            .GetComponent<QuestList>();
        
        // Проверяем квест "Построй 3 дома"
        if (building.buildingData.BuildingId == "house_basic")
        {
            int houseCount = EstateManager.Instance.GetBuildingCount("house_basic");
            
            if (houseCount >= 3)
            {
                // Завершаем квест
                CompleteQuest("build_three_houses");
            }
        }
    }
}
```

### Пример 3: Бонусы от зданий

```csharp
using UnityEngine;
using RPG.Homestead;
using RPG.Stats;

public class BuildingBonuses : MonoBehaviour
{
    void Start()
    {
        EstateManager.Instance.OnBuildingConstructed += ApplyBonus;
        EstateManager.Instance.OnBuildingUpgraded += ApplyBonus;
        EstateManager.Instance.OnBuildingDemolished += RemoveBonus;
    }
    
    void ApplyBonus(BuildingInstance building)
    {
        BaseStats playerStats = GameObject.FindGameObjectWithTag("Player")
            .GetComponent<BaseStats>();
        
        switch (building.buildingData.BuildingId)
        {
            case "house_basic":
                // Дом дает +10 HP за уровень
                int hpBonus = building.currentLevel * 10;
                playerStats.AddModifier("house_hp_bonus", hpBonus);
                break;
                
            case "workshop":
                // Мастерская дает скидку на крафт
                CraftingManager.Instance.SetCostMultiplier(0.9f);
                break;
                
            case "tower":
                // Башня дает +5 защиты за уровень
                int defenseBonus = building.currentLevel * 5;
                playerStats.AddModifier("tower_defense_bonus", defenseBonus);
                break;
        }
    }
    
    void RemoveBonus(string slotId)
    {
        // Удаляем бонусы при сносе
        BaseStats playerStats = GameObject.FindGameObjectWithTag("Player")
            .GetComponent<BaseStats>();
        
        playerStats.RemoveModifier("house_hp_bonus");
        playerStats.RemoveModifier("tower_defense_bonus");
    }
}
```

---

## 💡 Дополнительные советы

### Оптимизация

```csharp
// Кэшируйте ссылки на EstateManager
private EstateManager estateManager;

void Awake()
{
    estateManager = EstateManager.Instance;
}

// Используйте события вместо Update()
void Start()
{
    estateManager.OnBuildingConstructed += OnBuilt;
}
```

### Отладка

```csharp
// Добавьте логирование
void OnBuildingBuilt(BuildingInstance building)
{
    Debug.Log($"Built: {building.buildingData.DisplayName} " +
              $"at level {building.currentLevel} " +
              $"on slot {building.slotId}");
}

// Проверяйте состояние
void CheckHomesteadState()
{
    Debug.Log($"Total buildings: {estateManager.GetBuildingCount("")}");
    Debug.Log($"Unlocked buildings: {estateManager.IsBuildingUnlocked("house_basic")}");
}
```

### Тестирование

```csharp
// Создайте тестовые методы
[ContextMenu("Test: Build All")]
void TestBuildAll()
{
    BuildingSlot[] slots = FindObjectsOfType<BuildingSlot>();
    foreach (var slot in slots)
    {
        if (!slot.IsOccupied() && slot.GetAllowedBuildings().Length > 0)
        {
            EstateManager.Instance.ConstructBuilding(
                slot, 
                slot.GetAllowedBuildings()[0]
            );
        }
    }
}

[ContextMenu("Test: Demolish All")]
void TestDemolishAll()
{
    BuildingSlot[] slots = FindObjectsOfType<BuildingSlot>();
    foreach (var slot in slots)
    {
        if (slot.IsOccupied())
        {
            EstateManager.Instance.DemolishBuilding(slot);
        }
    }
}
```

---

## 📚 Дополнительные ресурсы

- **Полная документация**: `README.md`
- **API Reference**: `.kiro/specs/player-homestead-system/design.md`
- **Требования**: `.kiro/specs/player-homestead-system/requirements.md`

---

**Совет**: Начните с простых примеров и постепенно добавляйте сложность!
