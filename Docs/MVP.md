### MVP

## Main game loop

- 1 level
- player needs to survive exacly 15 waves to finish a level. Only one level available.
- player dies on reaching 0 health
- after each 5 waves. theres a boss fight

After each boss fight, the player may increase one of the following stats by 1:
- Health
- Armor
- Movement Speed
- Damage  
This stat boost lasts for the remainder of the level.


## AI overview
For Players and Enemies AI I will use Finite State Machine (FSM) approach. It will consist of these states:
- Idle: Scanning for enemies or bonus packs. Armor recovery 100%. Movement 0.
- Chase: Close minimal range with the closest enemy. Armor reovery 50%. Movement 100% 
- Attack: Attack an enemy within weapon range. Armor Recovery 0%. Movement 50%. 
- Retreat/Hide: Run away from enemies. Try to hide behind obstacles. Have no line of sight with enemy. (Life <= 30%)

Every robot (both player and enemy) evaluates its surroundings each frame and acts according to the following prioritized logic, which influences its actions within the current FSM state:

- If an enemy is within weapon range → begin attacking.
- Simultaneously, if a bonus pack is visible → move toward the nearest bonus pack (unless the state limits movement).
- If no enemy is in range, and no bonus pack is visible → move toward the closest visible enemy.
- If no enemy or bonus pack is visible → remain idle and scan surroundings.

Note: Robots can attack and move at the same time if the weapon and state allow it. For example, a robot may shoot while moving toward a bonus pack. The FSM determines behavior constraints (e.g. movement speed or armor regen), but this decision layer determines what the robot focuses on.

### Enemy robots
There will be 4 different types of normal robots.
- *Rodent*  a small but agile robot. Small health and small damage. Will use legs for movement and pistols for arms. 
- *Turtle* bigger but slower robot. Bigger health and bigger armor. Will use /roller/ for movement and flamethrower as a weapon.
- *Owl* medium robot. Medium health and armor. Will use wheels for movement and Laser pistol for arms.
- *Panther* medium robot. Agile and fast. Will use Legs for movement and sawblades for arms. Will have double legs.

### Enemy Boss robots
Boss variants of normal enemy robots. At first, just a bigger and stronger variants of normal enemy types.
- *Barn Owl* a first boss of the game. All stats and size doubled.
- *Black Panther* second Boss of the game. All stats and size doubled.
- *Pandora Turtle* last boss of the game. All stats will be 3/4 bigger than normal. Will fight alongside other enemies.

### Environment
The environmnet will consist of undestructible walls, and difficult terrain that slows movement. Map is an Arena eith 4 gates out of which enemies spawn. Spawn place will be random at first.

## Visual and sound
Robots are created from simple shapes. Theres a basic visual and sound feedbeck on hit and shooting.

### First levels progression
- Levels 1-4: Enemies that spawn are *Rodents* and *Turtles* in incerasing numbers each wave.
- level 5: First boss fight with a Barn Owl. After fight a bonus upgrade.
- Levels 6-9: Normal *Owls* enemies now spawn alongside *Rodents* and *Turtles*
- Level 10: Second Boss Fight *Black Panther*. After fight a bonus upgrade.
- Levels 11-14: Normal *Panther* enemies now spawn alongside *Rodents*, *Turtles* and *Owls*.
- Level 15: Last boss fight with *Pandora Turtle*. Turtle fights alongside 2 *Rodents*, 1 *Panther* and 1 *Owl*

### Bonus Packs
Bonus pack have a 15% chance of dropping a random pack from each enemy
3 health packs after a boss fight are guaranteed
- Health Pack: Heals 45 health ()capped by maximum
- Armor Pack: Adds 45 Armor (caps at 4/3 of maximum armor)
- Damage Boost Pack: Increase all damage by 50% for 5 seconds
- Movement Pack: Increase movement by 50% for 5 seconds

## Building robots UI
Building UI will include drag and drop menu where you build the robot from all available robot parts. MVP includes persistent robot builds using JSON-based Saving and Loading using Unity’s JsonUtility.
When building a robot, players must choose exactly one part from each category (body, weapon, movement, Core) and allocate 5 main stats.

## Main Stats
Player allocates 5 points into main stats during robot building.
- Health: Basic health of a Robot. Agter reaching 0 the robot blows up and dies. Can be replenished with health packs. 
- Armor: Second stat that ptoects the robot. It acts as a shield from taking Health Damage. All damage is first apllied to Armor and once theres no armor, the damage is dealt to the Robots health. Armor is being slowly *repaired*, replenished, when not taking damage.
- Movemet speed: Affects the speed at which robot moves.
- Damage: Affects overall damage of the robot.

## Minor Stats
Second stats come from weapons.
- Armor Damage - Type of damage that deals higher amount to armor and less amount to health
- Health Damage - Type of damage that deals higher amount to health and less amount to armor
- Weight: Affects the speed of robot. All equipment of the robot.

### a. Main Body Frames 
The Body Frame strongly influences robot stats like health, armor, and base speed.

| Type          | Health  | Armor   | Weight | Notes                  |
|---------------|---------|---------|-------|------------------------|
| **Light**     | 75      | 25     | 50     | Agile, faster cooldowns |
| **Balanced**  | 100     | 50     | 75      | All-around balanced     |
| **Heavy**     | 125     | 75     | 100      | Tanky, slow cooldowns   |

### b. Weapons / Arms (Hands)
Weapons change how the robot attacks.

| Weapon          | Damage  | Damage type  | Attack Speed | Weight | Range    | Special Behavior           |
|-----------------|---------|--------------|--------------|--------|---------|----------------------------|
| **Laser Gun**   | 150     | Health       | 3 sec        | 25 kg  | Whole map| Accurate ranged shots      |
| **Missile Pods**| 100     | Armor        | 3 sec        | 50 kg  | 50 m     | Explosive area damage      |
| **Saw Blades**  | 75      | Health       | 1 sec        | 10 kg  | Melee    | Close-range melee damage   |
| **Flamethrower**| 75      | Armor        | Continuous (3sec)| 50 kg | 50 m  | Burns enemies in an area. Overheats|

### c. Movement Parts (Legs)
Affects robot speed, maneuverability, and special movement behaviors.

| Legs           | Speed   | Special Traits                       |
|----------------|---------|--------------------------------------|
| **Wheels**     | 100     | Fast linear movement, slow turning   |
| **Tracks**     | 50      | Balanced, handles rough terrain      |
| **Legs**       | 75      | Agile, quick directional changes     |

### d. Optional Cores 
Robot cores offer subtle tactical modifications.

| Core              | Effect                    | Weight      | 
|-------------------|---------------------------|-------------|
| **Energy Core**   | Damage +15% Movement +25  | 10 kg       | 
| **Defense Core**  | Armor repair +50%         | 50 kg       |

### Enemy robots

| Robot  | Robot effect      | Weight | Hands       | Damage (type)| Movement/Legs | Health | Armor |
|--------|-------------------|--------|-------------|--------------|---------------|--------|-------|
| Rodent |Tiny. Small weight | 50     | Pistol      | 10 (health)  | 100           | 15     | 15    |
| Turtle | Strong Body       | 150    | Flamethrower| 15 (Armor)   | 75            | 50     | 50    |
| Panther| Balanced          | 100    | Sawblades   | 15 (health)  | 100           | 15     | 30    |
| Owl    | Sniper            | 100    | Laser Pistol| 20 (Health)  | 75            | 30     | 10    |

## Debug
For MVP, a basic debug panel will show:
- Current FSM state of player and enemies
- Active bonuses and cooldowns
- Health and armor bars