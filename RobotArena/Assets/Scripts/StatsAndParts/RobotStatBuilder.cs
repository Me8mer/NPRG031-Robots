using UnityEngine;

/// <summary>
/// Utility class for constructing <see cref="RobotStats"/> from part definitions.
/// 
/// Supports both: 
/// - Building from part IDs (for loading saved robots / arena spawning)
/// - Building from indices (for live Builder UI selections)
/// </summary>
public static class RobotStatsBuilder
{
    // MVP default perception values (could later move into Frame/Core definitions)
    private const float DefaultSightAngle = 120f;

    /// <summary>
    /// Builds a <see cref="RobotStats"/> object from part IDs (as stored in <see cref="RobotBuildData"/>).
    /// Used when loading robots into the arena.
    /// </summary>
    public static RobotStats BuildFromIds(RobotBuildData data, BodyPartsCatalog catalog)
    {
        if (!catalog)
        {
            Debug.LogError("RobotStatsBuilder.BuildFromIds: catalog is null.");
            return new RobotStats();
        }

        var frame = catalog.GetFrameDefById(data.frameId);
        var weapon = catalog.GetWeaponDefById(data.weaponId);
        var lower = catalog.GetLowerDefById(data.lowerId);
        var core = catalog.GetCoreDefById(data.coreId);

        return Compose(frame, weapon, lower, core);
    }

    /// <summary>
    /// Builds a <see cref="RobotStats"/> object from catalog indices.
    /// Used by the Builder UI during selection.
    /// </summary>
    public static RobotStats BuildFromIndices(int iFrame, int iWeapon, int iLower, int iCore, BodyPartsCatalog catalog)
    {
        if (catalog == null)
        {
            Debug.LogError("RobotStatsBuilder.BuildFromIndices: catalog is null.");
            return new RobotStats();
        }

        var frame = catalog.GetFrameDef(iFrame);
        var weapon = catalog.GetWeaponDef(iWeapon);
        var lower = catalog.GetLowerDef(iLower);
        var core = catalog.GetCoreDef(iCore);

        return Compose(frame, weapon, lower, core);
    }

    /// <summary>
    /// Fills an existing <see cref="RobotStats"/> object with values built from part IDs.
    /// This preserves shared references (e.g. Perception and Health still point to the same stats).
    /// </summary>
    public static void FillFromIds(RobotStats dst, RobotBuildData data, BodyPartsCatalog catalog)
    {
        if (dst == null) return;
        var built = BuildFromIds(data, catalog);
        dst.CopyFrom(built);
    }

    /// <summary>
    /// Fills an existing <see cref="RobotStats"/> object with values built from indices.
    /// </summary>
    public static void FillFromIndices(RobotStats dst, int iFrame, int iWeapon, int iLower, int iCore, BodyPartsCatalog catalog)
    {
        if (dst == null) return;
        var built = BuildFromIndices(iFrame, iWeapon, iLower, iCore, catalog);
        dst.CopyFrom(built);
    }

    /// <summary>
    /// Core composition logic that merges stats from all definitions.
    /// Called by both BuildFromIds and BuildFromIndices.
    /// </summary>
    private static RobotStats Compose(FrameDefinition frame, WeaponDefinition weapon, LowerDefinition lower, CoreDefinition core)
    {
        var stats = new RobotStats();

        // ---- FRAME ----
        // Defines base durability and weight
        stats.maxHealth = frame ? frame.baseHealth : 0f;
        stats.maxArmor = frame ? frame.baseArmor : 0f;
        float totalWeight = frame ? frame.baseWeight : 0f;

        // ---- WEAPON ----
        // Defines offense capability and adds weight
        if (weapon)
        {
            stats.damage = weapon.attackDamage;
            stats.attackRange = weapon.attackRange;
            stats.attackSpeed = weapon.attackSpeed;   // shots per minute
            totalWeight += weapon.baseWeight;
        }

        // ---- LOWER BODY ----
        // Defines movement speed and turning speed, adds weight
        if (lower)
        {
            stats.baseSpeed = lower.baseSpeed;
            totalWeight += lower.baseWeight;
            stats.turningSpeed = lower.turningSpeed;
        }

        // ---- CORE ----
        // Provides modifiers to armor and attack speed (expandable for more core bonuses later)
        if (core)
        {
            stats.maxArmor += core.armor;
            stats.attackSpeed += core.attackSpeed;
        }

        // ---- PERCEPTION ----
        stats.sightAngle = DefaultSightAngle;

        // ---- FINALIZE ----
        // Weight affects speed calculation elsewhere (in RobotController)
        stats.weight = Mathf.Max(0f, totalWeight);
        stats.BakeDerived();
        return stats;
    }
}
