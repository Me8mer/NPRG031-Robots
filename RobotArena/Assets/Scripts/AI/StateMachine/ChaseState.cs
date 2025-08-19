using UnityEngine;
using UnityEngine.AI;
using static StateTransitionHelper;

/// <summary>
/// Pursues a specific target (enemy or pickup) until the decision layer requests a transition.
/// </summary>
public class ChaseState : IState
{
    private const float PathCooldown = 0.10f;
    private const float MinRepathDist = 0.5f;
    private float _nextPathTime;
    private bool _chasingPickup;
    private readonly StateMachine _stateMachine;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _target;

    /// <summary>
    /// Creates a new ChaseState that will follow <paramref name="target"/>.
    /// </summary>
    public ChaseState(StateMachine fsm, Transform target)
    {
        _stateMachine = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _target = target;
    }

    public void Enter()
    {
        Debug.DrawLine(_controller.transform.position,
               _target.position,
               Color.red,
               1f);
        if (_target == null)
        {
            StateTransitionHelper.HandleTransition(_stateMachine, _controller);
            return;
        }

        _controller.SetCurrentState(RobotState.Chase);
        _chasingPickup = _target.GetComponent<Pickup>() != null;

        _agent.isStopped = false;
        _agent.speed = _controller.GetEffectiveSpeed(_controller.GetStats().chaseSpeedModifier);
        _agent.stoppingDistance = 0.15f;   // small cushion
        _agent.autoBraking = false;        // do not full-stop at the point
        _agent.acceleration = Mathf.Max(_agent.acceleration, 16f); // snappier restart

        if (_chasingPickup)
        {
            // If we were chasing a pickup and it just got consumed, bail immediately.
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                StateTransitionHelper.HandleTransition(_stateMachine, _controller);
                return;
            }
            _agent.stoppingDistance = 0f;
            _agent.autoBraking = true;   // precise stop at pickup
        }
        else
        {
            float ring = CombatHelpers.ComputeAttackRing(_controller, _target, 0.25f);
            _agent.stoppingDistance = Mathf.Max(0.25f, ring - 0.25f);
            _agent.autoBraking = true;   // gentle braking before ring
        }

        _agent.SetDestination(_target.position);
        _nextPathTime = 0f;
        Debug.Log($"{_controller.name} → Chase {_target.name}");
    }

    public void Tick()
    {
        var decision = _controller.GetDecision();

        bool stillChase = false;
        if (decision.Move == MovementIntent.ChaseEnemy && decision.MoveEnemy != null)
        {
            stillChase = (decision.MoveEnemy.transform == _target);
        }
        else if (decision.Move == MovementIntent.ChasePickup && decision.MovePickup != null)
        {
            stillChase = (decision.MovePickup.transform == _target);
        }

        if (stillChase)
        {
            if (Time.time >= _nextPathTime && !_agent.pathPending)
            {
                float drift = Vector3.Distance(_agent.destination, _target.position);
                if (drift > MinRepathDist)
                {
                    _agent.SetDestination(_target.position);
                    _nextPathTime = Time.time + PathCooldown;
                }
            }
            return;
        }

        StateTransitionHelper.HandleTransition(_stateMachine, _controller);
    }


    public void Exit()
    {
        // No cleanup needed
    }
}
