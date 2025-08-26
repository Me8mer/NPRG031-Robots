using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// State where the robot chases a specific target (enemy or pickup)
/// until the decision layer requests a transition.
/// </summary>
public class ChaseState : IState
{
    private const float PathCooldown = 0.10f;
    private const float SpeedModifier = 1f;
    private float _nextPathTime;
    private bool _chasingPickup;

    private readonly StateMachine _stateMachine;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _target;
    private readonly CombatNavigator _nav;


    public ChaseState(StateMachine fsm, Transform target)
    {
        _stateMachine = fsm;
        _controller = fsm.Owner;
        _agent = _controller.GetAgent();
        _target = target;
        _nav = _controller.GetNavigator();
    }

    public void Enter()
    {
        if (_target == null)
        {
            StateTransitionHelper.HandleTransition(_stateMachine, _controller);
            return;
        }

        _controller.SetCurrentState(RobotState.Chase);
        _chasingPickup = _target.GetComponent<Pickup>() != null;

        _agent.isStopped = false;
        _agent.speed = _controller.GetEffectiveSpeed(SpeedModifier);
        _agent.stoppingDistance = 0.05f;
        _agent.autoBraking = false;
        _agent.acceleration = Mathf.Max(_agent.acceleration, 16f);

        if (_chasingPickup)
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                StateTransitionHelper.HandleTransition(_stateMachine, _controller);
                return;
            }
            _agent.stoppingDistance = 0f;
            _agent.autoBraking = true;
        }
        else
        {
            // Enemy: keep a slight buffer before attack ring
            float ring = _nav.ComputeAttackRing(_target, 5.25f);
            _agent.stoppingDistance = 0.05f;
            _agent.autoBraking = false;
        }

        _agent.SetDestination(GetChaseGoal());
        _nextPathTime = 0f;
        Debug.Log($"{_controller.name} → Chase {_target.name}");
    }

    public void Tick()
    {
        if (_target == null)
        {
            StateTransitionHelper.HandleTransition(_stateMachine, _controller);
            return;
        }


        var decision = _controller.GetDecision();
        bool stillChase =
            (decision.Move == MovementIntent.ChaseEnemy && decision.MoveEnemy?.transform == _target) ||
            (decision.Move == MovementIntent.ChasePickup && decision.MovePickup?.transform == _target);

        if (stillChase)
        {
            if (Time.time >= _nextPathTime && !_agent.pathPending)
            {
                if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
                    _agent.ResetPath();

                _agent.SetDestination(GetChaseGoal());
                _nextPathTime = Time.time + PathCooldown;
            }
        }

        StateTransitionHelper.HandleTransition(_stateMachine, _controller);
    }

    public void Exit()
    {

    }

    /// <summary>
    /// Picks a nearby valid NavMesh point around the target to chase.
    /// </summary>
    private Vector3 GetChaseGoal()
    {
        Vector3 goal = _target.position;

        // Enemy: aim slightly short of the target, not at the pivot
        if (!_chasingPickup)
        {
            Vector3 myPos = _controller.transform.position;
            Vector3 toTgt = _target.position - myPos; toTgt.y = 0f;

            if (toTgt.sqrMagnitude > 1e-4f)
            {
                float standOff = Mathf.Max(1.0f, _agent.radius + 0.25f);
                goal = _target.position - toTgt.normalized * standOff;
            }
        }

        // Sample a bit wider to avoid edge cases
        float sampleRadius = Mathf.Max(1.5f, _agent.radius + 0.75f);
        if (NavMesh.SamplePosition(goal, out var hit, sampleRadius, NavMesh.AllAreas))
            goal = hit.position;

        return goal;
    }
}
