# Specification for NPRG031-RobotArena

## **1. Introduction**
This project is a Unity-based C# game developed for my programming 2 course. The game features two primary modes:  
- The main game loop is a PVE wave based AI automated battle arena.
- The second mode is a GUI for building and creating robots.

## **2. Gameplay Overview**
The main gameplay centers around an arena where a player built robot battles waves of enemy robots controlled by AI. Both player and enemy robots operate using programmed behaviors (AI). <!-- possible future control mechanics can be added.--> Mechanics could include some easy options from choosing stances: Aggresive, defensive etc, to using abilities.  There will be waves and after each wave <!--or after each x waves, depending on balance -->, there will be some choices of upgrades available. <!-- Maybe add leveling systems.--> Enemy robots progressively increase in strength and numbers each wave. <!--  with potential boss battles at specified intervals -->

### a. Players Robot in the main game loop

The player's robot is autonomously controlled using pre defined AI logic. Actions include: 
- Movement around the arena
- Shooting and targeting enemies
- Collecting bonus packs
- Activating abilities based on robot configuration.
Performance of these actions is determined by robot attributes, customizable in the Robot Builder Mode.

### b, Enemies Robots
Enemy robots are entirely AI-driven, with diverse types varying in attributes such as health, speed, attack strength, and size. Special boss robots may appear during specific waves, providing unique challenges and rewards.

### c, Bonus Packs 
Bonus packs are special items appearing randomly after defeating enemy robots or completing waves. Types of bonus packs include: <!--possibly others? -->

- **Health pack:** Restores a certain amount of health to the robot.  
- **Armor pack:** Grants additional armor points.
- **Damage boost:** Temporarily increases robot damage by a percentage for a limited time.
- **Movement boost:** Temporarily increases robot movement speed by a percentage for a limited time.

## 3. **Robot Building**
Robot building is the second main part of the game. It consists of a Robot building UI. The main functions will be creating new robots, saving robots and loading and editing existing robots. 


## Core Concept
The robot customization is centered around a clear and intuitive modular system. Players combine **Body Frames**, **Weapons (Hands)**, **Movement Parts (Legs)**, and optionally **Robot Cores**. Each part type contributes uniquely, providing choices for players.


### a. Main Body Frames (2-3 types)
The Body Frame strongly influences robot stats like health, armor, and base speed.

| Type          | Health   | Armor   | Speed    | Notes                  |
|---------------|----------|---------|----------|------------------------|
| **Light**     | Low      | Low     | High     | Agile, faster cooldowns |
| **Balanced**  | Medium   | Medium  | Medium   | All-around balanced     |
| **Heavy**     | High     | High    | Low      | Tanky, slow cooldowns    |

### b. Weapons / Arms (Hands)
Weapons change how the robot attacks.

| Weapon          | Damage  | Attack Speed | Range    | Special Behavior           |
|-----------------|---------|--------------|----------|----------------------------|
| **Laser Gun**   | Medium  | High         | Long     | Accurate ranged shots      |
| **Missile Pods**| High    | Low          | Medium   | Explosive area damage      |
| **Saw Blades**  | High    | Medium       | Short    | Close-range melee damage   |
| **Flamethrower** | Low    | High          | Medium   | Burns enemies for additional damage |

### c. Movement Parts (Legs)
Affects robot speed, maneuverability, and special movement behaviors.

| Legs           | Speed    | Agility   | Special Traits                       |
|----------------|----------|-----------|--------------------------------------|
| **Wheels**     | High     | Medium    | Fast linear movement, slow turning   |
| **Tracks**     | Medium   | Low       | Balanced, handles rough terrain      |
| **Legs**       | Medium   | High      | Agile, quick directional changes     |

### d. Optional Cores 
Robot cores offer subtle tactical modifications.

| Core              | Effect                                          | 
|-------------------|-------------------------------------------------|
| **Energy Core**   | Faster ability recharge, reduced armor          | 
| **Defense Core**  | Higher armor, slightly reduced speed            |

## **4. Future Considerations**
Possible expansions and enhancements for future development:
- Additional arena maps with unique environmental challenges.
- Limited player controls (direct ability triggers, target prioritization).
- Boss battle waves with unique mechanics.
- Leveling and progression systems for robots
- More diverse robot components for customization
<!--Possible future updates. More mapsa. Player controls. Boss battles. Level ups. Ideas? -->

# Notes
Note on AI:  
In this project, "AI" refers to scripted logic that governs robot behavior, such as decision making and state transitions. It does not involve real machine learning or adaptive algorithms.

