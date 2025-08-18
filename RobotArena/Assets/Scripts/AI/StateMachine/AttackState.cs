using UnityEngine;
using UnityEngine.AI;
using static StateTransitionHelper;

/// <summary>
/// Fires at a specific enemy while keeping within attack range.
/// Movement runs at stats.attackSpeedModifier. Armor regen is handled in RobotHealth.
/// Transitions out when the DecisionLayer changes the objective.
/// </summary>
public class AttackState : IState
{
    private const float RangeBuffer = 1.0f;   // how much inside the ring we accept when backing off
    private const float OrbitStep = 2.0f;     // meters per orbit step
    private const float MinRepathDist = 0.75f;// do not re-path if new dest is too close
    private float _nextPathTime;              // throttle re-path spam
    private const float PathCooldown = 0.10f; // seconds between SetDestination calls
    private readonly StateMachine _stateMachine;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _target;

    private int _orbitDir = 1;           // +1 or -1 for clockwise / counterclockwise
    private const float _kiteStepMeters = 2.0f; // how far each orbit step advances

    public AttackState(StateMachine fsm, Transform enemyTarget)
    {
        _stateMachine = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _target = enemyTarget;
    }

    public void Enter()
    {
        _controller.SetCurrentState(RobotState.Attack);

        _agent.isStopped = false;
        _agent.autoBraking = false;
        _agent.speed = _controller.GetEffectiveSpeed(_controller.GetStats().attackSpeedModifier);
        _agent.stoppingDistance = 0.1f;

        _orbitDir = ((_controller.GetInstanceID() & 1) == 0) ? 1 : -1;

        if (_target != null)
        {
            float ring = CombatHelpers.ComputeAttackRing(_controller, _target, 0.25f);
            Vector3 toMe = (_controller.transform.position - _target.position);
            if (toMe == Vector3.zero) toMe = Random.insideUnitSphere;
            toMe.y = 0f;
            Vector3 onRing = _target.position + toMe.normalized * ring;
            _agent.SetDestination(onRing);
        }

        _nextPathTime = Time.time + PathCooldown;
        Debug.Log($"{_controller.name} → Attack {(_target != null ? _target.name : "<null>")}");
    }


    public void Tick()
    {
        if (_target == null)
        {
            StateTransitionHelper.HandleTransition(_stateMachine, _controller);
            return;
        }


        var obj = _controller.GetObjective();
        if (obj.Type != RobotObjectiveType.AttackEnemy || obj.TargetEnemy == null || obj.TargetEnemy.transform != _target)
        {
            StateTransitionHelper.HandleTransition(_stateMachine, _controller);
            return;
        }



        Vector3 myPos = _controller.transform.position;
        Vector3 tgtPos = _target.position;

        Vector3 toTarget = tgtPos - myPos; toTarget.y = 0f;
        float dist = toTarget.magnitude;

        // Our desired ring uses OUR weapon range, not theirs
        float ring = CombatHelpers.ComputeAttackRing(_controller, _target, 0.25f);

        // Hysteresis band in meters, based on our raw attackRange (not the ring),
        // so the tolerance scales with weapon type
        float desired = Mathf.Max(0.1f, _controller.GetStats().attackRange);
        float tol = Mathf.Max(1.0f, desired * 0.15f); // keep your hysteresis

        // 1) Too close -> back off outward to the ring
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

        // 2) Too far -> do not move inward in Attack. Orbit at CURRENT radius.
        if (dist > ring + tol)
        {
            // Project a tangential step and keep the SAME radius we have now
            Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized * _orbitDir;
            Vector3 drift = myPos + tangent * OrbitStep;

            // reproject to current radius 'dist' so we do not close distance here
            Vector3 r = drift - tgtPos; r.y = 0f;
            if (r.sqrMagnitude < 0.0001f) r = (myPos - tgtPos);
            Vector3 onSameRadius = tgtPos + r.normalized * dist;

            SetDestSmart(onSameRadius);
            return;
        }

        // 3) Within the band -> orbit on our ring
        {
            Vector3 onRing = GetOrbitPointOnRing(myPos, tgtPos, ring, _orbitDir, OrbitStep);
            SetDestSmart(onRing);
        }

        // Add aim and fire here later
    }


    public void Exit()
    {
        // No special cleanup needed
    }


    /// <summary>
    /// Our desired center-to-center distance to the target.
    /// Uses our weapon range only, then adds both agent radii and a small cushion
    /// so the robots do not look like they are touching.
    /// </summary>
    private float ComputeRingDistance()
    {
        float desired = Mathf.Max(0.1f, _controller.GetStats().attackRange);

        float myR = _agent != null ? _agent.radius : 0.5f;
        float theirR = 0.5f;
        var targetAgent = _target != null ? _target.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() : null;
        if (targetAgent != null) theirR = targetAgent.radius;

        // final desired distance from target center to our center
        return desired + myR + theirR + 0.25f;
    }

    /// <summary>
    /// Returns a point on the ring around the target at distance 'ring',
    /// advanced tangentially by 'step' in the chosen orbit direction.
    /// </summary>
    private Vector3 GetOrbitPointOnRing(Vector3 myPos, Vector3 tgtPos, float ring, int orbitDir, float step)
    {
        Vector3 toTarget = tgtPos - myPos; toTarget.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toTarget).normalized * orbitDir;

        // take a small tangential step from our current position
        Vector3 drift = myPos + tangent * step;

        // reproject that drift back onto the ring
        Vector3 r = drift - tgtPos; r.y = 0f;
        if (r.sqrMagnitude < 0.0001f) r = (myPos - tgtPos);
        return tgtPos + r.normalized * ring;
    }

    /// <summary>
    /// Sets a destination only if far enough and cooldown passed,
    /// to avoid path spam and jitter.
    /// </summary>
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
