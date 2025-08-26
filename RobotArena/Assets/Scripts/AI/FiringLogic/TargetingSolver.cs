using UnityEngine;

/// <summary>
/// Centralized aiming helpers for one robot.
/// Keeps all "where to aim" and "can we shoot" rules in one place,
/// so they are consistent across states and controller logic.
/// </summary>
public sealed class TargetingSolver
{
    private readonly RobotController _c;

    public TargetingSolver(RobotController c)
    {
        _c = c;
    }

    /// <summary>
    /// Returns a world point on the enemy to aim at.
    /// Prefers collider bounds, then renderer bounds, then a fallback above pivot.
    /// </summary>
    public Vector3? AimPoint(RobotController enemy)
    {
        if (enemy == null) return null;
        //prefer anchor
        var anchor = enemy.GetComponentInChildren<TargetAnchor>(true);
        if (anchor != null) return anchor.transform.position;

        // Prefer collider bounds
        if (enemy.TryGetComponent<Collider>(out var col))
        {
            var b = col.bounds;
            return new Vector3(b.center.x, b.min.y + b.size.y * 0.6f, b.center.z);
        }

        // Then renderer bounds
        var rend = enemy.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var b = rend.bounds;
            return new Vector3(b.center.x, b.min.y + b.size.y * 0.6f, b.center.z);
        }

        // Fallback: fixed height above pivot
        return enemy.transform.position + Vector3.up * 1.0f;
    }

    /// <summary>
    /// Checks whether a straight line exists from muzzle to aimPoint
    /// that is not blocked by environment obstacles.
    /// Robots themselves are intentionally ignored.
    /// </summary>
    public bool HasLineOfFire(Vector3 muzzle, Vector3 aimPoint, LayerMask obstacleMask)
    {
        Vector3 dir = aimPoint - muzzle;
        float d = dir.magnitude;
        if (d <= 0.05f) return true;

        return !Physics.Raycast(muzzle, dir.normalized, d, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Returns true if the turret is facing the aimPoint within lock tolerance.
    /// </summary>
    public bool IsAimLocked(Transform turret, Vector3 aimPoint, float lockAngleDeg)
    {
        Vector3 dir = aimPoint - turret.position;
        dir.y = 0f; // ignore vertical tilt for yaw lock
        if (dir.sqrMagnitude < 0.0001f) return true;

        Quaternion target = Quaternion.LookRotation(dir);
        float angle = Quaternion.Angle(turret.rotation, target);
        return angle <= lockAngleDeg;
    }
}
