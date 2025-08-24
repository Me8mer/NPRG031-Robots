using UnityEngine;

public static class RobotStatsBuilder
{
    // Sensible MVP defaults. Move these into definitions later if you want full design control.
    private const float DefaultSightAngle = 120f;


    /// Build from saved IDs (use for Load and arena spawning).
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

    /// Build from current selection indices (use in the Builder UI).
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

    public static void FillFromIds(RobotStats dst, RobotBuildData data, BodyPartsCatalog catalog)
    {
        if (dst == null) return;
        var built = BuildFromIds(data, catalog);
        dst.CopyFrom(built);
    }

    public static void FillFromIndices(
        RobotStats dst, int iFrame, int iWeapon, int iLower, int iCore, BodyPartsCatalog catalog)
    {
        if (dst == null) return;
        var built = BuildFromIndices(iFrame, iWeapon, iLower, iCore, catalog);
        dst.CopyFrom(built);
    }

    private static RobotStats Compose(FrameDefinition frame, WeaponDefinition weapon, LowerDefinition lower, CoreDefinition core)
    {
        var stats = new RobotStats();

        // Frame
        stats.maxHealth = frame ? frame.baseHealth : 0f;        // see your Compose comments
        stats.maxArmor = frame ? frame.baseArmor : 0f;
        float totalWeight = frame ? frame.baseWeight : 0f;

        // Weapon
        if (weapon)
        {
            stats.damage = weapon.attackDamage;
            stats.attackRange = weapon.attackRange;
            stats.attackSpeed = weapon.attackSpeed;   // shots per second
            totalWeight += weapon.baseWeight;

 
        }

        // Lower
        if (lower)
        {
            stats.baseSpeed = lower.baseSpeed;
            totalWeight += lower.baseWeight;
            stats.turningSpeed = lower.turningSpeed; 
        }
        // Core modifiers
        if (core)
        {
            stats.maxArmor += core.armor;
            stats.attackSpeed += core.attackSpeed;
            // add more when you extend CoreDefinition
        }

        // Perception
        stats.sightAngle = DefaultSightAngle;    

        // Finalize
        stats.weight = Mathf.Max(0f, totalWeight);
        return stats;
    }
}
