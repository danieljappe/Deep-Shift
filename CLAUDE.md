# Deep Shift — Claude Code Project Context

## Project Summary
2D top-down sci-fi mining roguelite in Unity 2D (URP).
Player is a contract miner for VEKTRA Corp — descend into procedurally generated
mines, drill ore, and extract before dying. Core tension: push deeper for better ore
vs. call the hoist and get out safely.

---

## Unity Setup
- Engine: Unity 2D (URP)
- Target: PC (Steam), mobile port later
- All scripts: Assets/_Project/Scripts/
- All ScriptableObjects: Assets/_Project/ScriptableObjects/
- All prefabs: Assets/_Project/Prefabs/
- Scenes: Bootstrap (entry point), SurfaceCamp (meta loop), Mine (run loop)

---

## Architecture

### Event Bus (most important rule)
All systems communicate exclusively via the ScriptableObject event bus in Scripts/Core/.
Never create direct references between systems. Always raise an event instead.

Event types:
- GameEventSO           — no payload
- GameEventSO_Int       — int payload
- GameEventSO_Float     — float payload
- GameEventSO_String    — string payload
- GameEventSO_Bool      — bool payload

All event SO assets live in: Assets/_Project/ScriptableObjects/Events/

### Singletons
Use DontDestroyOnLoad pattern. One instance check in Awake — destroy duplicate if found.
Singletons: GameManager, EconomyManager, ORIONDialogueManager, SaveManager.

### ScriptableObjects
All SOs use [CreateAssetMenu(menuName = "DeepShift/...")] with organised subpaths.
Example: "DeepShift/Events/Player/PlayerDied"
Data SOs are read-only at runtime — never mutate SO data during a run.

### Save System
Persistent state (camp upgrades, meta-progression, economy) saved as JSON via Newtonsoft.
Run state (current floor, ore carried, active gear) is ephemeral — lost on death by design.
Save file path: Application.persistentDataPath + "/deepsave.json"

---

## Core Systems Overview

### GameManager (Scripts/Core/)
- Owns GameState enum: MainMenu, SurfaceCamp, InShift, PostShift, GameOver
- ChangeState() raises the appropriate event — contains no gameplay logic

### EconomyManager (Scripts/Economy/)
- Tracks: oreCredits, debtTokens, vektraRep, blackMarketChips
- All changes go through methods that raise events after modifying values
- debtTokens clamped to >= 0

### Hoist System (Scripts/Hoist/)
- Player activates intercom terminal → 10s countdown begins → enemies alerted
- HoistCalled, HoistCountdownTick (float), HoistExtracted, HoistCancelled events
- Successful extraction: ore and gear preserved
- Death: all carried ore lost, random gear durability hit, debt added

### Mining System (Scripts/Mining/)
- Grid-based tile destruction — directional drill input
- Ore yield calculated per tile type (see ore tiers below)
- Raises OrePickedUp (int: credit value), TileDestroyed, OreVeinDiscovered (string: type)

### Death System (Scripts/Death/)
- On PlayerDied: calculate ore loss, gear durability deduction, debt amount
- Permanent meta-progression upgrades are NEVER lost on death
- Debt added to EconomyManager via event

### ORION Dialogue Manager (Scripts/ORION/)
- Subscribes to ORIONDialogueTrigger and ORIONPriorityOverride events
- Priority queue: 0 = tips, 1 = advisories, 2 = critical (death, hoist countdown)
- 3s cooldown between low-priority lines
- Priority 2 always interrupts and clears queue
- Voice: ElevenLabs TTS, warm/chipper corporate tone
- Always accompanied by subtitles in monospaced VEKTRA-orange font

---

## Ore Tiers
- Floor 01-03: Iron, Silicate         — common, low value
- Floor 04-07: Veritanium, Glacite    — uncommon, moderate value
- Floor 08-12: Voidstone, Crymite     — rare, high value
- Floor 13+:   Echovein Crystals      — legendary, extreme value/danger

---

## Currency Types
- Ore Credits       — primary currency, earned by selling extracted ore
- VEKTRA Rep        — reputation, unlocks deeper tiers and vendors
- Black Market Chips — rare drops, used with underground vendor
- Debt Tokens       — negative currency accrued on death

---

## Naming Conventions
- Events:      PlayerDied, HoistCalled, OrePickedUp (PascalCase, descriptive verb)
- Managers:    GameManager, EconomyManager, ORIONDialogueManager
- Data SOs:    OreDataSO, GearDataSO, UpgradeNodeSO
- Listeners:   GameEventListener, GameEventListener_Int etc.
- Interfaces:  IGameEventListener (I prefix)
- Enums:       GameState, OreType, GearSlot, HazardType (PascalCase)

---

## Code Rules
- XML summary comments on all public methods and public fields
- Single responsibility per class — no god objects
- No GameObject.Find(), no hardcoded strings for tags or layers (use constants)
- No direct cross-system references — use the event bus
- No gameplay logic in Manager singletons — they coordinate, not simulate
- Stubs and TODOs are acceptable during infrastructure phases — mark with // TODO:

---

## Folder Structure
Assets/_Project/
├── Scripts/
│   ├── Core/          # Event bus, GameManager, Bootstrap
│   ├── Mining/        # Tile destruction, ore yield, drill
│   ├── Hoist/         # Terminal, countdown, extraction
│   ├── Death/         # Death penalty, debt, gear damage
│   ├── Economy/       # All currency management
│   ├── ORION/         # Dialogue manager, voice queue
│   ├── Progression/   # Meta-tree, camp upgrades, contracts
│   ├── Enemies/       # AI, fauna, drones
│   ├── Hazards/       # Gas, collapses, water, lava
│   ├── UI/            # HUD, ORION widget, menus
│   └── Save/          # JSON persistence
├── ScriptableObjects/
│   ├── Events/        # All GameEventSO assets
│   ├── Ore/           # OreDataSO per type
│   ├── Gear/          # GearDataSO per item
│   └── Upgrades/      # UpgradeNodeSO per node
├── Prefabs/
│   ├── Player/
│   ├── Enemies/
│   ├── Tiles/
│   ├── UI/
│   └── Hoist/
├── Scenes/
├── Art/
│   ├── Sprites/
│   ├── Tiles/
│   ├── UI/
│   └── Animations/
└── Audio/
    ├── Music/
    ├── SFX/
    └── ORION/

---

## Implementation Status

Keep `Docs/ImplementationStatus.md` up to date. After every feature addition, bug fix,
or system change, update the relevant row(s) in that file before finishing the task.
Use ✅ Done / ⬜ Pending / ⬜ Planned / ⬜ Stubbed as status markers.

---

## Current Development Phase
Phase 1 — Infrastructure & Prototype
- [x] Git setup
- [ ] Folder structure
- [ ] ScriptableObject event bus
- [ ] Core event SO assets
- [ ] GameManager + Bootstrap
- [ ] EconomyManager stub
- [ ] ORIONDialogueManager stub
- [ ] Grid tilemap + tile destruction
- [ ] Basic hoist timer + death penalty logic