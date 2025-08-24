using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Armor, DamageBoost, SpeedBoost }

    [Header("Generic Pickup Settings")]
    [Tooltip("If true, the pickup will choose a random effect on consume.")]
    public bool randomizeEffect = true;

    [Tooltip("For Health or Armor: absolute points.\nFor boosts: percent value, e.g. 25 = +25%.")]
    public float Value = 25f;

    [Tooltip("Duration for temporary boosts. If 0 or less, a sensible default is used.")]
    public float Duration = 12f;

    public static event Action PickupsChanged;

    private bool _consumed;

    public void Consume(RobotController collector)
    {
        if (_consumed || collector == null) return;
        _consumed = true;

        // Pick a random effect at pickup-time
        var chosen = GetChosenType();
        ApplyEffect(chosen, collector);

        // Deactivate and notify perception caches
        gameObject.SetActive(false);
        PickupsChanged?.Invoke();
        Debug.Log($"{collector.name} collected {chosen} pickup!");
    }

    private PickupType GetChosenType()
    {
        if (!randomizeEffect) return PickupType.Health; // fallback if you ever want a fixed effect
        var types = (PickupType[])Enum.GetValues(typeof(PickupType));
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    private void ApplyEffect(PickupType type, RobotController c)
    {
        var health = c.GetHealth();
        switch (type)
        {
            case PickupType.Health:
                if (health != null) health.Heal(Value);
                break;

            case PickupType.Armor:
                if (health != null) health.RestoreArmor(Value);
                break;

            case PickupType.DamageBoost:
                c.ApplyTimedDamageBoost(Value, Duration > 0f ? Duration : 6f);
                break;

            case PickupType.SpeedBoost:
                c.ApplyTimedSpeedBoost(Value, Duration > 0f ? Duration : 6f);
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
