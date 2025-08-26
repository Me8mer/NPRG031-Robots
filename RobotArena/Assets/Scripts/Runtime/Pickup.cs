using UnityEngine;
using System;

/// <summary>
/// Collectible pickup that grants health, armor, or temporary boosts.
/// Can optionally randomize effect on pickup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Armor, DamageBoost, SpeedBoost }

    [Header("Settings")]
    [Tooltip("If true, the pickup will choose a random effect on consume.")]
    public bool randomizeEffect = true;

    [Tooltip("For Health/Armor: absolute points. For boosts: percentage (e.g., 25 = +25%).")]
    public float Value = 50f;

    [Tooltip("Duration for temporary boosts. If <= 0, a sensible default is used.")]
    public float Duration = 5f;

    public static event Action PickupsChanged;

    private bool _consumed;

    /// <summary>Called when a robot collects this pickup.</summary>
    public void Consume(RobotController collector)
    {
        if (_consumed || collector == null) return;
        _consumed = true;

        var chosen = GetChosenType();
        ApplyEffect(chosen, collector);

        gameObject.SetActive(false);
        PickupsChanged?.Invoke();

        Debug.Log($"{collector.name} collected {chosen} pickup!");
    }

    private PickupType GetChosenType()
    {
        if (!randomizeEffect) return PickupType.Health;
        var types = (PickupType[])Enum.GetValues(typeof(PickupType));
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    private void ApplyEffect(PickupType type, RobotController c)
    {
        var health = c.GetHealth();
        switch (type)
        {
            case PickupType.Health:
                health?.Heal(Value*2);
                break;
            case PickupType.Armor:
                health?.RestoreArmor(Value*2);
                break;
            case PickupType.DamageBoost:
                c.ApplyTimedDamageBoost(Value, Duration);
                break;
            case PickupType.SpeedBoost:
                c.ApplyTimedSpeedBoost(Value, Duration);
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_consumed) return;
        var robot = other.GetComponentInParent<RobotController>();
        if (robot != null) Consume(robot);
    }
}
