# MVP

## Main game loop
- 2 to 4 player-created robots enter at the start of the match.
- Robots fight until only one remains alive or a timer runs out.
- On death (health reaches 0), a robot is eliminated.

The match ends when one robot remains or all but one have been eliminated. The surviving robot is declared the winner.

## AI overview
Robots (all players’ robots) use a **Finite State Machine (FSM)** with these states:
- **Idle**: Scanning for opponents or bonus packs. Armor recovery 100%. Movement 0.  
- **Chase**: Move toward the closest opponent (or pickup if visible). Armor recovery 50%. Movement 100%.  
- **Attack**: Attack an opponent within weapon range. Armor recovery 0%. Movement 50%.  
- **Retreat/Hide**: Run away from opponents when health ≤ 30%. Tries to break line of sight.  

Every robot evaluates surroundings each frame and acts based on the following prioritized logic:
- If an opponent is in weapon range → attack.
- At the same time, if a bonus pack is visible → move toward the nearest bonus pack (unless state restricts movement).
- If no opponent is in range but at least one is visible → chase the closest opponent.
- If nothing is visible → idle and scan surroundings.

Robots may attack and move at the same time, depending on weapon and state rules. The FSM determines speed and armor regeneration. The decision layer decides whether to focus on an opponent or a pickup.

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
- Show FSM state of each robot.
- Show active bonuses and cooldowns.
- Show health and armor bars.  
