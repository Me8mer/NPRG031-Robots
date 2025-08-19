using System;
using UnityEngine;
using UnityEngine.AI;


// High level movement states used by the FSM.
public enum RobotState
{
    Idle,
    Chase,
    Attack,   // currently used by StrafeState. You can rename later if you prefer.
    Retreat,
    Strafe
}


/// <summary>
/// Central hub that wires NavMeshAgent, Perception, Health, DecisionLayer and FSM.
/// Movement is handled by states. Firing is handled here every frame,
/// independent of the movement state.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Perception))]
[RequireComponent(typeof(RobotHealth))]
public class RobotController : MonoBehaviour
{


    [Header("Data")]
    public RobotStats stats;

    private NavMeshAgent agent;
    private StateMachine stateMachine;
    private Perception perception;
    private RobotHealth health;
    private DecisionLayer decision;

    public RobotState CurrentState { get; private set; } = RobotState.Idle;

    // Latest decision from the decision layer (movement + firing).
    private DecisionResult _lastDecision;

    [Header("Combat")]
    [SerializeField] private WeaponBase weapon;


    #region Unity
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        perception = GetComponent<Perception>();
        health = GetComponent<RobotHealth>();
    }

    void Start()
    {
        decision = new PlayerDecisionLayer(this);

        health.OnDeath += HandleDeath;

        stateMachine = new StateMachine();
        stateMachine.SetOwner(this);
        stateMachine.Initialize(new IdleState(stateMachine));
    }

    void Update()
    {
        // 1) Decide once per frame
        _lastDecision = decision.Decide();

        // 2) Handle firing regardless of current movement state
        HandleFiring(_lastDecision);

        // 3) Let the movement FSM tick. States will read GetDecision() and transition via helper.
        stateMachine.Tick();
    }
    #endregion

    #region Public API
    public void SetCurrentState(RobotState st) => CurrentState = st;

    public float GetEffectiveSpeed(float stateModifier)
    {
        float speedAfterWeight = stats.baseSpeed / Mathf.Max(0.001f, stats.weight);
        return speedAfterWeight * stateModifier;
    }

    public float GetAttackRangeMeters()
    {
        if (weapon != null) return weapon.EffectiveRange;
        return stats.attackRange; // fallback
    }

    public NavMeshAgent GetAgent() => agent;
    public Perception GetPerception() => perception;
    public RobotStats GetStats() => stats;
    public RobotHealth GetHealth() => health;

    // New accessor for helpers and states
    public DecisionResult GetDecision() => _lastDecision;
    #endregion

    #region Private
    private void HandleDeath()
    {
        if (agent != null) agent.isStopped = true;
        // Optional: effects, UI, cleanup
    }

    private void HandleFiring(DecisionResult decision)
    {
        var enemy = decision.FireEnemy;
        if (enemy == null || weapon == null) return;

        // simple aim point at chest height so shots are less likely to hit the floor
        Vector3 aimPoint = enemy.transform.position + Vector3.up * 0.5f;

        // WeaponController will do fire rate and range checks inside


        if (weapon.TryFireAt(aimPoint))
        {
            Debug.Log($"{name} fired projectile at {enemy.name}");
        }

            // Optional debug line so you see when AI intends to fire
            Debug.DrawLine(transform.position + Vector3.up * 0.5f, aimPoint, Color.cyan, 0f, false);
    }

    #endregion




}
