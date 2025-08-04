using UnityEngine;
using UnityEngine.AI;

public class IdleState : IState
{
    private readonly StateMachine _stateMachine;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;

    public IdleState(StateMachine fsm)
    {
        _stateMachine = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
    }

    public void Enter()
    {
        _controller.SetCurrentState(RobotState.Idle);

        // stop moving
        _agent.isStopped = true;
        _agent.speed = 0f;

        // full armor regen
        _controller.GetHealth().SetArmorRegen(_controller.GetStats().armorRegenIdle);

        Debug.Log($"{_controller.name} → Idle");
    }

    public void Tick()
    {
        // 1. get the global intent
        var objective = _controller.GetObjective();

        // 2. hand off to the right state
        switch (objective.Type)
        {
            case RobotObjectiveType.SeekPickup:
                _stateMachine.ChangeState(new ChaseState(_stateMachine, objective.TargetPickup));
                break;

            case RobotObjectiveType.ChaseEnemy:
                _stateMachine.ChangeState(new ChaseState(_stateMachine, objective.TargetEnemy));
                break;

            case RobotObjectiveType.AttackEnemy:
                _stateMachine.ChangeState(new AttackState(_stateMachine, objective.TargetEnemy));
                break;

            case RobotObjectiveType.Retreat:
                _stateMachine.ChangeState(new RetreatState(_stateMachine));
                break;

            case RobotObjectiveType.Idle:
            default:
                // stay here until something changes
                break;
        }
    }

    public void Exit()
    {
        // nothing to clean up
    }
}
