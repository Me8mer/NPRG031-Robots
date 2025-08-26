using TMPro;
using UnityEngine;

/// <summary>
/// Displays current robot stats in the Builder UI.
/// Updates dynamically when part selections change.
/// </summary>
public class StatsPanel : MonoBehaviour
{
    [Header("UI Labels")]
    [SerializeField] private TMP_Text txtHealth;
    [SerializeField] private TMP_Text txtArmor;
    [SerializeField] private TMP_Text txtSpeed;
    [SerializeField] private TMP_Text txtDamage;

    [Header("Data Sources")]
    [SerializeField] private BodyPartsCatalog catalog;
    [SerializeField] private DropdownManager dropdowns;

    /// <summary>
    /// Refreshes displayed stats based on current dropdown selections.
    /// Builds temporary stats from selected part indices.
    /// </summary>
    public void UpdateStats()
    {
        if (!catalog || !dropdowns) return;

        // Compose stats from selected indices
        var stats = RobotStatsBuilder.BuildFromIndices(
            dropdowns.BodyFrameIndex,
            dropdowns.WeaponIndex,
            dropdowns.LowerIndex,
            dropdowns.CoreIndex,
            catalog
        );
        stats.BakeDerived();
        float betterLookingSpeedMod = 100f;
        if (txtHealth) txtHealth.text = $"Health: {stats.maxHealth}";
        if (txtArmor) txtArmor.text = $"Armor: {stats.maxArmor}";
        if (txtSpeed) txtSpeed.text = $"Speed: {stats.bakedMaxMoveSpeed * betterLookingSpeedMod:F1}"; // (wt {stats.weight:F1})";
        if (txtDamage) txtDamage.text = $"Damage: {stats.damage}";
    }
}
