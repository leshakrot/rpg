# Player Homestead System - Designer Guide

## Overview

The Player Homestead System allows players to construct, upgrade, and manage buildings on their personal estate. This guide explains how to create and configure buildings, requirements, and building slots.

## Table of Contents

1. [Creating Building Data Assets](#creating-building-data-assets)
2. [Creating Requirement Assets](#creating-requirement-assets)
3. [Setting Up Building Slots](#setting-up-building-slots)
4. [Building Prefab Requirements](#building-prefab-requirements)
5. [Example Configurations](#example-configurations)

---

## Creating Building Data Assets

Building Data assets define all properties of a building type.

### How to Create

1. Right-click in the Project window
2. Select **Create > Homestead > Building Data**
3. Name the asset (e.g., `House_Basic.asset`)
4. Save in `Assets/Internal Assets/Homestead/Buildings/`

### Configuration Fields

#### Identification
- **Building Id**: Unique identifier string (e.g., "house_basic")
- **Display Name**: Human-readable name (e.g., "Basic House")
- **Description**: Multi-line text describing the building
- **Icon**: Sprite displayed in UI (recommended: 128x128 pixels)
- **Category**: Optional category for UI organization (e.g., "Housing")

#### Building Configuration
- **Max Level**: Maximum upgrade level (e.g., 3)
- **Max Instances**: Maximum allowed (-1 = unlimited)

#### Prefabs
- **Level Prefabs**: Array of GameObjects for each level
  - Index 0 = Level 1, Index 1 = Level 2, etc.

#### Requirements Per Level
- **Level Requirements**: Array of requirement configurations per level

---

## Creating Requirement Assets

Requirements determine what players need to construct or upgrade buildings.

### 1. Inventory Requirement

Requires specific items from player inventory.

**Create:** Right-click > **Create > Homestead > Requirements > Inventory**

**Configuration:**
- **Required Item**: Reference to InventoryItem asset
- **Required Quantity**: Number of items needed

**Example:** Wood x10, Stone x5

### 2. Currency Requirement

Requires gold/currency from player purse.

**Create:** Right-click > **Create > Homestead > Requirements > Currency**

**Configuration:**
- **Required Amount**: Amount of gold needed

**Example:** 500 Gold

### 3. Quest Requirement

Requires quest completion to unlock buildings.

**Create:** Right-click > **Create > Homestead > Requirements > Quest**

**Configuration:**
- **Required Quest**: Reference to Quest asset
- **Specific Objective**: (Optional) Specific objective to check

**Example:** Complete "Build Your Home" quest

### 4. Level Requirement

Requires player to reach specific character level.

**Create:** Right-click > **Create > Homestead > Requirements > Level**

**Configuration:**
- **Required Level**: Minimum player level

**Example:** Player Level 10

### 5. Prerequisite Building Requirement

Requires another building to be constructed first.

**Create:** Right-click > **Create > Homestead > Requirements > Prerequisite Building**

**Configuration:**
- **Prerequisite Building**: Reference to BuildingData asset
- **Minimum Level**: Minimum level of prerequisite

**Example:** Requires Basic House Level 2

### 6. Logical Requirement (AND/OR)

Combines multiple requirements with logical operators.

**Create:** Right-click > **Create > Homestead > Requirements > Logical (AND/OR)**

**Configuration:**
- **Logical Operator**: Choose AND or OR
- **Sub Requirements**: Array of requirements to combine

**Example:** (Wood x10 OR Stone x10) AND Gold 100

---

## Setting Up Building Slots

Building Slots are locations in the homestead scene where buildings can be constructed.

### How to Create

1. Open the homestead scene
2. Create an empty GameObject
3. Name it (e.g., `BuildingSlot_House_01`)
4. Position it where the building should appear
5. Add the **BuildingSlot** component

### Configuration

- **Slot Id**: Unique identifier (leave empty to auto-generate)
- **Allowed Buildings**: Array of BuildingData assets allowed on this slot
- **Slot Category**: Category for UI organization
- **Interaction Radius**: Distance player can interact from (default: 3.0)
- **Interaction Prompt**: Text shown to player

### Positioning Tips

- Position on flat terrain
- Leave space between slots for building prefabs
- Use Scene view gizmo (yellow sphere) to visualize interaction radius

---

## Building Prefab Requirements

Building prefabs are the visual GameObjects instantiated when buildings are constructed.

### Basic Requirements

1. Create a GameObject with visual elements
2. Add interactive components (optional)
3. Save as prefab in `Assets/Internal Assets/Homestead/Prefabs/Buildings/`

### Naming Convention

- Use level suffix: `House_Basic_Level1.prefab`, `House_Basic_Level2.prefab`

### Transform Setup

- Prefab origin (0,0,0) at ground level
- Building centered on origin
- Rotation at identity (0,0,0)

### Interactive Components

Building prefabs can contain any Unity components:

- **CraftingStation**: Enable crafting recipes
- **ProcessingStation**: Enable resource processing
- **ChestInventory**: Provide storage
- **NPCs**: Add dialogue and quests
- **Custom Scripts**: Any gameplay functionality

The system does not modify existing components - it simply instantiates the prefab.

---

## Example Configurations

### Example 1: Basic House

**BuildingData Configuration:**
```
Building Id: house_basic
Display Name: Basic House
Description: A simple dwelling for rest and storage
Max Level: 3
Max Instances: -1 (unlimited)
Category: Housing

Level Prefabs:
  [0] House_Basic_Level1.prefab
  [1] House_Basic_Level2.prefab
  [2] House_Basic_Level3.prefab

Level Requirements:
  [0] Level 1 Requirements:
    - Req_Wood_20.asset (Inventory: Wood x20)
    - Req_Stone_10.asset (Inventory: Stone x10)
    - Req_Gold_100.asset (Currency: 100 Gold)
  
  [1] Level 2 Requirements:
    - Req_Wood_40.asset (Inventory: Wood x40)
    - Req_Stone_30.asset (Inventory: Stone x30)
    - Req_Gold_250.asset (Currency: 250 Gold)
    - Req_Level_5.asset (Level: Player Level 5)
  
  [2] Level 3 Requirements:
    - Req_Wood_80.asset (Inventory: Wood x80)
    - Req_Stone_60.asset (Inventory: Stone x60)
    - Req_Gold_500.asset (Currency: 500 Gold)
    - Req_Level_10.asset (Level: Player Level 10)
```

### Example 2: Workshop (with Prerequisite)

**BuildingData Configuration:**
```
Building Id: workshop
Display Name: Workshop
Description: Craft items and process resources
Max Level: 2
Max Instances: -1
Category: Production

Level Prefabs:
  [0] Workshop_Level1.prefab
  [1] Workshop_Level2.prefab

Level Requirements:
  [0] Level 1 Requirements:
    - Req_House_Basic.asset (Prerequisite: Basic House Level 1)
    - Req_Wood_30.asset (Inventory: Wood x30)
    - Req_Iron_10.asset (Inventory: Iron x10)
    - Req_Gold_200.asset (Currency: 200 Gold)
  
  [1] Level 2 Requirements:
    - Req_Wood_60.asset (Inventory: Wood x60)
    - Req_Iron_30.asset (Inventory: Iron x30)
    - Req_Gold_400.asset (Currency: 400 Gold)
    - Req_Quest_Crafting.asset (Quest: Complete "Master Craftsman")
```

### Example 3: Storage Shed (with Instance Limit)

**BuildingData Configuration:**
```
Building Id: storage_shed
Display Name: Storage Shed
Description: Store extra items and resources
Max Level: 1
Max Instances: 3 (limited to 3 total)
Category: Storage

Level Prefabs:
  [0] Storage_Shed.prefab

Level Requirements:
  [0] Level 1 Requirements:
    - Req_Wood_15.asset (Inventory: Wood x15)
    - Req_Gold_50.asset (Currency: 50 Gold)
```

### Example 4: Advanced Building (with Logical Requirements)

**BuildingData Configuration:**
```
Building Id: forge
Display Name: Forge
Description: Smelt ores and craft metal equipment
Max Level: 2
Max Instances: 1
Category: Production

Level Prefabs:
  [0] Forge_Level1.prefab
  [1] Forge_Level2.prefab

Level Requirements:
  [0] Level 1 Requirements:
    - Req_Workshop_Level2.asset (Prerequisite: Workshop Level 2)
    - Req_Wood_OR_Coal.asset (Logical OR: Wood x50 OR Coal x20)
    - Req_Stone_50.asset (Inventory: Stone x50)
    - Req_Gold_300.asset (Currency: 300 Gold)
    - Req_Level_8.asset (Level: Player Level 8)
  
  [1] Level 2 Requirements:
    - Req_Advanced_Materials.asset (Logical AND: Iron x30 AND Steel x10)
    - Req_Gold_600.asset (Currency: 600 Gold)
    - Req_Quest_Blacksmith.asset (Quest: Complete "Master Blacksmith")
```

---

## Tips and Best Practices

### Building Design
- Start with simple buildings and add complexity gradually
- Use clear, descriptive names for all assets
- Test each building thoroughly before adding to production
- Balance costs with gameplay benefits

### Requirements
- Create reusable requirement assets
- Use logical requirements for flexible unlock conditions
- Don't make early buildings too expensive
- Gate powerful buildings behind quests or levels

### Building Slots
- Plan your homestead layout before placing slots
- Group similar building types together
- Leave room for player navigation
- Consider visual aesthetics and flow

### Prefabs
- Keep prefab complexity reasonable for performance
- Use LODs for detailed buildings
- Test prefab instantiation and destruction
- Ensure all interactive components work correctly

---

## Troubleshooting

### Building doesn't appear in construction menu
- Check that BuildingData is assigned to BuildingSlot's Allowed Buildings
- Verify BuildingData has valid Building Id
- Ensure prefab is assigned for Level 1

### Requirements not working
- Verify requirement assets are properly configured
- Check that player has required components (Inventory, Purse, etc.)
- Test requirements individually before combining

### Building prefab issues
- Ensure prefab origin is at ground level
- Check that prefab is not null in BuildingData
- Verify prefab doesn't have conflicting components

### Save/Load issues
- Ensure Building Id is unique and never changes
- Check that EstateManager is in the scene
- Verify SavingSystem is properly configured

---

## Additional Resources

- **Scripts Location:** `Assets/Scripts/Homestead/`
- **Example Assets:** `Assets/Internal Assets/Homestead/Buildings/`
- **Test Scene:** `Assets/Internal Assets/Homestead/Scenes/Homestead.unity`

For technical documentation, see the XML comments in the source code files.
