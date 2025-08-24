using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtHealth;
    [SerializeField] private TMP_Text txtArmor;
    [SerializeField] private TMP_Text txtSpeed;
    [SerializeField] private TMP_Text txtDamage;

    [Header("Data Source")]
    [SerializeField] private BodyPartsCatalog catalog;
    [SerializeField] private DropdownManager dropdowns;

    public void UpdateStats()
    {
        if (!catalog) return;

        // Build stats from current indices
        var stats = RobotStatsBuilder.BuildFromIndices(
            dropdowns.BodyFrameIndex,
            dropdowns.WeaponIndex,
            dropdowns.LowerIndex,
            dropdowns.CoreIndex,
            catalog
        );

        if (txtHealth) txtHealth.text = $"Health: {stats.maxHealth}";
        if (txtArmor) txtArmor.text = $"Armor: {stats.maxArmor}";
        if (txtSpeed) txtSpeed.text = $"Speed: {stats.weight:F1}";
        if (txtDamage) txtDamage.text = $"Damage: {stats.damage}";
    }
}
