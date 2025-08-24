using UnityEngine;
using UnityEngine.AI;
using static StateTransitionHelper;

/// <summary>
/// Movement-only state that maintains distance and strafes around a specific enemy.
/// Shooting is handled outside of states.
/// </summary>
public class StrafeState : IState
{
    private const float OrbitStep = 2.0f;      // meters per orbit step
    private const float MinRepathDist = 0.35f; // do not re-path if new dest is too close
    private const float PathCooldown = 0.10f;  // seconds between SetDestination calls
    private const float RangeStickTolerance = 0.35f;

    private readonly StateMachine _fsm;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _enemy;         // enemy we strafe around
    private CombatNavigator _nav;

    private int _orbitDir = 1;                 // +1 or -1 for clockwise / counterclockwise

    public StrafeState(StateMachine fsm, Transform enemyTarget)
    {
        _fsm = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _enemy = enemyTarget;
        _nav = _controller.GetNavigator();
    }

    public void Enter()
    {
        _controller.SetCurrentState(RobotState.Strafe);

        _agent.isStopped = false;
        _agent.autoBraking = false;
        _agent.speed = _controller.GetEffectiveSpeed(1f);
        _agent.stoppingDistance = 0.1f;

        _orbitDir = ((_controller.GetInstanceID() & 1) == 0) ? 1 : -1;

        if (_enemy != null)
        {
            float ring = _nav.ComputeAttackRing(_enemy, 0.25f);
            Vector3 toMe = (_controller.transform.position - _enemy.position);
            if (toMe == Vector3.zero) toMe = Random.insideUnitSphere;
            toMe.y = 0f;
            Vector3 onRing = _enemy.position + toMe.normalized * ring;
            _agent.SetDestination(onRing);
        }
    }

    public void Tick()
    {
        if (_enemy == null)
        {
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        var decision = _controller.GetDecision();
        if (decision.Move != MovementIntent.StrafeEnemy || decision.MoveEnemy == null || decision.MoveEnemy.transform != _enemy)
        {
            if (!_nav.InEffectiveAttackRange(_enemy, RangeStickTolerance))
            {
                StateTransitionHelper.HandleTransition(_fsm, _controller);
                return;
            }
        }

        Vector3 myPos = _controller.transform.position;
        Vector3 tgtPos = _enemy.position;

        Vector3 toTarget = tgtPos - myPos; toTarget.y = 0f;
        float dist = toTarget.magnitude;

        float ring = _nav.ComputeAttackRing(_enemy, 0.25f);

        float desired = Mathf.Max(0.1f, _controller.GetStats().attackRange);
        float tol = Mathf.Clamp(_controller.GetAttackRangeMeters() * 0.05f, 0.25f, 0.75f);

        var targeting = _controller.GetTargeting();
        var fireMask = _controller.GetFireObstaclesMask();
        var fireT = _controller.GetFirePointTransform();
        var enemyRC = _enemy ? _enemy.GetComponentInParent<RobotController>() : null;

        Vector3? aimPt = (enemyRC != null) ? targeting.AimPoint(enemyRC) : null;
        bool hasLOF = aimPt.HasValue && targeting.HasLineOfFire(fireT.position, aimPt.Value, fireMask);

        // Inside ring and can shoot -> back out to ring
        if (hasLOF && dist < ring - tol)
        {
            Vector3 outDir = (myPos - tgtPos); outDir.y = 0f;
            if (outDir.sqrMagnitude < 1e-6f) outDir = Random.insideUnitSphere;
            Vector3 backToRing = tgtPos + outDir.normalized * ring;
            _nav.TrySetDestinationSmart(backToRing, MinRepathDist, PathCooldown);
            return;
        }

        // Too far outside -> snap back to ring
        if (dist > ring + tol)
        {
            Vector3 r = (myPos - tgtPos); r.y = 0f;
            if (r.sqrMagnitude < 1e-6f) r = Random.insideUnitSphere;
            Vector3 onRing = tgtPos + r.normalized * ring;
            _nav.TrySetDestinationSmart(onRing, MinRepathDist, PathCooldown);
            return;
        }

        // LOF blocked -> immediately slide to regain LOF at same radius if needed
        if (!hasLOF)
        {
            if (_nav.TryFindLOSOnRing(_enemy, 0.25f, OrbitStep, 8, out var losPoint))
            {
                _nav.ForceSetDestination(losPoint, 0.05f);
            }
            else
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized * _orbitDir;
                Vector3 drift = myPos + tangent * OrbitStep;

                Vector3 r = drift - tgtPos; r.y = 0f;
                if (r.sqrMagnitude < 1e-6f) r = (myPos - tgtPos);
                Vector3 onSameRadius = tgtPos + r.normalized * dist;

                _nav.ForceSetDestination(onSameRadius, 0.05f);
            }
            return;
        }

        // Within band and LOF ok -> orbit
        Vector3 next = _nav.OrbitPointOnRing(myPos, tgtPos, ring, _orbitDir, OrbitStep);
        _nav.TrySetDestinationSmart(next, MinRepathDist, PathCooldown);
    }

    public void Exit() { }
}
