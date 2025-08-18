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
        //DEPOREACTED computed in health
        //_controller.GetHealth().SetArmorRegen(_controller.GetStats().armorRegenIdle);

        Debug.Log($"{_controller.name} → Idle");
    }

    public void Tick()
    {
        // Delegate any state change to the shared helper
        StateTransitionHelper.HandleTransition(_stateMachine, _controller);
    }


    public void Exit()
    {
        // nothing to clean up
    }
}
