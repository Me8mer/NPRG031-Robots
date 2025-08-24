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
    /// --- Debug Inspector Mirrors ---
    [SerializeField] private float debugHealth;
    [SerializeField] private float debugArmor;
    private bool destroyOnDeath = true;
    public event Action OnDeath;

    #region Unity Lifecycle
    void Awake()
    {
        _controller = GetComponent<RobotController>();
        _stats = _controller.GetStats();
    }

    void Update()
    {
        float regenPerSec;

        switch (_controller.CurrentState)
        {
            case RobotState.Strafe:
                regenPerSec = 10f;
                break;
            case RobotState.Chase:
                regenPerSec = 10F; //_stats.armorRegenChase;
                break;
            case RobotState.Retreat:
                regenPerSec = 20F;//_stats.armorRegenChase; 
                break;
            default:
                regenPerSec = 20F;//_stats.armorRegenIdle;
                break;
        }

        RegenerateArmor(regenPerSec * Time.deltaTime);
        // Keep Inspector mirrors up to date
        debugHealth = CurrentHealth;
        debugArmor = CurrentArmor;
    }
    #endregion


    #region Public API
    /// <summary>
    /// Applies <paramref name="amount"/> raw damage. Armor is depleted first;
    /// any remainder is subtracted from health.
    /// </summary>
    /// <param name="amount">Non‑negative damage value.</param>
    ///
    public void SetDestroyOnDeath(bool value) => destroyOnDeath = value;
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        float toArmor = Mathf.Min(CurrentArmor, amount / 2);
        float toHealth = amount - (toArmor*2);

        CurrentArmor -= toArmor;
        CurrentHealth -= toHealth;

        if (CurrentHealth <= 0f)
            Die();
    }

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
        if (destroyOnDeath)
        {
            Destroy(gameObject);
            return;
        }
        // Soft-death for arena rounds
        gameObject.SetActive(false);
    }

    public void Revive()
    {
        // Refill from current _stats and re-enable
        ApplyStats(_stats);
        gameObject.SetActive(true);
    }
    /// <summary>Heals health and armor to their maximum values.</summary>
    public void RefillToMax()
    {
        CurrentHealth = _stats.maxHealth;
        CurrentArmor = _stats.maxArmor;
        debugHealth = CurrentHealth;
        debugArmor = CurrentArmor;
    }

    #endregion
}
