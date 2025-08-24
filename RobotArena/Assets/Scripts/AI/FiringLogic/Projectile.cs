using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;

public class Projectile : MonoBehaviour
{
    [Header("Tuning")]
    public float speed = 30f;

    public LayerMask hitMask; // Set in prefab: Robots + Environment

    private Vector3 _prevPos;
    private float _traveled;
    private GameObject _owner;


    private float damage;
    private float maxDistance;

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

        // Sweep to avoid tunneling
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
            // Adapt this to your health API
            if (hit.collider.GetComponentInParent<RobotHealth>() is RobotHealth health)
            {
                // If your RobotHealth uses a different method name, change it here.
                health.TakeDamage(damage);
            }
        }
        Debug.Log($"Projectile hit for {damage} damage");
        // TODO optional: spawn impact VFX
        Destroy(gameObject);
    }
}
