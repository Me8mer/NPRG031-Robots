using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIDebugProbe : MonoBehaviour
{
    [Header("Toggles")]
    [SerializeField] private bool enableProbe = true;
    [SerializeField] private bool drawGizmos = true;

    [Header("Env")]
    [SerializeField] private LayerMask wallMask;

    [Header("Stuck Heuristics")]
    [SerializeField] private float minRemainToCare = 0.5f;
    [SerializeField] private float slowSpeed = 0.05f;
    [SerializeField] private float stuckSeconds = 0.6f;

    private NavMeshAgent _agent;
    private RobotController _rc;
    private CombatNavigator _nav;
    private float _stuckTimer;
    private string _lastStuckMsg;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rc = GetComponent<RobotController>();
        if (_rc != null) _nav = _rc.GetNavigator();

        // Common misconfig: non-kinematic rigidbody + NavMeshAgent
        var rb = GetComponent<Rigidbody>();
        if (rb && !rb.isKinematic)
        {
            Debug.LogWarning($"[AIDbg] {name}: Non-kinematic Rigidbody together with NavMeshAgent. Physics can push you through walls or fight the agent. Consider isKinematic=true.");
        }
    }

    void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[AIDbg] {name} beat: " +
                      $"hasPath={_agent.hasPath} " +
                      $"status={_agent.pathStatus} " +
                      $"rem={_agent.remainingDistance:F2} " +
                      $"isStopped={_agent.isStopped} " +
                      $"onNavMesh={_agent.isOnNavMesh} " +
                      $"vel={_agent.velocity.magnitude:F2} " +
                      $"desired={_agent.desiredVelocity.magnitude:F2}");
        }
        if (!enableProbe || _agent == null) return;

        // 1) LOS/LOF sanity
        bool lof = HasLOF();

        // 2) Path sanity
        bool pathOK = _agent.hasPath && !_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathComplete;

        // 3) Motion sanity
        bool tryingToMove = _agent.remainingDistance > minRemainToCare && _agent.desiredVelocity.sqrMagnitude > 0.001f;
        bool barelyMoving = _agent.velocity.magnitude < slowSpeed;

        // 4) Wall proximity / wedge
        bool nearWall = NearWall(out var hitCol, out var depen);
        bool navEdgeBlock = NavBlockedToDestination(out var hitPos);

        if (tryingToMove && barelyMoving)
        {
            _stuckTimer += Time.deltaTime;
        }
        else
        {
            _stuckTimer = 0f;
        }

        if (_stuckTimer >= stuckSeconds)
        {
            string reason = "";
            if (!pathOK) reason += " path!=OK";
            if (nearWall) reason += $" wedge({hitCol?.name}) depen={depen:F2}";
            if (navEdgeBlock) reason += $" navRayHit@{hitPos}";
            if (!lof) reason += " noLOF";

            string msg = $"[AIDbg] STUCK {name}: vel={_agent.velocity.magnitude:F2} rem={_agent.remainingDistance:F2} desired={_agent.desiredVelocity.magnitude:F2} " +
                         $"status={_agent.pathStatus} hasPath={_agent.hasPath} pending={_agent.pathPending} isOnNavMesh={_agent.isOnNavMesh} |{reason}";
            if (msg != _lastStuckMsg)
            {
                Debug.Log(msg);
                _lastStuckMsg = msg;
            }
        }
    }

    private bool HasLOF()
    {
        if (_rc == null) return false;
        var enemy = _rc.GetDecision().MoveEnemy;
        if (enemy == null) return false;

        var targeting = _rc.GetTargeting();
        var fireT = _rc.GetFirePointTransform();
        if (targeting == null || fireT == null) return false;

        Vector3? aim = targeting.AimPoint(enemy);
        if (!aim.HasValue) return false;

        return targeting.HasLineOfFire(fireT.position, aim.Value, _rc.GetFireObstaclesMask());
    }

    private bool NearWall(out Collider hitCol, out float depenMagnitude)
    {
        hitCol = null;
        depenMagnitude = 0f;
        float r = Mathf.Max(_agent.radius * 0.95f, 0.1f);
        var cols = Physics.OverlapSphere(transform.position, r, wallMask, QueryTriggerInteraction.Ignore);
        if (cols == null || cols.Length == 0) return false;

        // Try to compute depenetration against first collider
        foreach (var c in cols)
        {
            Vector3 dir;
            float dist;
            // Use a small sphere matching agent radius
            bool overlap = Physics.ComputePenetration(
                c, c.transform.position, c.transform.rotation,
                // Represent the agent as a small capsule upright
                gameObject.AddComponent<SphereCollider>(), transform.position, transform.rotation,
                out dir, out dist
            );
            // Avoid actually adding a collider
            var sc = GetComponent<SphereCollider>();
            if (sc) Destroy(sc);

            if (overlap)
            {
                hitCol = c;
                depenMagnitude = dist;
                return true;
            }
        }
        return true; // near wall, even if not penetrating
    }

    private bool NavBlockedToDestination(out Vector3 hitPos)
    {
        hitPos = default;
        if (!_agent.hasPath || _agent.pathPending) return false;

        var corners = _agent.path.corners;
        if (corners == null || corners.Length < 2) return false;

        // cast along last segment to see if navmesh edge blocks
        Vector3 from = corners[corners.Length - 2];
        Vector3 to = corners[corners.Length - 1];
        if (NavMesh.Raycast(from, to, out var hit, NavMesh.AllAreas))
        {
            hitPos = hit.position;
            return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || _agent == null) return;

        // Path corners
        if (_agent.hasPath)
        {
            var corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Debug.DrawLine(corners[i] + Vector3.up * 0.05f, corners[i + 1] + Vector3.up * 0.05f, Color.cyan);
            }
        }

        // Desired velocity arrow
        Vector3 p = transform.position;
        Debug.DrawLine(p, p + _agent.desiredVelocity, Color.yellow);

        // LOF ray
        if (Application.isPlaying && _rc != null)
        {
            var enemy = _rc.GetDecision().MoveEnemy;
            var targeting = _rc.GetTargeting();
            var fireT = _rc.GetFirePointTransform();
            if (enemy && targeting != null && fireT != null)
            {
                var aim = targeting.AimPoint(enemy);
                if (aim.HasValue)
                {
                    Debug.DrawLine(fireT.position, aim.Value, Color.red);
                }
            }

            // Attack ring, if navigator is available
            if (_nav != null && enemy)
            {
                float ring = _nav.ComputeAttackRing(enemy.transform, 1.0f);
                DrawRing(enemy.transform.position, ring, new Color(0.2f, 0.9f, 0.2f, 0.8f));
            }
        }

        // Agent radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(_agent.radius * 0.95f, 0.1f));
    }

    private void DrawRing(Vector3 center, float radius, Color c)
    {
        Gizmos.color = c;
        const int segs = 40;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segs; i++)
        {
            float t = (i / (float)segs) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
            Debug.DrawLine(prev + Vector3.up * 0.02f, next + Vector3.up * 0.02f, c);
            prev = next;
        }
    }
}
