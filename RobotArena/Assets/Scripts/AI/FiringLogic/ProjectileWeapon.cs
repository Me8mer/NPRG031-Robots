// Weapons/ProjectileWeapon.cs
using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float maxRange = 25f;
    [SerializeField] private float spreadDegrees = 1.5f;
    [SerializeField] private LayerMask hitMask;

    private float _nextFireTime;
    private GameObject _owner;

    private void Awake()
    {
        _owner = gameObject;
    }

    public override float EffectiveRange => maxRange;
    public override bool CanFire => Time.time >= _nextFireTime;

    public override bool TryFireAt(Vector3 worldPoint)
    {
        if (!CanFire || projectilePrefab == null || muzzle == null) return false;

        Vector3 toTarget = worldPoint - muzzle.position;
        float distance = toTarget.magnitude;
        if (distance > maxRange) return false;

        Vector3 dir = ApplySpread(toTarget.normalized, spreadDegrees);
        var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
        proj.Init(_owner, damage, projectileSpeed, maxRange, hitMask);

        _nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        return true;
    }

    private static Vector3 ApplySpread(Vector3 dir, float degrees)
    {
        if (degrees <= 0f) return dir;
        Quaternion q = Quaternion.Euler(Random.Range(-degrees, degrees), Random.Range(-degrees, degrees), 0f);
        return q * dir;
    }
}
