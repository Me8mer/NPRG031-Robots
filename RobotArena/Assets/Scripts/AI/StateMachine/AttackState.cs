using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Fires at a specific enemy while keeping within attack range.
/// Movement runs at stats.attackSpeedModifier. Armor regen is handled in RobotHealth.
/// Transitions out when the DecisionLayer changes the objective.
/// </summary>
public class AttackState : IState
{
    private readonly StateMachine _stateMachine;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _target;

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
        _agent.speed = _controller.GetEffectiveSpeed(_controller.GetStats().attackSpeedModifier);

        // Nudge the agent toward the enemy to keep them inside range
        if (_target != null)
        {
            _agent.SetDestination(_target.position);
        }

        Debug.Log($"{_controller.name} → Attack {_target?.name ?? "(null)"}");
    }

    public void Tick()
    {
        // Still attacking the same target?
        var objective = _controller.GetObjective();
        bool stillAttack = objective.Type == RobotObjectiveType.AttackEnemy
                           && objective.TargetEnemy != null
                           && objective.TargetEnemy.transform == _target;

        if (!stillAttack)
        {
            StateTransitionHelper.HandleTransition(_stateMachine, _controller);
            return;
        }

        // Keep moving toward target so we maintain range
        if (_target != null && !_agent.pathPending)
        {
            _agent.SetDestination(_target.position);
        }

        // TODO: hook your weapon fire here when you add a weapon system
        // For now this just orients the body via NavMesh steering.
    }

    public void Exit()
    {
        // No special cleanup needed
    }
}
