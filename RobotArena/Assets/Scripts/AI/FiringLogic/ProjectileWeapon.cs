// Weapons/ProjectileWeapon.cs
using UnityEngine;

public class ProjectileWeapon : WeaponBase, IProjectileWeapon
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private bool debug = true;

    private float _nextFireTime;
    private GameObject _owner;
    private RobotStats _stats;

    private void Awake()
    {
        _owner = gameObject;
        var rc = GetComponentInParent<RobotController>();
        _stats = rc ? rc.GetStats() : null;
    }

    public override float EffectiveRange => _stats != null ? _stats.attackRange : 25f;
    public override bool CanFire => Time.time >= _nextFireTime;

    private void OnValidate()
    {
        if (!muzzle) Debug.LogWarning($"{name}: ProjectileWeapon missing Muzzle transform.");
        if (!projectilePrefab) Debug.LogWarning($"{name}: ProjectileWeapon missing Projectile prefab.");
    }

    public override bool TryFireAt(Vector3 worldPoint)
    {
        if (!CanFire) { if (debug) Debug.Log($"{name}: refuse fire, cooldown"); return false; }
        if (projectilePrefab == null) { if (debug) Debug.Log($"{name}: refuse fire, projectilePrefab null"); return false; }
        if (muzzle == null) { if (debug) Debug.Log($"{name}: refuse fire, muzzle null"); return false; }
        if (_stats == null) { if (debug) Debug.Log($"{name}: refuse fire, stats null"); return false; }


        Vector3 toTarget = worldPoint - muzzle.position;
        float distance = toTarget.magnitude;
        if (distance > _stats.attackRange) return false;

        var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(toTarget.normalized));

        proj.Init(
           _owner,
           _stats.damage,                 // damage from stats
           projectilePrefab.speed,        // speed from prefab
           _stats.attackRange,            // range from stats
           hitMask
       );

        // attackSpeed is shots per minute
        float shotsPerMinute = Mathf.Max(1f, _stats.attackSpeed);
        float cooldown = 60f / shotsPerMinute;
        _nextFireTime = Time.time + cooldown;

        return true;
    }

}
