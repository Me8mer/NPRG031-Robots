using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// IDLE state. Robot stays stationary until another intent is decided.
/// </summary>
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

        // Stop movement completely
        _agent.isStopped = true;
        _agent.speed = 0f;

        // Armor regen is now handled in RobotHealth.Update()
        Debug.Log($"{_controller.name} → Idle");
    }

    public void Tick()
    {
        // Delegate state change decisions to the shared transition helper
        StateTransitionHelper.HandleTransition(_stateMachine, _controller);
    }

    public void Exit()
    {
        // No cleanup needed for Idle
    }
}
