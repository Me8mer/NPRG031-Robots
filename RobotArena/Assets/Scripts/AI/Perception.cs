using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles spatial awareness. Detects enemies and pickups using
/// sphere overlaps, field-of-view cone checks, and optional line-of-sight raycasts.
/// </summary>
[RequireComponent(typeof(RobotController))]
public class Perception : MonoBehaviour
{
    private RobotController controller;
    private RobotStats stats;

    private const float DetectionRadius = 60f;

    [Tooltip("How often (seconds) to refresh cached queries")]
    public float checkInterval = 0.2f;

    private float lastEnemyCheckTime;
    private float _lastAllOppCheckTime;
    private float _lastAllPickupsCheckTime;

    private List<Pickup> _cachedAllPickups = new();
    private List<RobotController> _cachedAllOpponents = new();
    private List<RobotController> cachedVisibleEnemies = new();

    [Tooltip("Obstacle layers that block vision")]
    public LayerMask obstacleMask;

    [Tooltip("How often to refresh the global list of opponents")]
    public float allOpponentsCheckInterval = 1.0f;

    [Tooltip("How often to refresh the global list of pickups")]
    public float allPickupsCheckInterval = 1.0f;

    [Header("Detection Layers")]
    [Tooltip("Layer mask for robots")]
    public LayerMask enemyMask;
    [Tooltip("Layer mask for pickups")]
    public LayerMask pickupMask;

    private void Awake()
    {
        controller = GetComponent<RobotController>();
        stats = controller.GetStats();
    }

    // ---------------- PICKUPS ----------------

    /// <summary>
    /// Returns cached list of all pickups. Refreshes only once per <see cref="allPickupsCheckInterval"/>.
    /// </summary>
    public List<Pickup> GetAllPickups()
    {
        if (Time.time - _lastAllPickupsCheckTime < allPickupsCheckInterval)
            return _cachedAllPickups;

        _lastAllPickupsCheckTime = Time.time;
        _cachedAllPickups = ScanAllPickups();
        return _cachedAllPickups;
    }

    /// <summary>
    /// Updates the internal <see cref="RobotStats"/> reference.
    /// Call this when stats are injected or modified.
    /// </summary>
    public void ApplyStats(RobotStats s) => stats = s;

    private List<Pickup> ScanAllPickups()
    {
        var list = new List<Pickup>();
        var all = UnityEngine.Object.FindObjectsByType<Pickup>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null) continue;
            if (!p.isActiveAndEnabled) continue;
            if (!p.gameObject.activeInHierarchy) continue;
            list.Add(p);
        }
        return list;
    }

    // ---------------- OPPONENTS ----------------

    /// <summary>
    /// Returns cached list of visible enemies, updated only once per <see cref="checkInterval"/>.
    /// </summary>
    public List<RobotController> GetEnemiesInRange()
    {
        if (Time.time - lastEnemyCheckTime < checkInterval)
            return cachedVisibleEnemies;

        lastEnemyCheckTime = Time.time;
        cachedVisibleEnemies = ScanForEnemiesInRange();
        return cachedVisibleEnemies;
    }

    /// <summary>
    /// Internal method that performs actual scan for visible enemies in FOV and LOS.
    /// </summary>
    private List<RobotController> ScanForEnemiesInRange()
    {
        var visibleEnemies = new List<RobotController>();
        Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRadius, enemyMask);
        float halfAngle = stats.sightAngle * 0.5f;

        foreach (var hit in hits)
        {
            Transform potentialRoot = hit.transform.root;
            if (!potentialRoot.TryGetComponent<RobotController>(out var potentialEnemy))
                continue;
            if (potentialEnemy == controller) continue;

            Vector3 direction = (potentialEnemy.transform.position - transform.position).normalized;

            // FOV check
            if (Vector3.Angle(transform.forward, direction) > halfAngle)
                continue;

            // LOS check
            Vector3 eye = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(eye, direction, out RaycastHit info, DetectionRadius, obstacleMask))
            {
                if (info.transform.root != potentialEnemy.transform.root) continue;
            }

            visibleEnemies.Add(potentialEnemy);
        }

        return visibleEnemies;
    }

    /// <summary>
    /// Returns cached list of all opponents in the scene (excluding self).
    /// Refreshes only once per <see cref="allOpponentsCheckInterval"/>.
    /// </summary>
    public List<RobotController> GetAllOpponents()
    {
        if (Time.time - _lastAllOppCheckTime < allOpponentsCheckInterval)
            return _cachedAllOpponents;

        _lastAllOppCheckTime = Time.time;
        _cachedAllOpponents = ScanAllOpponents();
        return _cachedAllOpponents;
    }

    private List<RobotController> ScanAllOpponents()
    {
        var list = new List<RobotController>();
        var all = UnityEngine.Object.FindObjectsByType<RobotController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < all.Length; i++)
        {
            var rc = all[i];
            if (rc == null || rc == controller) continue;
            var health = rc.GetHealth();
            if (health != null && health.CurrentHealth <= 0f) continue;
            list.Add(rc);
        }
        return list;
    }

    // --- Cache invalidation for pickups ----
    private void OnEnable() => Pickup.PickupsChanged += InvalidatePickupsCache;
    private void OnDisable() => Pickup.PickupsChanged -= InvalidatePickupsCache;

    public void InvalidatePickupsCache()
    {
        _cachedAllPickups.Clear();
        _lastAllPickupsCheckTime = -999f;
    }

    // ---------------- Gizmos ----------------

    private void OnDrawGizmosSelected()
    {
        if (stats == null)
        {
            var ctrl = GetComponent<RobotController>();
            if (ctrl == null || ctrl.GetStats() == null) return;
            stats = ctrl.GetStats();
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        float halfAngle = stats.sightAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir * DetectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * DetectionRadius);
    }
}
