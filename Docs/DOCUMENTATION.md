# BattleRobots — Programming Documentation

> Unity 2022+ • C# • NavMesh • ScriptableObjects • TextMeshPro

This document explains the structure and logic of the project.

- [High-Level Architecture](#high-level-architecture)
- [Gameplay Flow & Lifecycles](#gameplay-flow--lifecycles)
  - [Builder Scene lifecycle](#builder-scene-lifecycle)
  - [Arena Scene lifecycle](#arena-scene-lifecycle)
  - [Per-frame AI & Combat loop](#per-frame-ai--combat-loop)
- [Data & Assembly](#data--assembly)
  - [Part Definitions (ScriptableObjects)](#part-definitions-scriptableobjects)
  - [Catalog](#catalog)
  - [Stats Composition](#stats-composition)
  - [Prefab & Socket Conventions](#prefab--socket-conventions)
- [Core Systems](#core-systems)
  - [RobotController (hub)](#robotcontroller-hub)
  - [Perception](#perception)
  - [Decision Layer & FSM](#decision-layer--fsm)
  - [Navigation & Geometry](#navigation--geometry)
  - [Targeting & Weapons](#targeting--weapons)
  - [Health & Damage](#health--damage)
  - [Pickups & Spawner](#pickups--spawner)
- [Arena UI & Game Loop](#arena-ui--game-loop)
- [Builder UI & Save/Load](#builder-ui--saveload)
- [Tuning & Balancing](#tuning--balancing)
- [Performance Notes](#performance-notes)
- [Conventions & Utilities](#conventions--utilities)

---

## High-Level Architecture

**Two scenes, one loop:**

- **BuilderScene** — build robots, tint them, save to disk (JSON), assign to player slots.
- **ArenaPrototypeScene** — spawn the selected robots, run repeated rounds, track wins.

**Key subsystems:**

- **Data:** `*Definition` ScriptableObjects + `BodyPartsCatalog`.
- **Assembly:** `RobotAssembler` spawns part prefabs into sockets and applies tints.
- **Stats:** `RobotStats` and `RobotStatsBuilder` compose final stats from parts.
- **AI:** `DecisionLayer` → `StateMachine` (Idle/Chase/Strafe/Retreat) with `StateTransitionHelper`.
- **Movement:** `CombatNavigator` + `NavMeshAgent`.
- **Targeting & Weapons:** `TargetingSolver`, `WeaponBase` → `ProjectileWeapon`, `Projectile`.
- **Perception:** LOS/FOV/overlaps with caching.
- **Health:** `RobotHealth` (armor, regen, death).
- **Pickups:** `Pickup` effects and `PickupSpawner`.
- **Arena Loop:** `ArenaGameLoop` (countdown → fight → winner → repeat), `ArenaScoreboard`, `ArenaPlayerPanels`.
- **Save/Load & Loader:** `BuildSerializer`, `SaveLoadManager`, `PlayerLoaderUI`, `SelectedRobotsStore`.

---

## Gameplay Flow & Lifecycles

### Builder Scene lifecycle

1. **Main Menu** → **Build** or **Player Loader** (`PanelManager`).
2. **Builder UI**
   - `DropdownManager` selects parts by index.
   - `ColorManager` selects tints.
   - `RobotAssembler.Apply(...)` spawns prefabs into sockets and applies tints.
   - `StatsPanel.UpdateStats()` shows composed stats via `RobotStatsBuilder.BuildFromIndices(...)`.
   - `SaveLoadManager.TrySave()` writes a `RobotBuildData` JSON via `BuildSerializer`.
3. **Player Loader**
   - `PlayerLoaderUI` lists saved JSONs.
   - Enforces **unique** assignments for 2–4 slots.
   - On Continue: selected `RobotBuildData` list is stored in `SelectedRobotsStore`.

### Arena Scene lifecycle

1. **ArenaBootstrap**
   - Reads builds from `SelectedRobotsStore`.
   - Instantiates the **runtime rig prefab** per robot.
   - `RobotAssembler.AssembleFromBuild(...)` spawns parts; returns transforms and weapon.
   - Stats composed via `RobotStatsBuilder.FillFromIds(...)` and pushed to `RobotController`, `RobotHealth`, `Perception`.
   - Wires up `RobotController` (lower/upper bodies, muzzle, weapon).
   - Optionally snaps to NavMesh.
2. **ArenaGameLoop**
   - Prepares start poses & scoreboard.
   - **Round loop:**
     - Reset robots → countdown → unlock → fight.
     - Polls alive robots; declares winner; increments score; repeats.
3. **Camera**
   - `ArenaCameraController` follows/ orbits current target; Tab cycles.

### Per-frame AI & Combat loop

Per robot, each frame:

```
RobotController.Update:
  - Query Perception (visible enemies/pickups)
  - DecisionLayer.Decide()  -> DecisionResult (Move intent + Fire target)
  - StateMachine.Tick()     -> CurrentState.Tick() (Chase/Strafe/Retreat/Idle)
       - Movement via CombatNavigator + NavMeshAgent
  - TargetingSolver/AimPoint and line-of-fire checks
  - Weapon.TryFireAt(...), spawn Projectile if cooldown ready
Projectile.Update:
  - Sweep raycast to avoid tunneling
  - On hit -> RobotHealth.TakeDamage()
ArenaGameLoop (outer loop):
  - Poll alive count; on single survivor -> scoreboard, next round
```

---

## Data & Assembly

### Part Definitions (ScriptableObjects)

- `FrameDefinition` — `baseHealth`, `baseArmor`, `baseWeight`.
- `LowerDefinition` — `baseSpeed`, `baseWeight`, `turningSpeed`.
- `WeaponDefinition` — `attackDamage`, `attackRange`, `attackSpeed` (shots/min), `baseWeight`.
- `CoreDefinition` — `armor` bonus, `attackSpeed` bonus.

These are pure data assets (no methods).

### Catalog

`BodyPartsCatalog` holds `FrameOption/LowerOption/WeaponOption/CoreOption` entries (ID + prefab + definition).  
Provides:
- Lookups by **index** (Builder) and by **ID** (Save/Load, Arena spawn).
- Safe getters and index finders.

### Stats Composition

`RobotStatsBuilder` merges definitions into a `RobotStats`:

- **Health/Armor/Weight** ← Frame (+ Core armor).
- **Speed/Turning** ← Lower (Speed affected by weight later in controller).
- **Damage/Range/AttackSpeed** ← Weapon (+ Core attackSpeed).
- **SightAngle** — default (120°) for now.

> Note: Attack speed is treated as **shots per minute** across the codebase.

### Prefab & Socket Conventions

Assembler expects these child transforms (case-sensitive names):

- **On Frame prefab** (spawned under `frameSocket`):
  - `WeaponMount`, `CoreMount`, `LowerMount`
  - `UpperBody` (optional; used as fallback for muzzle lookups)
  - `FrameVisual` (optional; tint root)
- **On Lower prefab** (spawned under `lowerSocket`):
  - `LowerBody` (optional)
  - `LowerVisual` (optional; tint root)
- **On Weapon prefab** (spawned under `WeaponMount`):
  - `Muzzle` (firing origin)
  - `WeaponVisual` (optional; tint root)
- **On Core prefab**:
  - `CoreVisual` (optional; tint root)

Tinting uses `TintUtility` via **MaterialPropertyBlock**; supported properties `_BaseColor` or `_Color`.

---

## Core Systems

### RobotController (hub)

Responsible for:
- Storing `RobotStats`, caching subsystem references.
- Wiring lower/upper/weapon/muzzle transforms.
- Computing effective movement values from stats (e.g., speed vs. weight).
- Exposing helpers (e.g., `GetFirePointTransform()`, `GetTargeting()`, `GetAgent()`).

**Important:** Reset/lock behavior between rounds is handled by `ArenaGameLoop`.

### Perception

`Perception` provides spatial awareness:
- **Visible enemies** — `OverlapSphere` within detection radius → FOV cone → optional LOS ray.
- **All opponents** — cached `FindObjectsByType<RobotController>(...)` every N seconds.
- **All pickups** — cached `FindObjectsByType<Pickup>(...)` every N seconds; cache invalidated on `Pickup.PickupsChanged`.

Tunable intervals via inspector fields. Uses `obstacleMask`, `enemyMask`, `pickupMask`.

### Decision Layer & FSM

- **DecisionLayer / PlayerDecisionLayer**: decides `DecisionResult`
  - `Move` intent: `Idle`, `ChaseEnemy`, `StrafeEnemy`, `ChasePickup`, `Retreat`.
  - Also sets a `FireTarget` if appropriate.
- **StateMachine** holds current state; `StateTransitionHelper` gates transitions (hysteresis to avoid flip-flop):
  - `Chase ↔ Strafe` flip gate
  - Target switch gate
  - Range tolerances
- **States**
  - `IdleState` — stops movement; waits for intent.
  - `ChaseState` — path toward pickup/enemy with cooldown and min repath distances.
  - `StrafeState` — alternates orbit direction, and periodically flips (default 3s) to prevent pathing deadlocks.”
  - `RetreatState` — sample cone of candidate retreat points; pick best by distance & LOS breaks. Retreat is triggered when health falls below ~30% and armor is also below 75%

### Navigation & Geometry

`CombatNavigator` centralizes movement math:

- `ComputeAttackRing(target, cushion)` — combines weapon range, agent radii, cushion.
- `HasLineOfSight` / `HasLineOfFireTo`
- `TryFindLOSOnRing` — searches around the ring for a clear shot point.
- `OrbitPointOnRing` — tangential move around the enemy.
- `TrySetDestinationSmart` / `ForceSetDestination` — apply SetDestination with cooldowns to avoid thrashing.
- `FindBestRetreatHop` — samples candidate retreat positions; scoring favors distance & LOS breaks.

### Targeting & Weapons

- `TargetingSolver`
  - `AimPoint(RobotController enemy)` — collider/renderer center at ~60% height; fallback above pivot.
  - `HasLineOfFire(muzzle, aimPoint, mask)` — straight raycast against obstacles.
  - `IsAimLocked(turret, aimPoint, lockAngle)` — yaw-plane lock.
- `WeaponBase` → `ProjectileWeapon`
  - `TryFireAt(worldPoint)` checks cooldown & range, spawns `Projectile` at `Muzzle`.
  - Cooldown computed from `RobotStats.attackSpeed` (shots/min).
- `Projectile` / `LaserProjectile`
  - Continuous collision via raycast sweep per frame.
  - On hit: `RobotHealth.TakeDamage(damage)`; optional VFX hook.

### Health & Damage

`RobotHealth`
- Pools: `CurrentHealth`, `CurrentArmor`.
- **Armor rule**: absorbs up to half of incoming damage at a 2:1 ratio (intentional design quirk).
- Simple armor regen per state (e.g., more regen in Retreat).
- `OnDeath` event; either `Destroy(gameObject)` or deactivate (arena toggles this).

### Pickups & Spawner

`Pickup`
- Types: `Health`, `Armor`, `DamageBoost`, `SpeedBoost`.
- Boosts are percentage-based (e.g., 25 = +25%), with duration (defaults provided).
- On trigger enter, applies effect and disables itself; raises `PickupsChanged`.

`PickupSpawner`
- Spawns at random points inside a `BoxCollider` area.
- Snaps to NavMesh; requires `minEdgeClearance`.
- Caps active pickups; intervals randomized (initial delay + jitter).

---

## Arena UI & Game Loop

- **ArenaGameLoop**
  - Resets robots to start poses; revives & refills.
  - Countdown → unlock control → fight while >1 alive.
  - On winner: increment score, flash messages, pause before next round.
  - Pause handling via `Time.timeScale` and optional `pausePanel`.
- **ArenaScoreboard / ArenaScoreboardRow**
  - Dynamic rows per robot; tracks and displays win counts.
- **ArenaPlayerPanels**
  - Slot-based UI (2–4) with name + health/armor bars, auto refresh.
- **ArenaCameraController**
  - Smooth follow/orbit; Tab cycles target; A/D orbit input.

---

## Builder UI & Save/Load

- **DropdownManager** — populates part dropdowns from `BodyPartsCatalog`.
- **ColorManager** — palette management (names + colors); applies tints to `RobotAssembler`.
- **StatsPanel** — displays composed stats in real time.
- **SaveLoadManager**
  - Builds `RobotBuildData` from current UI.
  - Saves via `BuildSerializer` to `<persistent>/RobotBuilds` (Editor: repo root).
  - Loads and **opens for editing** (supports overwrite flow).
- **PlayerLoaderUI**
  - Lists saved builds; enforces uniqueness across 2–4 slots.
  - On Continue: `SelectedRobotsStore.Set(chosen)` and load arena scene.

---

## Tuning & Balancing

Where to change things:

- **Perception**: FOV angle (`stats.sightAngle`), detection radius (constant in `Perception`), obstacle/enemy/pickup masks.
- **Movement**: path cooldowns, min repath distances, orbit step, attack-ring cushion (in `CombatNavigator` + states).
- **Transitions**: flip/retarget gates in `StateTransitionHelper`.
- **Weapon**: damage/range/speed come from part definitions and `RobotStats`.
- **Health**: max pools in `FrameDefinition` + core armor bonus; armor regen in `RobotHealth.Update()`.

> For long-term flexibility, I should migrate hardcoded constants into **tuning ScriptableObjects**.

---

## Performance Notes

- Uses `FindObjectsByType` (Unity 2022+) for faster, non-sorted scans.
- Perception caches **visible enemies** and **global pickups/opponents** with polling intervals.
- NavMesh pathing protected by `TrySetDestinationSmart` cooldowns to avoid SetDestination spam.
- Projectile uses **raycast sweep** per frame to avoid tunneling instead of relying solely on physics.

---

## Conventions & Utilities

- **Naming:** sockets/markers are **case-sensitive** (`WeaponMount`, `CoreMount`, `LowerMount`, `Muzzle`, `UpperBody`, `LowerBody`, `*Visual` roots).
- **Colors:** `TintUtility` uses `_BaseColor` then `_Color` (MaterialPropertyBlock).
- **Serialization:** `RobotBuildData` stores both **IDs and indices** for robustness; IDs preferred at load time.
- **Selected Robots:** transient static store via `SelectedRobotsStore`.

