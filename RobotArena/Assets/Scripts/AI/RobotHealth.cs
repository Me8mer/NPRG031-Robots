using System;
using UnityEngine;

/// <summary>
/// Tracks health and armor pools, performs regeneration based on the
/// current <see cref="RobotState"/>, and raises a death event when
/// health reaches zero.
/// </summary>
[RequireComponent(typeof(RobotController))]
public class RobotHealth : MonoBehaviour
{
    private RobotController _controller;
    private RobotStats _stats;
    /// <summary>Current hit points after armor is gone.</summary>
    public float CurrentHealth { get; private set; }
    /// <summary>Current armor points that will absorb damage first.</summary>
    public float CurrentArmor { get; private set; }
    /// <summary>Invoked once, immediately before the GameObject is destroyed.</summary>
    public event Action OnDeath;

    #region Unity Lifecycle
    void Awake()
    {
        _controller = GetComponent<RobotController>();
        _stats = _controller.GetStats();
        CurrentHealth = _stats.maxHealth;
        CurrentArmor = _stats.maxArmor;
    }

    void Update()
    {
        float regenRatePerSec = _controller.CurrentState == RobotState.Chase
                      ? _stats.armorRegenChase
                      : _stats.armorRegenIdle;

        RegenerateArmor(regenRatePerSec * Time.deltaTime);

    }
    #endregion


    #region Public API
    /// <summary>
    /// Applies <paramref name="amount"/> raw damage. Armor is depleted first;
    /// any remainder is subtracted from health.
    /// </summary>
    /// <param name="amount">Non‑negative damage value.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        float toArmor = Mathf.Min(CurrentArmor, amount);
        float toHealth = amount - toArmor;

        CurrentArmor -= toArmor;
        CurrentHealth -= toHealth;

        if (CurrentHealth <= 0f)
            Die();
    }
    #endregion


    #region Private Helpers
    /// <summary>Heals armor by <paramref name="amount"/>, clamped to <c>maxArmor</c>.</summary>
    private void RegenerateArmor(float amount)
    {
        CurrentArmor = Mathf.Min(_stats.maxArmor, CurrentArmor + amount);
    }
    /// <summary>Destroys the robot and notifies listeners.</summary>
    private void Die()
    {
        OnDeath?.Invoke();
        // TODO: play explosion, disable components, destroy GameObject
        Destroy(gameObject);
    }

    public void SetArmorRegen(float ratePerSecond)
    {
        if (_controller.CurrentState == RobotState.Idle)
            _stats.armorRegenIdle = ratePerSecond;
        else if (_controller.CurrentState == RobotState.Chase)
            _stats.armorRegenChase = ratePerSecond;
    }
    #endregion
}
