using System;
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
    private float lastCheckTime;
    public float checkInterval = 0.2f;


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
    /// Performs a sphere / cone / LOS test to find a visible enemy.
    /// </summary>
    /// <param name="enemy">Transform of the first enemy seen or <c>null</c>.</param>
    /// <returns><c>true</c> if at least one enemy is visible.</returns>
    public bool SeeEnemy(out Transform enemy)
    {
        enemy = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectionRadius, enemyMask);
        float halfAngle = stats.sightAngle * 0.5f;
        foreach (var hit in hits)
        {
            Vector3 dir = (hit.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) <= halfAngle)
            {
                // Raycast to ensure nothing blocks vision
                if (!Physics.Raycast(transform.position, dir, out RaycastHit obstacle, stats.detectionRadius, ~enemyMask))
                {
                    enemy = hit.transform;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Simple sphere check for nearby pickups.
    /// </summary>
    /// <param name="pack">Transform of a pickup found, else <c>null</c>.</param>
    public bool SeePickup(out Transform pack)
    {
        pack = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, stats.detectionRadius, pickupMask);
        if (hits.Length > 0)
        {
            pack = hits[0].transform; // TODO: choose nearest
            return true;
        }
        return false;
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
