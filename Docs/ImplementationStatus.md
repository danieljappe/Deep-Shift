# Deep Shift — Implementation Status

Last updated: 2026-03-15 (Phase 5 — surface camp, scene orchestration)

---

## Phase 1 — Infrastructure & Prototype

| Feature | Status | Notes |
|---|---|---|
| Git setup | ✅ Done | |
| Folder structure | ✅ Done | Assets/_Project layout in place |
| ScriptableObject event bus | ✅ Done | GameEventSO, GameEventSOTyped<T>, IGameEventListener |
| Core event SO assets | ✅ Done | Generated via EventAssetGenerator editor tool |
| GameManager + Bootstrap | ✅ Done | SceneController drives all transitions; GameManager tracks state |
| EconomyManager | ⬜ Stubbed | |
| ORIONDialogueManager | ⬜ Stubbed | |
| Grid tilemap + tile destruction | ✅ Done | MineGrid, CA cave gen, RoomPlacer, HitTile |
| Basic hoist timer + death penalty | ✅ Done | HoistTerminal, DeathPenaltySystem, DeathScreenUI |

---

## Phase 2 — Core Gameplay Loop

| Feature | Status | Notes |
|---|---|---|
| Player movement | ✅ Done | PlayerController, edge-based wall collision |
| Pointer-based aiming | ✅ Done | Mouse/gamepad aim direction fed to DrillController |
| Charged drill mechanic | ✅ Done | DrillController, 0.8s charge, charge indicator |
| Ore pickup + inventory | ✅ Done | OrePickup, PlayerInventory, OreHUD |
| Drill damages tiles | ✅ Done | MineGrid.HitTile, TileDestroyed event |
| Drill damages enemies | ✅ Done | IDamageable interface, Physics2D.OverlapCircle |
| Player health system | ✅ Done | PlayerHealthSystem, TakeDamage/Heal/ResetToFull, HealthBarHUD |
| Player death | ✅ Done | PlayerDied event, 50% ore loss, debt accrual |
| Death screen + new shift | ✅ Done | DeathScreenUI, ShiftStarted → RestartFromFloor1 |
| Hoist terminal + countdown | ✅ Done | HoistTerminal, HoistCountdownHUD, 8s countdown |
| Floor regeneration | ✅ Done | MineTestBootstrap, HoistExtracted → RegenerateFloor |
| Floating text feedback | ✅ Done | FloatingText.Spawn() |

---

## Phase 3 — Enemies

| Feature | Status | Notes |
|---|---|---|
| IDamageable interface | ✅ Done | Scripts/Core/IDamageable.cs |
| DrillVibrationBroadcaster | ✅ Done | Translates TileDestroyed → DrillImpact (Vector2Int) |
| GameEventSO_Vector2Int | ✅ Done | Typed event carrying grid position |
| BorerDataSO | ✅ Done | Read-only tunable stats SO |
| BorerController | ✅ Done | 4-state machine: IDLE→ALERT→AGGRO→DE_AGGRO→IDLE |
| Borer vibration detection | ✅ Done | Inner/outer radius + vibration counter + decay |
| Borer hoist swarm | ✅ Done | All Borers alert on HoistCalled regardless of distance |
| Borer lunge attack | ✅ Done | LungeRoutine, 0.15s dash, EnemyDealDamage event |
| Borer chitin drop | ✅ Done | Random drop on death, credits via OrePickedUp channel |
| Threat budget spawner | ✅ Done | EnemySpawnDataSO, ThreatBudgetConfig, EnemySpawner |
| Borer prefab | ⬜ Pending | Requires manual assembly in Unity Editor — see prefab comment in BorerController.cs |
| Borer BorerData asset | ⬜ Pending | Create via DeepShift/Enemies/BorerData menu |
| EnemySpawnData asset | ⬜ Pending | Create via DeepShift/Enemies/EnemySpawnData menu |
| Borer health bar | ✅ Done | BorerHealthBar component; hidden idle, visible on hit/aggro |
| Borer damage numbers | ✅ Done | FloatingText.Spawn in TakeDamage |
| Borer debug logs removed | ✅ Done | All [Borer] prefix Debug.Log calls removed |
| More enemy types | ⬜ Planned | |

---

## Phase 3b — Ranged Weapon & Hotbar

| Feature | Status | Notes |
|---|---|---|
| WeaponDataSO | ✅ Done | Scripts/Weapons/WeaponDataSO.cs; create BoltPistol.asset via menu |
| RangedWeaponController | ✅ Done | Fire input, ammo economy, code-spawned projectiles |
| Projectile | ✅ Done | Dynamic RB, tile + lifetime destruction, IDamageable hit |
| AmmoPickup | ✅ Done | Static SpawnAt factory; cyan cyan tint; auto-collect |
| HotbarController | ✅ Done | Keys 1/2 switch slots; enables/disables components |
| HotbarHUD | ✅ Done | OnGUI slot boxes; VEKTRA orange active highlight; ammo counter |
| Weapon event assets | ✅ Done | WeaponSlotChanged, WeaponAmmoChanged, WeaponFired, ProjectileHitEnemy |
| Ammo scatter | ⬜ Removed | No automatic scatter — place pickups manually if needed |
| Floor cleanup | ✅ Done | Projectile destroyed on floor regen/shift restart |
| BoltPistol.asset | ⬜ Pending | Create ScriptableObjects/Gear/BoltPistol.asset via menu |
| Inspector wiring | ⬜ Pending | Assign event SOs + components per plan wiring table |

---

## Phase 4 — Hazards

| Feature | Status | Notes |
|---|---|---|
| GasTileDataSO | ✅ Done | Tile data type for gas cells |
| GasDamageSystem | ✅ Done | Burst on drill (20 dmg), tick on standing (5/s) |
| Gas breach message | ✅ Done | "GAS POCKET BREACHED" OnGUI overlay |
| Damage flash (all sources) | ✅ Done | Red screen tint centralised in PlayerHealthSystem |
| Collapse hazard | ⬜ Planned | |
| Water/lava hazard | ⬜ Planned | |

---

## Phase 5 — Economy & Progression

| Feature | Status | Notes |
|---|---|---|
| Ore credits tracking | ✅ Done | ExtractionSystem: HoistExtracted → sum inventory → AddOreCredits → ClearInventory |
| Debt tokens | ✅ Done | DeathPenaltySystem adds _revivalFee debt on PlayerDied |
| Economy save/load | ✅ Done | EconomyManager.SaveToJson/LoadFromJson (JsonUtility, persistentDataPath) |
| Economy HUD | ✅ Done | EconomyHUD: credits (orange) + debt (red) top-right; event-driven |
| Death clears inventory | ✅ Done | DeathPenaltySystem.ClearInventory() — all ore lost on death per design |
| VEKTRA Rep | ⬜ Planned | EconomyManager.ChangeVektraRep exists; no sources yet |
| Black Market Chips | ⬜ Planned | EconomyManager.AddBlackMarketChips exists; no sources yet |
| Meta-upgrade tree | ⬜ Planned | |
| SurfaceCamp scene | ✅ Done | SurfaceCampUI: credits, debt, net balance, Begin Shift button |
| Scene orchestration | ✅ Done | SceneController: Bootstrap→SurfaceCamp→Mine→SurfaceCamp loop |
| Vendors | ⬜ Planned | |

---

## Phase 6 — ORION Dialogue

| Feature | Status | Notes |
|---|---|---|
| ORIONDialogueManager | ⬜ Stubbed | Priority queue, 3s cooldown, priority 2 interrupts |
| ElevenLabs TTS integration | ⬜ Planned | |
| VEKTRA-orange subtitle widget | ⬜ Planned | |
| Scripted dialogue lines | ⬜ Planned | |

---

## Known Gaps / Tech Debt

- Debug logs still present in PlayerController (added during lunge-damage debugging)
- Ore pickup spawned in code (no prefab) — `DrillController.SpawnOrePickup()`
- Chitin shard spawns credits directly via OrePickedUp; no ChitinShardPickup prefab yet
- Wall tiles have no physics colliders — solid tiles only exist logically in MineGrid
- EventAssetGenerator creates all event SO assets; re-run after adding new event types
