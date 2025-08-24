using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// RETREAT
/// Runs away from all visible enemies and prefers positions that break line of sight.
/// </summary>
public class RetreatState : IState
{
    private readonly StateMachine _fsm;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Perception _perception;
    private readonly RobotStats _stats;
    private CombatNavigator _nav;

    // Re-path every quarter second so we react but don’t spam pathing
    private float _nextRepathTime;
    private const float RepathInterval = 0.25f;

    // Tunables (can move to stats later if you want)
    private const float HopDistance = 12f;   // how far one retreat hop aims
    private const int Samples = 10;          // cone samples
    private const float ConeHalfAngle = 55f; // degrees around escape dir

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
        _agent.autoBraking = false; // keep speed while hopping between retreat points
        // Reasonable speed: same as Chase multiplier (fast get-out)
        _agent.speed = _controller.GetEffectiveSpeed(5F);

        SetBestRetreatPoint();
        _nextRepathTime = Time.time + RepathInterval;
        Debug.Log($"{_controller.name} → Retreat");
    }

    public void Tick()
    {
        // If high-level goal changed, let the helper switch state
        var decision = _controller.GetDecision();
        if (decision.Move != MovementIntent.Retreat)
        {
            StateTransitionHelper.HandleTransition(_fsm, _controller);
            return;
        }

        if (Time.time >= _nextRepathTime && !_agent.pathPending)
        {
            SetBestRetreatPoint();
            _nextRepathTime = Time.time + RepathInterval;
        }
    }

    public void Exit() { }

    // ---------- Logic ----------

    private void SetBestRetreatPoint()
    {
        Vector3 origin = _controller.transform.position;
        List<RobotController> threats = _perception.GetEnemiesInRange();
        if (threats.Count == 0) { StateTransitionHelper.HandleTransition(_fsm, _controller); return; }

        Vector3 best = _nav.FindBestRetreatHop(origin, threats, HopDistance, Samples, ConeHalfAngle); 
        _agent.SetDestination(best);
    }

}
