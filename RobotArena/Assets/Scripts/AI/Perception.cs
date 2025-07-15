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
    //private Transform cachedEnemy;
    //private bool lastEnemyResult;

    /// <summary>How often (seconds) to refresh cached queries.</summary>
    //private float lastCheckTime;
    public float checkInterval = 0.2f;
    private float lastEnemyCheckTime;
    private float lastPickupCheckTime;
    private List<Transform> cachedVisiblePickups = new();

    private List<Transform> cachedVisibleEnemies = new();

    [Tooltip("Obstacle layers that block vision")]
    public LayerMask obstacleMask;


    [Header("Detection Layers")]
    [Tooltip("Layer mask for enemy robots")]
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


    /// <summary>
    /// Returns cached list of visible pickups, updating at most every checkInterval seconds.
    /// </summary>
    public List<Transform> GetPickupsInRange()
    {
        if (Time.time - lastPickupCheckTime < checkInterval)
            return cachedVisiblePickups;

        lastPickupCheckTime = Time.time;
        cachedVisiblePickups = ScanForPickupsInRange();
        return cachedVisiblePickups;
    }
    /// <summary>
    /// Performs actual scan for visible pickups.
    /// </summary>
    private List<Transform> ScanForPickupsInRange()
    {
        List<Transform> visiblePickups = new();
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectionRadius, pickupMask);
        float halfAngle = stats.sightAngle * 0.5f;

        foreach (var hit in hits)
        {
            Transform pickup = hit.transform.root;

            Vector3 direction = (pickup.position - transform.position).normalized;

            // FOV check
            if (Vector3.Angle(transform.forward, direction) > halfAngle)
                continue;

            // LOS check
            if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, stats.detectionRadius, obstacleMask))
            {
                if (hitInfo.transform.root != pickup)
                    continue;
            }

            visiblePickups.Add(pickup);
        }

        return visiblePickups;
    }


    public bool CanSee(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        float halfAngle = stats.sightAngle * 0.5f;
        if (Vector3.Angle(transform.forward, dir) > halfAngle)
            return false;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, stats.detectionRadius, obstacleMask))
            return hit.transform == target;

        return true;
    }

    /// <summary>
    /// Returns cached list of visible enemies, updating it at most every checkInterval seconds.
    /// </summary>
    public List<Transform> GetEnemiesInRange()
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
    private List<Transform> ScanForEnemiesInRange()
    {
        List<Transform> visibleEnemies = new();
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectionRadius, enemyMask);
        float halfAngle = stats.sightAngle * 0.5f;

        foreach (var hit in hits)
        {
            Transform potentialEnemy = hit.transform.root;
            if (potentialEnemy == transform) continue; // skip self
            if (!potentialEnemy.TryGetComponent<RobotController>(out _)) continue;

            Vector3 direction = (potentialEnemy.position - transform.position).normalized;

            // FOV check
            if (Vector3.Angle(transform.forward, direction) > halfAngle)
                continue;

            // Line of sight check
            if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, stats.detectionRadius, obstacleMask))
            {
                if (hitInfo.transform.root != potentialEnemy)
                    continue;
            }

            visibleEnemies.Add(potentialEnemy);
        }

        return visibleEnemies;
    }




    /// <summary>
    /// Draws the detection radius and FOV cone when the robot is
    /// selected in the Unity Editor Scene view.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw detection radius and sight cone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.detectionRadius);
        Vector3 forward = transform.forward * stats.detectionRadius;
        float halfAngleRad = stats.sightAngle * 0.5f * Mathf.Deg2Rad;
        Vector3 leftDir = Quaternion.Euler(0, -stats.sightAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, stats.sightAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * stats.detectionRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * stats.detectionRadius);
    }
}
