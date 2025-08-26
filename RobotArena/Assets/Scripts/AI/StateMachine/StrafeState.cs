using UnityEngine;
using UnityEngine.AI;
using static StateTransitionHelper;

/// <summary>
/// Strafe state:
/// - Active only against the enemy chosen by the DecisionLayer
/// - Requires line of fire to be maintained
/// - Moves onto an attack ring around the enemy
/// - Orbits around the target, flipping orbit direction periodically
/// - All movement requests go through <see cref="CombatNavigator"/>
/// </summary>
public class StrafeState : IState
{
    // --- Constants ---
    private const float OrbitStep = 2.0f;        // step size along tangent while orbiting
    private const float MinRepathDist = 0.35f;   // min delta before re-pathing
    private const float PathCooldown = 0.10f;    // cooldown between SetDestination calls
    private const float RingCushion = 0.5f;      // expands ring radius slightly
    private const float FlipPeriod = 3.0f;       // seconds between orbit direction flips
    private const float SpeedModifier = 0.75f;   // speed multiplier while strafing

    // --- References ---
    private readonly StateMachine _fsm;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _enemy;
    private readonly CombatNavigator _nav;

    // --- State ---
    private int _orbitDir = 1;                   // +1 clockwise, -1 counterclockwise
    private float _nextFlipAt;                   // next time to flip orbit side

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
        _agent.speed = _controller.GetEffectiveSpeed(SpeedModifier);
        _agent.stoppingDistance = 0.1f;

        // Deterministic variation per robot (alternating orbit direction)
        _orbitDir = ((_controller.GetInstanceID() & 1) == 0) ? 1 : -1;

        // Require valid target + LOF at entry
        if (_enemy == null || !_nav.HasLineOfFireTo(_enemy))
        {
            HandleTransition(_fsm, _controller);
            return;
        }

        // Step onto the attack ring initially
        float ring = _nav.ComputeAttackRing(_enemy, RingCushion);
        Vector3 toMe = _controller.transform.position - _enemy.position;
        if (toMe == Vector3.zero) toMe = Random.insideUnitSphere;
        toMe.y = 0f;

        Vector3 onRing = _enemy.position + toMe.normalized * ring;
        _nav.ForceSetDestination(onRing);

        _nextFlipAt = Time.time + FlipPeriod;
    }

    public void Tick()
    {
        if (_enemy == null)
        {
            HandleTransition(_fsm, _controller);
            return;
        }

        // Must still be strafing THIS enemy
        var decision = _controller.GetDecision();
        if (decision.Move != MovementIntent.StrafeEnemy ||
            decision.MoveEnemy == null ||
            decision.MoveEnemy.transform != _enemy)
        {
            HandleTransition(_fsm, _controller);
            return;
        }

        // Only strafe while we have LOF
        if (!_nav.HasLineOfFireTo(_enemy))
        {
            HandleTransition(_fsm, _controller);
            return;
        }

        // Flip orbit direction on timer
        if (Time.time >= _nextFlipAt)
        {
            _orbitDir = -_orbitDir;
            _nextFlipAt += FlipPeriod;
        }

        // Orbit along the ring (keep distance approximately constant)
        Vector3 myPos = _controller.transform.position;
        Vector3 tgtPos = _enemy.position;

        float ring = _nav.ComputeAttackRing(_enemy, RingCushion);
        Vector3 next = _nav.OrbitPointOnRing(myPos, tgtPos, ring, _orbitDir, OrbitStep);

        _nav.TrySetDestinationSmart(next, MinRepathDist, PathCooldown);
    }

    public void Exit() { }
}
