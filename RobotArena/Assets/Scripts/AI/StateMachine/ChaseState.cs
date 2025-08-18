using UnityEngine;
using UnityEngine.AI;

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
        _controller.SetCurrentState(RobotState.Chase);

        // Start moving
        _agent.isStopped = false;
        _agent.speed = _controller.GetEffectiveSpeed(_controller.GetStats().chaseSpeedModifier);
        _agent.SetDestination(_target.position);

        Debug.Log($"{_controller.name} → Chase {_target.name}");
    }

    public void Tick()
    {
        var objective = _controller.GetObjective();

        bool stillChase = false;
        if (objective.Type == RobotObjectiveType.ChaseEnemy && objective.TargetEnemy != null)
        {
            stillChase = (objective.TargetEnemy.transform == _target);
        }
        else if (objective.Type == RobotObjectiveType.SeekPickup && objective.TargetPickup != null)
        {
            stillChase = (objective.TargetPickup.transform == _target);
        }

        if (stillChase)
        {
            if (_target != null && !_agent.pathPending)
            {
                _agent.SetDestination(_target.position);
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
