using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Handles spatial awareness. Detects enemies and pickups using a
/// sphere overlap + cone test + optional line‑of‑sight raycast.
/// </summary>
[RequireComponent(typeof(RobotController))]
public class Perception : MonoBehaviour
{
    private RobotController controller;
    private RobotStats stats;

    /// <summary>How often (seconds) to refresh cached queries.</summary>
    //private float lastCheckTime;
    public float checkInterval = 0.2f;
    private float lastEnemyCheckTime;
    private float lastPickupCheckTime;
    private float _lastAllOppCheckTime;
    private float _lastAllPickupsCheckTime;
    private List<Pickup> _cachedAllPickups = new();

    private static readonly Collider[] _enemyHits = new Collider[64];
    private static readonly Collider[] _pickupHits = new Collider[64];

    private List<Pickup> cachedVisiblePickups = new();
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

    void Awake()
    {
        controller = GetComponent<RobotController>();
        stats = controller.GetStats();
    }
    // Currently unused for caching but left for potential optimisation.
    void Update()
    {
    }

    // ---------------- PICKUPS ----------------
    /// <summary>
    /// Returns cached list of visible pickups, updating at most every checkInterval seconds.
    /// </summary>
    //public List<Pickup> GetPickupsInRange()
    //{
    //    if (Time.time - lastPickupCheckTime < checkInterval)
    //        return cachedVisiblePickups;

    //    lastPickupCheckTime = Time.time;
    //    cachedVisiblePickups = ScanPickupsInRange();
    //    return cachedVisiblePickups;
    //}

    ///// <summary>
    ///// Performs actual scan for visible pickups.
    ///// </summary>
    //private List<Pickup> ScanForPickupsInRange()
    //{
    //    List<Pickup> visiblePickups = new();
    //    Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectionRadius, pickupMask);
    //    float halfAngle = stats.sightAngle * 0.5f;

    //    foreach (var hit in hits)
    //    {
    //        // Get root with Pickup
    //        Transform pickupRoot = hit.transform.root;
    //        if (!pickupRoot.TryGetComponent<Pickup>(out var pickup))
    //            continue;

    //        // Direction and FOV check
    //        Vector3 direction = (pickup.transform.position - transform.position).normalized;
    //        if (Vector3.Angle(transform.forward, direction) > halfAngle)
    //            continue;

    //        // Line-of-sight check
    //        Vector3 eye = transform.position + Vector3.up * 0.5f;
    //        if (Physics.Raycast(eye, direction, out RaycastHit hitInfo, stats.detectionRadius, obstacleMask))
    //        {
    //            if (hitInfo.transform.root != pickup.transform)
    //                continue;
    //        }

    //        visiblePickups.Add(pickup);
    //    }

    //    return visiblePickups;
    //}

    // Perception.cs  (add alongside GetAllOpponents)
    public List<Pickup> GetAllPickups()
    {
        if (Time.time - _lastAllPickupsCheckTime < allPickupsCheckInterval)
            return _cachedAllPickups;

        _lastAllPickupsCheckTime = Time.time;
        _cachedAllPickups = ScanAllPickups();
        return _cachedAllPickups;
    }

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
            if (!p.isActiveAndEnabled) continue;      // skip disabled or already consumed
            if (!p.gameObject.activeInHierarchy) continue;
            list.Add(p);
        }
        return list;
    }


    // ---------------- OPPONENTS ----------------

    public bool CanSeeEnemy(RobotController target)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        float halfAngle = stats.sightAngle * 0.5f;
        if (Vector3.Angle(transform.forward, dir) > halfAngle)
            return false;

        Vector3 eye = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(eye, dir, out RaycastHit hit, stats.detectionRadius, obstacleMask))
        {
            return hit.transform.root == target.transform.root;
        }

        return true;
    }

    /// <summary>
    /// Returns cached list of visible enemies, updating it at most every checkInterval seconds.
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
    /// Internal method that performs actual scan for visible enemies.
    /// </summary>
    private List<RobotController> ScanForEnemiesInRange()
    {
        List<RobotController> visibleEnemies = new();
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectionRadius, enemyMask);
        float halfAngle = stats.sightAngle * 0.5f;

        foreach (var hit in hits)
        {
            Transform potentialEnemyRoot = hit.transform.root;
            if (!potentialEnemyRoot.TryGetComponent<RobotController>(out var potentialEnemy))
                continue;
            if (potentialEnemy == controller) continue; // skip self


            Vector3 direction = (potentialEnemy.transform.position - transform.position).normalized;

            // FOV check
            if (Vector3.Angle(transform.forward, direction) > halfAngle)
                continue;

            // Line of sight check
            Vector3 eye = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(eye, direction, out RaycastHit info, stats.detectionRadius, obstacleMask))
            {
                if (info.transform.root != potentialEnemy.transform.root) continue;
            }

            visibleEnemies.Add(potentialEnemy);
        }

        return visibleEnemies;
    }


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
            if (rc == null || rc == controller) continue;              // not me
            var health = rc.GetHealth();
            if (health != null && health.CurrentHealth <= 0f) continue; // skip dead
            list.Add(rc);
        }
        return list;

    }



    /// <summary>
    /// Draws the detection radius and FOV cone when the robot is
    /// selected in the Unity Editor Scene view.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Ensure we have valid stats reference in editor
        if (stats == null)
        {
            var ctrl = GetComponent<RobotController>();
            if (ctrl == null || ctrl.GetStats() == null)
                return;
            stats = ctrl.GetStats();
        }

        // Draw detection radius and sight cone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.detectionRadius);

        float halfAngle = stats.sightAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir * stats.detectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * stats.detectionRadius);
    }
}
