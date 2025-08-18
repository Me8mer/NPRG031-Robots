using UnityEngine;

/// <summary>
/// Identifies a collectible bonus pack that robots can seek and use.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    public enum PickupType { Health, Armor, DamageBoost, SpeedBoost }

    [Header("Pickup Settings")]
    public PickupType Type;

    [Tooltip("How much value this pickup provides (HP restored, armor restored, or % boost).")]
    public float Value = 25f;

    [Tooltip("Duration in seconds for temporary boosts. 0 means instant effect.")]
    public float Duration = 0f;

    private bool _consumed = false;

    /// <summary>
    /// Called when a robot collects this pickup.
    /// </summary>
    public void Consume(RobotController collector)
    {
        if (_consumed) return;
        _consumed = true;

        // TODO: Apply effect here later
        Debug.Log($"{collector.name} collected {Type} pickup!");

        // Deactivate or destroy
        gameObject.SetActive(false);
        // Or Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Pickup {name} triggered by {other.name}");

        if (_consumed) return;

        var robot = other.GetComponentInParent<RobotController>();
        if (robot != null)
        {
            Consume(robot);
        }
    }
}
