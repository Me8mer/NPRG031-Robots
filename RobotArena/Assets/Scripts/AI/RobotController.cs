using System;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

/// TODO Put into own file??????
/// <summary>
/// High‑level finite‑state‑machine identifiers shared by AI,
/// perception and health subsystems.
/// </summary>
public enum RobotState
{
    /// <summary>Standing still, regenerating at full rate.</summary>
    Idle,
    /// <summary>Path‑finding toward a visible enemy target.</summary>
    Chase,
    /// <summary>Firing the active weapon at a target in range.</summary>
    Attack,
    /// <summary>Retreating to cover or a flee point when fragile.</summary>
    Retreat
}

/// <summary>
/// Central hub MonoBehaviour that wires together NavMeshAgent,
/// perception, health and the finite‑state machine. One instance
/// lives on each robot prefab at runtime.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Perception))]
[RequireComponent(typeof(RobotHealth))]
public class RobotController : MonoBehaviour
{
    /// <summary>Design‑time stats assigned via Inspector.</summary>
    [Header("Data")]
    public RobotStats stats;

    private NavMeshAgent agent;
    private StateMachine stateMachine;
    private Perception perception;
    private RobotHealth health;
    private DecisionLayer decision;
    /// <summary>Currently active high‑level state.</summary>
    public RobotState CurrentState { get; private set; } = RobotState.Idle;

    [SerializeField] private bool isPlayer = false;

    #region Unity Callbacks
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        perception = GetComponent<Perception>();
        health = GetComponent<RobotHealth>();
    }

    void Start()
    {
        decision = isPlayer
            ? new PlayerDecisionLayer(this)
            : new EnemyDecisionLayer(this);

        stateMachine = new StateMachine();
        stateMachine.SetOwner(this);
        stateMachine.Initialize(new IdleState(stateMachine));
    }

    void Update()
    {
        stateMachine.Tick();
    }
    #endregion

    #region FSM Helpers
    /// <summary>
    /// Called by each concrete state on <c>Enter()</c> so controller knows what state is active.
    /// </summary>
    public void SetCurrentState(RobotState st) => CurrentState = st;

    /// <summary>
    /// Calculates effective movement speed based on weight and a
    /// caller provided state multiplier.
    /// </summary>
    /// <param name="stateModifier">Multiplier from <see cref="RobotStats"/>.</param>
    public float GetEffectiveSpeed(float stateModifier)
    {
        float speedAfterWeight = stats.baseSpeed / stats.weight;
        return speedAfterWeight * stateModifier;
    }

    // Shorthand accessors so other scripts remain decoupled from field names.
    public NavMeshAgent GetAgent() => agent;
    public Perception GetPerception() => perception;
    public RobotStats GetStats() => stats;
    public RobotHealth GetHealth() => health;
    public RobotObjective GetObjective() => decision.Decide();
    #endregion
}
