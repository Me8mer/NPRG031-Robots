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
    private const float MinRepathDist = 0.75f; // do not re-path if new dest is too close
    private const float PathCooldown = 0.10f;  // seconds between SetDestination calls
    private const float RangeStickTolerance = 1.25f;

    private readonly StateMachine _fsm;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _enemy;         // enemy we strafe around

    private int _orbitDir = 1;                 // +1 or -1 for clockwise / counterclockwise
    private float _nextPathTime;               // throttle SetDestination

    public StrafeState(StateMachine fsm, Transform enemyTarget)
    {
        _fsm = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _enemy = enemyTarget;
    }

    public void Enter()
    {
        // Keep the existing enum value for now to avoid ripple changes.
        _controller.SetCurrentState(RobotState.Strafe);

        _agent.isStopped = false;
        _agent.autoBraking = false;
        _agent.speed = _controller.GetEffectiveSpeed(_controller.GetStats().attackSpeedModifier);
        _agent.stoppingDistance = 0.1f;

        _orbitDir = ((_controller.GetInstanceID() & 1) == 0) ? 1 : -1;

        if (_enemy != null)
        {
            float ring = CombatHelpers.ComputeAttackRing(_controller, _enemy, 0.25f);
            Vector3 toMe = (_controller.transform.position - _enemy.position);
            if (toMe == Vector3.zero) toMe = Random.insideUnitSphere;
            toMe.y = 0f;
            Vector3 onRing = _enemy.position + toMe.normalized * ring;
            _agent.SetDestination(onRing);
        }

        _nextPathTime = Time.time + PathCooldown;
        Debug.Log($"{_controller.name} → Strafe {(_enemy != null ? _enemy.name : "<null>")}");
    }

    public void Tick()
    {
        // If the enemy vanished, or the movement objective changed, re-decide.
        if (_enemy == null)
        {
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        var decision = _controller.GetDecision();
        if (decision.Move != MovementIntent.StrafeEnemy || decision.MoveEnemy == null || decision.MoveEnemy.transform != _enemy)
        {
            // If we are right at the ring, allow one more frame to finish a path update.
            if (!StateTransitionHelper.CombatHelpers.InEffectiveAttackRange(_controller, _enemy, RangeStickTolerance))
            {
                StateTransitionHelper.HandleTransition(_fsm, _controller);
                return;
            }
        }

        Vector3 myPos = _controller.transform.position;
        Vector3 tgtPos = _enemy.position;

        Vector3 toTarget = tgtPos - myPos; toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // Desired ring distance uses OUR weapon range
        float ring = CombatHelpers.ComputeAttackRing(_controller, _enemy, 0.25f);

        // Hysteresis based on raw attackRange so tolerance scales with weapon type
        float desired = Mathf.Max(0.1f, _controller.GetStats().attackRange);
        float tol = Mathf.Max(1.0f, desired * 0.15f);

        // 1) Too close → back off to the ring
        if (dist < ring - tol)
        {
            if (dist > 0.05f)
            {
                Vector3 away = (-toTarget / Mathf.Max(0.001f, dist));
                Vector3 backToRing = tgtPos + away * ring;
                SetDestSmart(backToRing);
            }
            return;
        }

        // 2) Too far → do NOT rush inward. Keep same radius and slide tangentially.
        if (dist > ring + tol)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized * _orbitDir;
            Vector3 drift = myPos + tangent * OrbitStep;

            // reproject drift back to the current radius 'dist' to avoid closing distance in strafe
            Vector3 r = drift - tgtPos; r.y = 0f;
            if (r.sqrMagnitude < 0.0001f) r = (myPos - tgtPos);
            Vector3 onSameRadius = tgtPos + r.normalized * dist;

            SetDestSmart(onSameRadius);
            return;
        }

        // 3) Within band → orbit on the ring
        Vector3 onRing = GetOrbitPointOnRing(myPos, tgtPos, ring, _orbitDir, OrbitStep);
        SetDestSmart(onRing);
    }

    public void Exit()
    {
        // nothing to clean up
    }

    private Vector3 GetOrbitPointOnRing(Vector3 myPos, Vector3 tgtPos, float ring, int orbitDir, float step)
    {
        Vector3 toTarget = tgtPos - myPos; toTarget.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized * orbitDir;

        Vector3 drift = myPos + tangent * step;

        Vector3 r = drift - tgtPos; r.y = 0f;
        if (r.sqrMagnitude < 0.0001f) r = (myPos - tgtPos);
        return tgtPos + r.normalized * ring;
    }

    private void SetDestSmart(Vector3 dest)
    {
        if (Time.time < _nextPathTime) return;
        if (Vector3.Distance(_controller.transform.position, dest) < MinRepathDist) return;
        if (!_agent.pathPending)
        {
            _agent.SetDestination(dest);
            _nextPathTime = Time.time + PathCooldown;
        }
    }
}
