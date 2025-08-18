using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// RETREAT
/// Runs away from all visible enemies and prefers positions that break line of sight.
/// </summary>
public class RetreatState : IState
{
    private readonly StateMachine _fsm;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Perception _perception;
    private readonly RobotStats _stats;

    // Re-path every quarter second so we react but don’t spam pathing
    private float _nextRepathTime;
    private const float RepathInterval = 0.25f;

    // Tunables (can move to stats later if you want)
    private const float HopDistance = 12f;   // how far one retreat hop aims
    private const int Samples = 10;          // cone samples
    private const float ConeHalfAngle = 55f; // degrees around escape dir

    public RetreatState(StateMachine fsm)
    {
        _fsm = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _perception = _controller.GetPerception();
        _stats = _controller.GetStats();
    }

    public void Enter()
    {
        _controller.SetCurrentState(RobotState.Retreat);
        _agent.isStopped = false;
        // Reasonable speed: same as Chase multiplier (fast get-out)
        _agent.speed = _controller.GetEffectiveSpeed(_stats.chaseSpeedModifier);

        SetBestRetreatPoint();
        _nextRepathTime = Time.time + RepathInterval;
        Debug.Log($"{_controller.name} → Retreat");
    }

    public void Tick()
    {
        // If high-level goal changed, let the helper switch state
        var objective = _controller.GetObjective();
        if (objective.Type != RobotObjectiveType.Retreat)
        {
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        if (Time.time >= _nextRepathTime && !_agent.pathPending)
        {
            SetBestRetreatPoint();
            _nextRepathTime = Time.time + RepathInterval;
        }
    }

    public void Exit() { }

    // ---------- Logic ----------

    private void SetBestRetreatPoint()
    {
        Vector3 origin = _controller.transform.position;

        // 1) Collect threats using Perception (already FOV+LOS filtered)
        List<RobotController> threats = _perception.GetEnemiesInRange();
        if (threats.Count == 0)
        {
            // Nobody in sight → let the transition helper take us to Idle/Chase/etc.
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        // 2) Compute “away from the pack” direction (closer threats push more)
        Vector3 awayDir = ComputeAwayDirection(origin, threats);
        if (awayDir == Vector3.zero)
        {
            // Fallback: a stable direction so we don’t stand still
            int sign = (_controller.GetInstanceID() & 1) == 0 ? 1 : -1;
            awayDir = Quaternion.Euler(0f, 65f * sign, 0f) * _controller.transform.forward;
        }

        // 3) Sample a small cone on the NavMesh around awayDir and score
        Vector3 best = origin + awayDir * HopDistance;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < Samples; i++)
        {
            float t = (Samples == 1) ? 0f : (float)i / (Samples - 1);
            float angle = Mathf.Lerp(-ConeHalfAngle, ConeHalfAngle, t);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * awayDir;
            Vector3 desired = origin + dir * HopDistance;

            // Snap to NavMesh
            if (!NavMesh.SamplePosition(desired, out var hit, 4f, NavMesh.AllAreas))
                continue;

            float score = ScoreCandidate(hit.position, origin, threats);
            if (score > bestScore)
            {
                bestScore = score;
                best = hit.position;
            }
        }

        _agent.SetDestination(best);
    }

    private Vector3 ComputeAwayDirection(Vector3 origin, List<RobotController> threats)
    {
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < threats.Count; i++)
        {
            var t = threats[i];
            if (t == null) continue;
            Vector3 toThreat = t.transform.position - origin;
            float d = Mathf.Max(0.25f, toThreat.magnitude);
            sum += (-toThreat / (d * d)); // inverse-square weighting
        }
        sum.y = 0f;
        return (sum.sqrMagnitude > 1e-6f) ? sum.normalized : Vector3.zero;
    }

    private float ScoreCandidate(Vector3 point, Vector3 origin, List<RobotController> threats)
    {
        // Prefer: bigger minimum distance to any threat, bigger average distance,
        // bonus if most threats lose LOS to the point, and actually moving away.
        float minDist = float.PositiveInfinity;
        float avgDist = 0f;
        int losBreaks = 0;

        for (int i = 0; i < threats.Count; i++)
        {
            var t = threats[i]; if (t == null) continue;
            float d = Vector3.Distance(point, t.transform.position);
            minDist = Mathf.Min(minDist, d);
            avgDist += d;

            if (!HasLineOfSight(t.transform.position, point))
                losBreaks++;
        }
        if (threats.Count > 0) avgDist /= threats.Count;

        float score = 0f;
        score += minDist * 1.4f;
        score += avgDist * 0.6f;
        score += losBreaks * 12f;
        score += Mathf.Clamp(Vector3.Distance(point, origin), 0f, 6f) * 0.25f;
        return score;
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        // Use Perception’s obstacleMask for LOS; keep all mask ownership there.
        Vector3 dir = to - from;
        float d = dir.magnitude;
        if (d <= 0.05f) return true;

        // small lift to avoid ground hits
        var origin = from + Vector3.up * 0.5f;
        return !Physics.Raycast(origin, dir.normalized, d, _perception.obstacleMask, QueryTriggerInteraction.Ignore);
    }
}
