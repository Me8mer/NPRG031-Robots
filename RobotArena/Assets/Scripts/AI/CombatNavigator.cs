using UnityEngine;
using UnityEngine.AI;

/// Centralized LOS + ring math + NavMesh helpers.
/// One instance lives on each RobotController.
public sealed class CombatNavigator
{
    private readonly RobotController _c;
    private readonly NavMeshAgent _agent;
    private readonly Perception _perception;

    private float _nextPathTime;

    public CombatNavigator(RobotController c)
    {
        _c = c;
        _agent = c.GetAgent();
        _perception = c.GetPerception();
    }

    public LayerMask ObstacleMask => _perception != null ? _perception.obstacleMask : 0;

    // ---------- Geometry ----------
    public float ComputeAttackRing(Transform target, float cushion = 0.25f)
    {
        if (_c == null || target == null) return 0.1f;

        float desired = Mathf.Max(0.1f, _c.GetAttackRangeMeters() - 2f);
        float myR = 0.5f; if (_agent) myR = _agent.radius;

        float theirR = 0.5f;
        var ta = target.GetComponentInParent<NavMeshAgent>();
        if (ta) theirR = ta.radius;

        return desired + myR + theirR + Mathf.Max(0f, cushion);
    }

    public bool InEffectiveAttackRange(Transform target, float toleranceMeters = 0.5f)
    {
        if (target == null) return false;
        float ring = ComputeAttackRing(target);
        float dist = Vector3.Distance(_c.transform.position, target.position);
        return dist <= ring + Mathf.Max(0f, toleranceMeters);
    }

    // ---------- Visibility ----------
    public bool HasLineOfSight(Vector3 from, Vector3 to, float lift = 0.5f)
    {
        Vector3 dir = to - from;
        float d = dir.magnitude;
        if (d <= 0.05f) return true;
        Vector3 origin = from + Vector3.up * lift;
        return !Physics.Raycast(origin, dir.normalized, d, ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public bool HasLineOfSight(Transform a, Transform b)
    {
        if (!a || !b) return false;
        return HasLineOfSight(a.position, b.position);
    }
    private static Vector3 ProjectToNavMesh(Vector3 desired)
    {
        return UnityEngine.AI.NavMesh.SamplePosition(desired, out var hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas)
            ? hit.position
            : desired;
    }

    public bool HasLineOfFireTo(Transform enemy)
    {
        if (!enemy) return false;

        var targeting = _c.GetTargeting();
        var enemyRc = enemy.GetComponentInParent<RobotController>();
        Vector3? aim = (enemyRc != null)
            ? targeting.AimPoint(enemyRc)
            : (Vector3?)(enemy.position + Vector3.up * 1.0f);
        if (aim == null) return false;

        var muzzleT = _c.GetFirePointTransform();
        if (muzzleT == null) return false;

        var mask = _c.GetFireObstaclesMask();
        return targeting.HasLineOfFire(muzzleT.position, aim.Value, mask);
    }

    // ---------- Ring orbit helpers ----------
    public Vector3 OrbitPointOnRing(Vector3 myPos, Vector3 tgtPos, float ring, int orbitDir, float stepMeters)
    {
        Vector3 toTarget = tgtPos - myPos; toTarget.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized * orbitDir;
        Vector3 drift = myPos + tangent * stepMeters;
        Vector3 r = drift - tgtPos; r.y = 0f;
        if (r.sqrMagnitude < 1e-6f) r = (myPos - tgtPos);
        return tgtPos + r.normalized * ring;
    }

    public bool TryFindLOSOnRing(Transform target, float cushion, float stepMeters, int arcSamples, out Vector3 losPoint)
    {
        losPoint = _c.transform.position;
        if (!target) return false;

        float ring = ComputeAttackRing(target, cushion);
        Vector3 myPos = _c.transform.position;
        Vector3 tgtPos = target.position;

        int startDir = ((_c.GetInstanceID() & 1) == 0) ? 1 : -1;
        int half = Mathf.Max(1, arcSamples / 2);
        for (int i = 0; i <= half; i++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                int dir = (side == -1) ? -startDir : startDir;
                Vector3 candidate = OrbitPointOnRing(myPos, tgtPos, ring, dir, stepMeters * i);
                if (!NavMesh.SamplePosition(candidate, out var hit, 2.0f, NavMesh.AllAreas)) continue;
                if (HasLineOfSight(hit.position, tgtPos)) { losPoint = hit.position; return true; }
            }
        }
        return false;
    }

    // ---------- Destination throttling ----------
    public bool TrySetDestinationSmart(Vector3 dest, float minRepathDist = 0.5f, float cooldown = 0.10f)
    {
        if (_agent == null) return false;
        if (Time.time < _nextPathTime) return false;
        if (_agent.pathPending) return false;
        if (Vector3.Distance(_c.transform.position, dest) < minRepathDist) return false;

        _agent.SetDestination(ProjectToNavMesh(dest));
        _nextPathTime = Time.time + cooldown;
        return true;
    }

    public bool ForceSetDestination(Vector3 dest, float cooldownAfter = 0.05f)
    {
        if (_agent == null) return false;
        _agent.SetDestination(ProjectToNavMesh(dest));
        _nextPathTime = Time.time + cooldownAfter;
        return true;
    }

    // ---------- Retreat sampling (cover-ish hop) ----------
    public Vector3 FindBestRetreatHop(Vector3 origin, System.Collections.Generic.List<RobotController> threats,
                                      float hopDist, int samples, float halfAngleDeg)
    {
        Vector3 away = Vector3.zero;
        for (int i = 0; i < threats.Count; i++)
        {
            var t = threats[i]; if (!t) continue;
            Vector3 toT = t.transform.position - origin;
            float d = Mathf.Max(0.25f, toT.magnitude);
            away += (-toT / (d * d));
        }
        away.y = 0f;
        if (away == Vector3.zero)
        {
            int sign = (_c.GetInstanceID() & 1) == 0 ? 1 : -1;
            away = Quaternion.Euler(0f, 65f * sign, 0f) * _c.transform.forward;
        }

        Vector3 best = origin + away.normalized * hopDist;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < samples; i++)
        {
            float t = (samples == 1) ? 0f : (float)i / (samples - 1);
            float angle = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * away.normalized;
            Vector3 desired = origin + dir * hopDist;

            if (!NavMesh.SamplePosition(desired, out var hit, 4f, NavMesh.AllAreas))
                continue;

            float score = ScoreRetreatCandidate(hit.position, origin, threats);
            if (score > bestScore) { bestScore = score; best = hit.position; }
        }
        return best;
    }

    private float ScoreRetreatCandidate(Vector3 point, Vector3 origin, System.Collections.Generic.List<RobotController> threats)
    {
        float minDist = float.PositiveInfinity;
        float avgDist = 0f;
        int losBreaks = 0;

        for (int i = 0; i < threats.Count; i++)
        {
            var t = threats[i]; if (!t) continue;
            float d = Vector3.Distance(point, t.transform.position);
            minDist = Mathf.Min(minDist, d);
            avgDist += d;
            if (!HasLineOfSight(t.transform.position, point)) losBreaks++;
        }
        if (threats.Count > 0) avgDist /= threats.Count;

        float score = 0f;
        score += minDist * 1.4f;
        score += avgDist * 0.6f;
        score += losBreaks * 12f;
        score += Mathf.Clamp(Vector3.Distance(point, origin), 0f, 6f) * 0.25f;
        return score;
    }
}
