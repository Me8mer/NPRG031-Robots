using UnityEngine;

public static class AimHelpers
{
    /// Returns the best point to aim at on a target.
    public static Vector3 GetAimPoint(Transform target, float fallbackHeight = 1.0f)
    {
        if (target == null) return Vector3.zero;

        // Prefer explicit anchor
        var anchor = target.GetComponentInChildren<TargetAnchor>();
        if (anchor) return anchor.transform.position;

        // Then try the collider bounds
        if (target.TryGetComponent<Collider>(out var col))
        {
            var b = col.bounds;                 // world space
            return new Vector3(b.center.x, b.min.y + b.size.y * 0.6f, b.center.z);
        }

        // Then any renderer bounds
        var rend = target.GetComponentInChildren<Renderer>();
        if (rend)
        {
            var b = rend.bounds;
            return new Vector3(b.center.x, b.min.y + b.size.y * 0.6f, b.center.z);
        }

        // Fallback to a fixed height over pivot
        return target.position + Vector3.up * Mathf.Max(0.1f, fallbackHeight);
    }
}
