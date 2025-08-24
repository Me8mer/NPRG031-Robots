using UnityEngine;

/// Centralized aiming helpers for one robot.
/// Keeps “where to aim” and “can we shoot” rules in one place.
public sealed class TargetingSolver
{
    private readonly RobotController _c;
    public TargetingSolver(RobotController c) { _c = c; }

    /// Returns a world point on the enemy to aim at.
    public Vector3? AimPoint(RobotController enemy)
    {
        if (enemy == null) return null;

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

    /// Obstacles-only line-of-fire check.
    public bool HasLineOfFire(Vector3 muzzle, Vector3 aimPoint, LayerMask obstacleMask)
    {
        Vector3 dir = (aimPoint - muzzle);
        float d = dir.magnitude;
        if (d <= 0.05f) return true;
        return !Physics.Raycast(muzzle, dir.normalized, d, obstacleMask, QueryTriggerInteraction.Ignore);
    }
    public bool IsAimLocked(Transform turret, Vector3 aimPoint, float lockAngleDeg)
    {
        Vector3 dir = aimPoint - turret.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return true;

        Quaternion target = Quaternion.LookRotation(dir);
        float angle = Quaternion.Angle(turret.rotation, target);
        return angle <= lockAngleDeg;
    }
}
