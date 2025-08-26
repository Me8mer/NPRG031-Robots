using UnityEngine;

/// <summary>
/// Simple forward-moving projectile that deals damage on hit.
/// Includes continuous collision detection via raycasting to avoid tunneling.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Tuning")]
    [Tooltip("Travel speed in meters per second.")]
    public float speed = 30f;

    [Tooltip("Mask of layers this projectile can collide with (robots + environment).")]
    public LayerMask hitMask;

    private Vector3 _prevPos;
    private float _traveled;
    private GameObject _owner;

    private float damage;
    private float maxDistance;

    /// <summary>
    /// Initializes the projectile with dynamic values provided by the firing weapon.
    /// </summary>
    public void Init(GameObject owner, float damageOverride, float speedOverride, float maxDistanceOverride, LayerMask maskOverride)
    {
        _owner = owner;
        if (damageOverride > 0f) damage = damageOverride;
        if (speedOverride > 0f) speed = speedOverride;
        if (maxDistanceOverride > 0f) maxDistance = maxDistanceOverride;
        if (maskOverride.value != 0) hitMask = maskOverride;
    }

    private void Start()
    {
        _prevPos = transform.position;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 dir = transform.forward;

        // Sweep to avoid tunneling through thin colliders
        if (Physics.Raycast(_prevPos, dir, out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit);
            return;
        }

        transform.position += dir * step;
        _traveled += step;
        _prevPos = transform.position;

        if (_traveled >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void HandleHit(RaycastHit hit)
    {
        if (hit.collider != null)
        {
            if (hit.collider.GetComponentInParent<RobotHealth>() is RobotHealth health)
            {
                health.TakeDamage(damage);
            }
        }

        Debug.Log($"Projectile hit {hit.collider?.name ?? "unknown"} for {damage} damage");
        // TODO: optional VFX / impact effect
        Destroy(gameObject);
    }
}
