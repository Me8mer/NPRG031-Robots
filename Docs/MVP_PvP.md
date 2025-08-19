# MVP

## Main game loop
- 2 to 4 player-created robots enter at the start of the match.
- Robots fight until only one remains alive or a timer runs out.
- On death (health reaches 0), a robot is eliminated.

The match ends when one robot remains or all but one have been eliminated. The surviving robot is declared the winner.
## AI overview

Robots use a **Finite State Machine (FSM)** for **movement only** and a separate always-on **firing loop**.

### Movement states
- **Idle**: Stand still and scan. Armor regen 100%. Movement 0.
- **Chase**: Move toward a target. Target can be an enemy or a pickup. Armor regen 50%. Movement 100%.
- **Strafe**: Orbit a specific enemy at weapon ring distance. Armor regen 0%. Movement 50%.
- **Retreat**: Run away from visible enemies and try to break line of sight when health ≤ 30%.

### Decision logic (each frame)
The decision layer outputs two channels:

- **MovementIntent**: one of `Idle`, `ChaseEnemy`, `ChasePickup`, `StrafeEnemy`, `Retreat`.
- **FireIntent**: the best enemy in effective range, or `None`.

**Priority**  
1) If health ≤ threshold → **MovementIntent = Retreat**.  
2) Else if any enemy is in effective range → **MovementIntent = StrafeEnemy** (around that enemy).  
3) Else if any pickup exists on the map → **MovementIntent = ChasePickup** (nearest).  
4) Else if any enemy exists → **MovementIntent = ChaseEnemy** (nearest).  
5) Else → **MovementIntent = Idle**.

**Firing is independent of movement.** If any enemy is in effective range and visible, the robot will aim and shoot every frame, even while chasing a pickup or retreating.

Every robot evaluates surroundings each frame and acts based on:

- Always shoot an enemy that is within effective range and visible.
- If an enemy is in range → **Strafe** around that enemy.
- Else if a pickup exists → **Chase** the pickup.
- Else if an enemy exists → **Chase** the nearest enemy.
- Else **Idle**.

#### Decision Layer contract
- **MovementIntent** controls the FSM transition only.
- **FireIntent** selects a single enemy to shoot at, independent of the current movement state.
- Pickups are discovered at whole‑map scope, enemies can be evaluated both in‑range (for firing) and whole‑map (for chasing).


## Match structure
- Robots spawn at distinct spawn pads in the arena.
- Bonus packs spawn on timers at predefined or random positions, up to a maximum number active.
- No enemies or waves spawn — only player robots are active during a match.
- The last robot alive wins the match.

## Environment
- Arena with indestructible walls.
- Bonus pack spawn points placed around the map.

## Visual and sound
Robots are built from simple modular shapes. Basic visual and sound feedback on hits, movement, and shooting.

### Bonus Packs
Bonus packs spawn periodically on the map:
- **Health Pack**: Restores a portion of health (capped by max).  
- **Armor Pack**: Restores or boosts armor (up to cap).  
- **Damage Boost Pack**: +50% damage for 5 seconds.  
- **Movement Boost Pack**: +50% movement speed for 5 seconds.  


## Building robots UI
Building UI will include drag and drop menu where you build the robot from all available robot parts. MVP includes persistent robot builds using JSON-based Saving and Loading using Unity’s JsonUtility.
When building a robot, players must choose exactly one part from each category (body, weapon, movement, Core) and allocate 5 main stats.

- Drag-and-drop menu to assemble a robot from parts.
- JSON-based saving and loading of builds.
- Exactly one choice from each category: body, weapon, movement, core.
- Allocate 5 points into main stats.

## Main Stats
Players allocate 5 points among:
- **Health**: Max HP. On reaching 0, robot is destroyed. Can be healed by health packs.  
- **Armor**: Shields health. Damage is applied to armor first, regenerates slowly if not taking damage.  
- **Movement Speed**: Affects movement speed.  
- **Damage**: Affects all outgoing damage.  

## Minor Stats
Derived from weapons:
- **Armor Damage** – Deals more vs armor, less vs health.  
- **Health Damage** – Deals more vs health, less vs armor.  
- **Weight** – Heavier robots move slower.  

### a. Main Body Frames
| Type         | Health | Armor | Weight | Notes                  |
|--------------|--------|-------|--------|------------------------|
| **Light**    | 75     | 25    | 50     | Agile, faster cooldowns |
| **Balanced** | 100    | 50    | 75     | All-around balanced     |
| **Heavy**    | 125    | 75    | 100    | Tanky, slow cooldowns   |

### b. Weapons / Arms
| Weapon           | Damage | Damage type | Attack Speed | Weight | Range   | Special Behavior                |
|------------------|--------|-------------|--------------|--------|---------|---------------------------------|
| **Laser Gun**    | 150    | Health      | 3 sec        | 25 kg  | Whole map | Accurate ranged shots          |
| **Missile Pods** | 100    | Armor       | 3 sec        | 50 kg  | 50 m     | Explosive area damage          |
| **Saw Blades**   | 75     | Health      | 1 sec        | 10 kg  | Melee    | Close-range melee damage       |
| **Flamethrower** | 75     | Armor       | Continuous   | 50 kg  | 50 m     | Burns enemies in area, overheats|

### c. Movement Parts
| Legs       | Speed | Special Traits                        |
|------------|-------|---------------------------------------|
| **Wheels** | 100   | Fast linear movement, slow turning    |
| **Tracks** | 50    | Balanced, good on rough terrain       |
| **Legs**   | 75    | Agile, quick directional changes      |

### d. Optional Cores
| Core             | Effect                     | Weight |
|------------------|----------------------------|--------|
| **Energy Core**  | +15% Damage, +25 Movement  | 10 kg  |
| **Defense Core** | +50% Armor regen           | 50 kg  |

## Debug
For MVP debugging:
- Show current **movement state** (Idle/Chase/Strafe/Retreat).
- Show current **MovementIntent** and the chosen target (enemy or pickup).
- Show **FireIntent** target if any (the enemy being shot).
- Show active bonuses and cooldowns.
- Show health and armor bars.