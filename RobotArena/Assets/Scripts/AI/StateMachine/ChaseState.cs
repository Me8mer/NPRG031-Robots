using UnityEngine;
using UnityEngine.AI;
using static StateTransitionHelper;

/// <summary>
/// Pursues a specific target (enemy or pickup) until the decision layer requests a transition.
/// </summary>
public class ChaseState : IState
{
    private const float PathCooldown = 0.10f;
    private const float MinRepathDist = 0.5f;
    private float _nextPathTime;
    private bool _chasingPickup;
    private readonly StateMachine _stateMachine;
    private readonly RobotController _controller;
    private readonly NavMeshAgent _agent;
    private readonly Transform _target;
    private CombatNavigator _nav;
    private float _oldStoppingDistance;
    private bool _patchedStoppingDistance;

    /// <summary>
    /// Creates a new ChaseState that will follow <paramref name="target"/>.
    /// </summary>
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
        _chasingPickup = _target.GetComponent<Pickup>() != null;

        _agent.isStopped = false;
        _agent.speed = _controller.GetEffectiveSpeed(1.5F);
        _agent.stoppingDistance = 0.05f;   // small cushion
        _agent.autoBraking = false;        // do not full-stop at the point
        _agent.acceleration = Mathf.Max(_agent.acceleration, 16f); // snappier restart

        if (_chasingPickup)
        {
            // If we were chasing a pickup and it just got consumed, bail immediately.
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                StateTransitionHelper.HandleTransition(_stateMachine, _controller);
                return;
            }
            _agent.stoppingDistance = 0f;
            _agent.autoBraking = true;   // precise stop at pickup
        }
        else
        {
            float ring = _nav.ComputeAttackRing(_target, 5.25f);
            _agent.stoppingDistance = 0.05f;
            _agent.autoBraking = false;   // gentle braking before ring
        }

        _agent.SetDestination(_target.position);
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
            // DecisionLayer will flip us to Strafe when LOF is true.
            if (Time.time >= _nextPathTime && !_agent.pathPending)
            {
                Vector3 goal = GetChaseGoal();
                if (Vector3.Distance(_agent.destination, goal) > MinRepathDist)
                {
                    _agent.SetDestination(goal);
                    _nextPathTime = Time.time + PathCooldown;
                }
            }
        }

        StateTransitionHelper.HandleTransition(_stateMachine, _controller);
    }


    public void Exit()
    {
        if (_agent && _patchedStoppingDistance)
        {
            _agent.stoppingDistance = _oldStoppingDistance; // restore
            _patchedStoppingDistance = false;
        }
    }

    private Vector3 GetChaseGoal()
    {
        // Start from the enemy pivot
        Vector3 goal = _target.position;

        // Sample a reachable NavMesh point near them.
        // Radius: just a bit larger than our agent to avoid hugging the wall edge.
        float sampleRadius = _agent ? Mathf.Max(1.0f, _agent.radius + 0.5f) : 1.0f;

        if (UnityEngine.AI.NavMesh.SamplePosition(goal, out var hit, sampleRadius, UnityEngine.AI.NavMesh.AllAreas))
            goal = hit.position;

        return goal;
    }
}
