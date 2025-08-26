# BattleRobots — Arena & Builder (Unity C#)

Round-based robot arena skirmishes with a full in-game robot **Builder**. Assemble custom robots from modular parts, save your builds, 
assign them to player slots, and watch them battle it out with a lightweight AI.


---

## Table of Contents

- [Game Overview](#game-overview)
- [Core Features](#core-features)
- [How to Play](#how-to-play)
- [Scenes](#scenes)
- [Controls](#controls)
- [Systems & Architecture](#systems-architecture)
  - [Robot Assembly & Data](#robot-assembly-data)
  - [Stats Composition](#stats-composition)
  - [AI & Movement](#ai-movement)
  - [Targeting & Weapons](#targeting-weapons)
  - [Perception & Pickups](#perception-pickups)
  - [Arena Game Loop & UI](#arena-game-loop-ui)
- [Project Setup](#project-setup)
- [Notes](#notes)

---

## Game Overview

BattleRobots is a small Unity game where you:

1. **Build** robots from modular parts (Frame, Lower body, Weapon, Core) and tint them with palette colors.
2. **Save** your builds and **assign** them to up to 4 player slots.
3. **Fight** in a round-based arena until a single robot remains. Wins accumulate on a scoreboard across rounds.


---

## Core Features

- **Modular Builder:** Combine prefabs & definitions; live preview with tinting.  
- **Save/Load Builds:** JSON saves to a platform-appropriate folder; edit/overwrite.  
- **AI:** Decision layer + FSM (Idle / Chase / Strafe / Retreat) with line-of-sight and attack-ring logic.  
- **Targeting:** Consistent aim points and line-of-fire checks from a centralized solver.  
- **NavMesh Movement:** Chasing, ring strafing, smart retreat hops, path cooldowns.  
- **Pickups:** Health, Armor, Damage Boost, Speed Boost; spawner with cap and intervals.  
- **Arena Loop:** Countdown → fight → winner → repeat; scoreboard + player panels; camera follow/orbit.  
- **Controls:** Pause, switch camera target, orbit; keyboard-friendly for quick demos.  

---

## How to Play

1. **Open the Builder**  
   - From the **Main Menu**, click **Start Game** → **Player Loader** or go to the **Builder** to create/edit builds.  
   - In **Builder**:  
     - Choose parts via dropdowns.  
     - Choose colors via palettes.  
     - Stats panel updates in real time.  
     - **Save** your robot with a name.  

2. **Assign Players**  
   - Open **Player Loader** and select saved robots for 2–4 slots (uniqueness enforced).  
   - Click **Continue** to load the arena.  

3. **Fight**  
   - The **Arena** runs a countdown, unlocks robots, and tracks the round winner.  
   - Scoreboard updates after each round; the loop continues automatically.  

---

## Scenes

- **BuilderScene** — Main menu, Builder UI, Save/Load management, Player Loader.  
- **ArenaPrototypeScene** — Spawns the selected robots and runs the round loop.  

You’ll see references to these scene names in UI scripts (e.g., PlayerLoaderUI and PauseMenu).

---

## Controls

- **Tab** — Cycle camera target (next robot).  
- **A / D** — Orbit camera left/right around the current target.  
- **P or Esc** — Pause/Resume.  

---

## Systems & Architecture

### Robot Assembly & Data

- **Part ScriptableObjects**  
  - `FrameDefinition`, `LowerDefinition`, `WeaponDefinition`, `CoreDefinition`  
  - Pure data: health, armor, speed, turning speed, damage, attack range, attack speed, weight, etc.  
- **Catalog**  
  - `BodyPartsCatalog` holds options (ID + prefab + definition) for each part type.  
  - Provides lookups by **index** and by **ID** (used for Save/Load).  
- **Assembler**  
  - `RobotAssembler` spawns the chosen part prefabs into sockets (`frameSocket`, `lowerSocket`) and mounts (`WeaponMount`, `CoreMount`), applies tints via `TintUtility`.  
  - Exposes assembly from **indices** (Builder) and **IDs** (Arena spawn from saves).  

### Stats Composition

- `RobotStats` stores aggregated values (health, armor, baseSpeed, damage, range, attackSpeed, turningSpeed, sightAngle, weight).  
- `RobotStatsBuilder` composes stats from definitions (frame/lower/weapon/core) and writes them into the controller’s existing `RobotStats` (keeps references synchronized across systems like Health/Perception).  

### AI & Movement

- **Decision Layer**  
  - `DecisionLayer` (abstract) and `PlayerDecisionLayer` decide a `DecisionResult` each frame:  
    - Movement intent: `Idle`, `ChaseEnemy`, `ChasePickup`, `StrafeEnemy`, `Retreat`  
    - Fire target: which enemy to shoot  
  - Basic “stickiness” reduces jitter for aiming/strafe target.  
- **FSM**  
  - States: `IdleState`, `ChaseState`, `StrafeState`, `RetreatState`  
  - `StateMachine` manages transitions via `StateTransitionHelper` based on the `DecisionResult`.  
- **Navigation**  
  - `CombatNavigator` centralizes movement math:  
    - Attack ring computation, line-of-sight/fire checks  
    - Orbiting/strafe ring points  
    - Retreat hop sampling and scoring (distance, LOS breaks)  
    - Smart path cooldowns (prevents SetDestination spam)  
- **Controller**  
  - `RobotController` wires everything, applies stat-driven movement, exposes fire points/aiming helpers, and applies temporary boosts.  

### Targeting & Weapons

- **Targeting**  
  - `TargetingSolver` returns aim points (collider/renderer fallback) and verifies line-of-fire from the muzzle; also exposes aim-lock checks.  
- **Weapons**  
  - `WeaponBase` (abstract) → `ProjectileWeapon` (standard shooter)  
  - `Projectile`/`LaserProjectile` handle travel, continuous collision via raycast sweeps, and apply damage through `RobotHealth`.  
  - Attack speed is **shots per minute** (cooldown handled by weapon).  

### Perception & Pickups

- **Perception**  
  - `Perception` caches **visible enemies** via OverlapSphere (FOV + LOS) and **all opponents/pickups** with interval refresh.  
- **Pickups**  
  - `Pickup` grants Health, Armor, Damage Boost %, or Speed Boost % (with durations).  
  - `PickupSpawner` randomly spawns pickups inside a `BoxCollider` area, snaps to NavMesh, respects edge clearance, caps active count, and randomizes intervals.  

### Arena Game Loop & UI

- **Loop**  
  - `ArenaGameLoop` runs: reset → countdown → unlock → fight → winner → repeat.  
  - Revives robots between rounds and maintains per-robot wins.  
- **Scoreboard & Panels**  
  - `ArenaScoreboard` + `ArenaScoreboardRow` show wins per robot and flash round messages.  
  - `ArenaPlayerPanels` displays each robot’s **name + health + armor** bars live.  
- **Camera**  
  - `ArenaCameraController` follows a chosen robot with smooth orbit and target cycling.  
- **Pause**  
  - `PauseMenu` toggles pause and can return to the builder scene.  
- **Save/Load & Player Loader**  
  - `BuildSerializer` saves/loads **RobotBuildData** (JSON).  
  - `SaveLoadManager` drives the Builder’s save/load UI.  
  - `PlayerLoaderUI` assigns saved builds to 2–4 slots (unique enforcement) and launches the arena.  


## Project Setup

- **Unity:** 2022.2+ recommended  
  (Code uses `FindObjectsByType` for speed; if you target older Unity versions, replace with `FindObjectOfType/FindObjectsOfType` where needed.)  
- **TextMeshPro** required (TMP components used in UI).  
- **NavMesh:** Bake the arena map so robots can move.  

**Open & Run**  
1. Open the project in Unity.  
2. Load **BuilderScene**.  
3. Create & save a couple of robots.  
4. Open **Player Loader**, assign 2–4 unique robots, **Continue**.  
5. Watch the fight in **ArenaPrototypeScene**.  

---

## Notes

- “AI” here means **scripted behavior** (no ML).  
- Code is documented with XML comments throughout for in-IDE help and potential doc generation.  
- You’ll see minor debug logs on state enters (useful for quick testing).  

