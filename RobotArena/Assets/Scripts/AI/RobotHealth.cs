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

    [Header("Debug Inspector Mirrors")]
    [SerializeField] private float debugHealth;
    [SerializeField] private float debugArmor;

    private bool destroyOnDeath = true;

    /// <summary>Raised immediately before the robot is destroyed or disabled.</summary>
    public event Action OnDeath;

    #region Unity Lifecycle
    private void Awake()
    {
        _controller = GetComponent<RobotController>();
        _stats = _controller.GetStats();
    }

    private void Update()
    {
        float regenPerSec = _controller.CurrentState switch
        {
            RobotState.Strafe => 10f,
            RobotState.Chase => 10f,
            RobotState.Retreat => 20f,
            _ => 20f
        };

        RegenerateArmor(regenPerSec * Time.deltaTime);

        // Debug inspector mirrors
        debugHealth = CurrentHealth;
        debugArmor = CurrentArmor;
    }
    #endregion

    #region Public API
    /// <summary>
    /// Controls whether the robot is destroyed or just deactivated on death.
    /// </summary>
    public void SetDestroyOnDeath(bool value) => destroyOnDeath = value;

    /// <summary>
    /// Applies raw damage. Armor absorbs part first, remainder goes to health.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        // Armor absorbs half of incoming damage at 2:1 ratio
        float toArmor = Mathf.Min(CurrentArmor, amount / 2f);
        float toHealth = amount - (toArmor * 2f);

        CurrentArmor -= toArmor;
        CurrentHealth -= toHealth;

        if (CurrentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// Sets stats and refills health and armor to max values.
    /// </summary>
    public void ApplyStats(RobotStats stats)
    {
        _stats = stats ?? new RobotStats();
        CurrentHealth = _stats.maxHealth;
        CurrentArmor = _stats.maxArmor;
        debugHealth = CurrentHealth;
        debugArmor = CurrentArmor;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        CurrentHealth = Mathf.Min(_stats.maxHealth, CurrentHealth + amount);
    }

    public void RestoreArmor(float amount)
    {
        if (amount <= 0f) return;
        CurrentArmor = Mathf.Min(_stats.maxArmor, CurrentArmor + amount);
    }
    #endregion

    [ContextMenu("Apply Debug Values")]
    private void ApplyDebugValues()
    {
        CurrentHealth = Mathf.Clamp(debugHealth, 0f, _stats.maxHealth);
        CurrentArmor = Mathf.Clamp(debugArmor, 0f, _stats.maxArmor);
    }

    #region Private Helpers
    private void RegenerateArmor(float amount)
    {
        CurrentArmor = Mathf.Min(_stats.maxArmor, CurrentArmor + amount);
    }

    private void Die()
    {
        OnDeath?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>Revives robot with full stats and re-enables GameObject.</summary>
    public void Revive()
    {
        ApplyStats(_stats);
        gameObject.SetActive(true);
    }

    /// <summary>Fully refills both health and armor.</summary>
    public void RefillToMax()
    {
        CurrentHealth = _stats.maxHealth;
        CurrentArmor = _stats.maxArmor;
        debugHealth = CurrentHealth;
        debugArmor = CurrentArmor;
    }
    #endregion
}
