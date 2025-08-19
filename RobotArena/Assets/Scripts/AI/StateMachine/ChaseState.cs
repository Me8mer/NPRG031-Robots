using UnityEngine;
using UnityEngine.AI;
using static StateTransitionHelper;

/// <summary>
/// Pursues a specific target (enemy or pickup) until the decision layer requests a transition.
/// </summary>
public class ChaseState : IState
{
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
        _agent.isStopped = false;
        _agent.speed = _controller.GetEffectiveSpeed(_controller.GetStats().chaseSpeedModifier);

        if (_target.GetComponent<Pickup>() != null)
        {
            _agent.stoppingDistance = 0f;
            _agent.autoBraking = true;
        }
        else
        {
            float ring = CombatHelpers.ComputeAttackRing(_controller, _target, 0.25f);
            _agent.stoppingDistance = Mathf.Max(0.25f, ring - 0.25f);
            _agent.autoBraking = true;
        }

        _agent.SetDestination(_target.position);
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
            if (_target != null && !_agent.pathPending)
            {
                if (Vector3.Distance(_agent.destination, _target.position) > 1.0f)
                {
                    _agent.SetDestination(_target.position);
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
