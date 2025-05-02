using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Default state: robot stands still, faces the first visible enemy,
/// and regenerates armor at the highest rate.
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
        // Stop moving
        _agent.isStopped = true;
        _agent.speed = 0f;

        // TODO rethink where armor regen trigger is
        // Optional: start full armor regen rate
        _controller.GetHealth().SetArmorRegen(_controller.GetStats().armorRegenIdle);

        // Debug
        Debug.Log($"{_controller.name} → Idle");
    }

    public void Tick()
    {
        // 1. Look for an enemy
        if (_controller.GetPerception().SeeEnemy(out Transform enemy))
        {
            // Enemy spotted – switch to Chase later
            // For now just spin to face it for visual feedback
            _controller.transform.LookAt(enemy.position, Vector3.up);

            // TODO: _fsm.ChangeState(new ChaseState(_fsm, enemy));
        }
        // 2. Could also scan for bonus packs here
    }

    public void Exit()
    {
        // No teardown needed yet
    }
}
