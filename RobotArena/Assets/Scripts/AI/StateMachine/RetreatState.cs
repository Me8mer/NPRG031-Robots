using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// RETREAT state. Robot runs away from visible enemies,
/// preferring positions that increase distance and break line of sight.
/// </summary>
public class RetreatState : IState
{
    private readonly StateMachine _fsm;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Perception _perception;
    private readonly RobotStats _stats;
    private readonly CombatNavigator _nav;

    private float _nextRepathTime;
    private const float RepathInterval = 0.25f;

    // Tunables (could move to RobotStats if needed)
    private const float HopDistance = 12f;   // distance of one retreat hop
    private const int Samples = 10;          // cone samples
    private const float ConeHalfAngle = 55f; // search cone in degrees
    private const float SpeedModifier = 1.2f;



    public RetreatState(StateMachine fsm)
    {
        _fsm = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _perception = _controller.GetPerception();
        _stats = _controller.GetStats();
        _nav = _controller.GetNavigator();
    }

    public void Enter()
    {
        _controller.SetCurrentState(RobotState.Retreat);

        _agent.isStopped = false;
        _agent.autoBraking = false;
        _agent.speed = _controller.GetEffectiveSpeed(SpeedModifier);
        SetBestRetreatPoint();
        _nextRepathTime = Time.time + RepathInterval;

        Debug.Log($"{_controller.name} → Retreat");
    }

    public void Tick()
    {
        // Check if decision layer still wants us retreating
        var decision = _controller.GetDecision();
        if (decision.Move != MovementIntent.Retreat)
        {
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        // Periodically re-evaluate retreat point
        if (Time.time >= _nextRepathTime && !_agent.pathPending)
        {
            SetBestRetreatPoint();
            _nextRepathTime = Time.time + RepathInterval;
        }
    }

    public void Exit() { }

    /// <summary>
    /// Picks the best retreat point away from visible threats
    /// and sets it as NavMeshAgent destination.
    /// </summary>
    private void SetBestRetreatPoint()
    {
        Vector3 origin = _controller.transform.position;
        List<RobotController> threats = _perception.GetEnemiesInRange();

        if (threats.Count == 0)
        {
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        Vector3 best = _nav.FindBestRetreatHop(origin, threats, HopDistance, Samples, ConeHalfAngle);
        _agent.SetDestination(best);
    }
}
