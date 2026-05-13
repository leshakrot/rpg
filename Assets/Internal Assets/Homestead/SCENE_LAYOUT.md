# Homestead Scene Layout

## Scene Hierarchy

```
Homestead.unity
├── Main Camera
├── Directional Light
├── Ground (Plane 100x100)
├── EstateManager
│   └── Buildings (empty parent for instantiated buildings)
├── BuildingSlot_1 (Position: -20, 0, 20)
├── BuildingSlot_2 (Position: 0, 0, 20)
├── BuildingSlot_3 (Position: 20, 0, 20)
├── BuildingSlot_4 (Position: -20, 0, 0)
├── BuildingSlot_5 (Position: 20, 0, 0)
└── ConstructionMenuCanvas
    └── MenuPanel
        ├── BuildingListSection
        │   └── BuildingListScrollView
        │       └── Viewport
        │           └── Content (building buttons spawn here)
        ├── DetailsSection
        │   ├── BuildingNameText
        │   ├── BuildingIcon
        │   ├── BuildingLevelText
        │   ├── BuildingDescriptionText
        │   ├── RequirementsLabel
        │   ├── RequirementsScrollView
        │   │   └── Viewport
        │   │       └── Content (requirement items spawn here)
        │   ├── BuildButton
        │   ├── UpgradeButton
        │   ├── DemolishButton
        │   └── CloseButton
        └── ConfirmationDialog (hidden by default)
            └── ConfirmPanel
                ├── ConfirmationText
                ├── YesButton
                └── NoButton
```

## Top-Down View (Building Slot Positions)

```
                    Z
                    ↑
                    |
        [-20,20]    [0,20]    [20,20]
            ●         ●          ●
         Slot 1    Slot 2    Slot 3
                    |
                    |
        [-20,0]               [20,0]
            ●                   ●
         Slot 4              Slot 5
                    |
    ----------------+---------------→ X
                    |
                    |
```

## Building Slot Configuration

| Slot | Position | Allowed Buildings | Notes |
|------|----------|-------------------|-------|
| Slot 1 | (-20, 0, 20) | All | Top-left corner |
| Slot 2 | (0, 0, 20) | All | Top-center |
| Slot 3 | (20, 0, 20) | All | Top-right corner |
| Slot 4 | (-20, 0, 0) | Storage, Workshop | Middle-left |
| Slot 5 | (20, 0, 0) | Storage, Workshop | Middle-right |

**Interaction Radius**: 3.0 units (all slots)

## UI Layout (Screen Space)

```
┌─────────────────────────────────────────────────────────────┐
│                     Construction Menu                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │                                                        │  │
│  │  ┌──────────┐  ┌─────────────────────────────────┐  │  │
│  │  │ Building │  │  Building Name                   │  │  │
│  │  │  List    │  │  [Icon]  Level 1 / 3            │  │  │
│  │  │          │  │                                   │  │  │
│  │  │ • House  │  │  Description text here...        │  │  │
│  │  │ • Workshop│  │                                   │  │  │
│  │  │ • Storage│  │  Requirements:                    │  │  │
│  │  │          │  │  ┌─────────────────────────────┐ │  │  │
│  │  │          │  │  │ ✓ 10x Wood                  │ │  │  │
│  │  │          │  │  │ ✗ 100 Gold (need 50 more)   │ │  │  │
│  │  │          │  │  └─────────────────────────────┘ │  │  │
│  │  │          │  │                                   │  │  │
│  │  │          │  │  [Build] [Upgrade] [Demolish]    │  │  │
│  │  │          │  │  [Close]                          │  │  │
│  │  └──────────┘  └─────────────────────────────────┘  │  │
│  │                                                        │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Menu Panel**: 60% of screen (centered)
**Building List**: 35% of menu width (left side)
**Details Section**: 65% of menu width (right side)

## Confirmation Dialog (Overlay)

```
┌─────────────────────────────────────────────────────────────┐
│                    [Dark Overlay]                            │
│                                                              │
│              ┌──────────────────────────┐                   │
│              │                          │                   │
│              │  Are you sure you want   │                   │
│              │  to demolish this        │                   │
│              │  building?               │                   │
│              │                          │                   │
│              │   [Yes]        [No]      │                   │
│              │                          │                   │
│              └──────────────────────────┘                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Overlay**: Full screen, semi-transparent black
**Panel**: 400x200 pixels, centered

## Component References

### EstateManager
- **homesteadIdentifier**: "main_homestead"
- **buildingsParent**: Transform reference to "Buildings" GameObject

### BuildingSlot (each)
- **slotId**: Auto-generated GUID (or manually set)
- **allowedBuildings**: Array of BuildingData references
- **slotCategory**: "General"
- **interactionRadius**: 3.0
- **interactionPrompt**: "Open Construction Menu"

### ConstructionMenuUI
All UI element references are wired automatically by the setup script.

## Prefab Structure

### BuildingButton.prefab
```
BuildingButton (Image + Button)
├── Icon (Image, 40x40)
└── Text (TextMeshProUGUI)
```

### RequirementItem.prefab
```
RequirementItem (TextMeshProUGUI)
```

### ConstructionMenu.prefab
```
ConstructionMenuCanvas (Canvas)
└── [Complete hierarchy as shown in Scene Hierarchy above]
```

## Material Colors

- **Ground**: RGB(0.3, 0.5, 0.3) - Green-ish
- **Menu Panel**: RGBA(0.1, 0.1, 0.1, 0.95) - Dark gray, semi-transparent
- **Building List Background**: RGB(0.15, 0.15, 0.15) - Darker gray
- **Button Background**: RGB(0.3, 0.3, 0.3) - Medium gray
- **Confirmation Overlay**: RGBA(0, 0, 0, 0.8) - Black, 80% opacity

## Lighting

- **Directional Light**: 
  - Rotation: (50, -30, 0)
  - Intensity: 1.0
  - Color: White
- **Ambient**: Skybox mode, intensity 1.0

## Camera

- **Position**: Default (0, 1, -10)
- **Rotation**: Default (0, 0, 0)
- **Projection**: Perspective
- **Field of View**: 60°

## Notes

- All positions are in world space (Unity units)
- UI uses RectTransform with anchors for responsive layout
- Building slots use Gizmos for editor visualization (yellow wireframe sphere)
- Menu is hidden by default (menuPanel.SetActive(false))
- Confirmation dialog is hidden by default
