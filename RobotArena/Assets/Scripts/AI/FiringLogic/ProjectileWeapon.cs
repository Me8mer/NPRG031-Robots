using UnityEngine;

/// <summary>
/// Standard weapon that fires projectile prefabs from a muzzle.
/// Cooldown and damage are based on <see cref="RobotStats"/>.
/// </summary>
public class ProjectileWeapon : WeaponBase, IProjectileWeapon
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Tuning")]
    [Tooltip("Mask of layers projectiles should collide with.")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private bool debug = true;

    private float _nextFireTime;
    private GameObject _owner;
    private RobotStats _stats;
    private RobotController _rController;

    private void Awake()
    {
        _owner = gameObject;
        _rController = GetComponentInParent<RobotController>();
        _stats = _rController ? _rController.GetStats() : null;
    }

    public override float EffectiveRange => _stats != null ? _stats.attackRange : 25f;
    public override bool CanFire => Time.time >= _nextFireTime;

    private void OnValidate()
    {
        if (!muzzle) Debug.LogWarning($"{name}: ProjectileWeapon missing muzzle transform.");
        if (!projectilePrefab) Debug.LogWarning($"{name}: ProjectileWeapon missing projectile prefab.");
    }

    /// <summary>
    /// Fires a projectile prefab towards the given world point if cooldown is ready.
    /// </summary>
    public override bool TryFireAt(Vector3 worldPoint)
    {
        if (!CanFire) { if (debug) Debug.Log($"{name}: fire blocked (cooldown)"); return false; }
        if (projectilePrefab == null) { if (debug) Debug.Log($"{name}: fire blocked (no prefab)"); return false; }
        if (muzzle == null) { if (debug) Debug.Log($"{name}: fire blocked (no muzzle)"); return false; }
        if (_stats == null) { if (debug) Debug.Log($"{name}: fire blocked (no stats)"); return false; }

        Vector3 toTarget = worldPoint - muzzle.position;
        float distance = toTarget.magnitude;
        if (distance > _stats.attackRange) return false;
        float dmg = _rController != null ? _rController.GetEffectiveDamage() : _stats.damage;

        var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(toTarget.normalized));
        proj.Init(
            _owner,
            dmg,             // damage from stats
            projectilePrefab.speed,    // projectile prefab speed
            _stats.attackRange,        // max travel distance
            hitMask
        );

        // attackSpeed is shots per minute
        float shotsPerMinute = Mathf.Max(1f, _stats.attackSpeed);
        float cooldown = 60f / shotsPerMinute;
        _nextFireTime = Time.time + cooldown;

        return true;
    }
}
