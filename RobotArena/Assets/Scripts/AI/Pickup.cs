using UnityEngine;

/// <summary>
/// Identifies a collectible bonus item.
/// </summary>
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Armor, DamageBoost, SpeedBoost }

    public PickupType Type;
}
