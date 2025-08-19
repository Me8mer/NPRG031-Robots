using System;
using UnityEngine;
using UnityEngine.AI;


// High level movement states used by the FSM.
public enum RobotState
{
    Idle,
    Chase,
    Attack,   // currently used by StrafeState
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
    [Header("Body Parts")]
    [Tooltip("Transform that represents the rotating base / chassis")]
    [SerializeField] private Transform lowerBody;
    [Tooltip("Transform that represents the rotating turret / upper body")]
    [SerializeField] private Transform upperBody;
    [Tooltip("Point where projectiles spawn from")]
    [SerializeField] private Transform firePoint;

    [Header("Rotation and Aim")]
    [Tooltip("Degrees per second for lower body yaw")]
    [SerializeField] private float lowerTurnSpeed = 180f;
    [Tooltip("Degrees per second for upper body yaw")]
    [SerializeField] private float upperTurnSpeed = 360f;
    [Tooltip("Max signed yaw offset the upper body may rotate relative to lower body")]
    [SerializeField] private float maxUpperYawFromLower = 80f;
    [Tooltip("Angle tolerance to consider aim 'locked' for firing")]
    [SerializeField] private float aimToleranceDeg = 3f;
    [Tooltip("If the upper yaw exceeds this, request lower body to help re-center")]
    [SerializeField] private float reCenterThresholdDeg = 60f;

    [Header("Combat")]
    [SerializeField] private WeaponBase weapon;
    [Tooltip("Layers that can block shots")]
    [SerializeField] private LayerMask fireObstaclesMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    [Header("Data")]
    public RobotStats stats;

    // Core subsystems
    private NavMeshAgent _agent;
    private StateMachine _stateMachine;
    private Perception _perception;
    private RobotHealth _health;
    private DecisionLayer _decision;


    public RobotState CurrentState { get; private set; } = RobotState.Idle;

    // Latest decision from the decision layer (movement + firing).
    private DecisionResult _lastDecision;

    // Cached values
    private float _weaponRange;

    #region Unity
    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _perception = GetComponent<Perception>();
        _health = GetComponent<RobotHealth>();

        // Let us control lower body rotation manually for consistent separation of concerns
        _agent.updateRotation = false;
        _weaponRange = weapon != null ? weapon.EffectiveRange : stats.attackRange;
    }

    void Start()
    {
        _decision = new PlayerDecisionLayer(this);

        _health.OnDeath += HandleDeath;

        _stateMachine = new StateMachine();
        _stateMachine.SetOwner(this);
        _stateMachine.Initialize(new IdleState(_stateMachine));

        // Fallback if body parts were not wired in the inspector
        if (lowerBody == null) lowerBody = transform;
        if (upperBody == null) upperBody = transform;
        if (firePoint == null) firePoint = upperBody;
    }

    void Update()
    {
        _lastDecision = _decision.Decide();

        // 2) Apply lower body facing from movement intent or agent velocity
        ApplyLowerBodyRotation();

        // 3) Aim upper body based on current fire target
        Vector3? aimPoint = ComputeAimPoint(_lastDecision);
        bool aimLocked = ApplyUpperBodyAiming(aimPoint);

        // 4) Handle firing regardless of current movement state, but only when properly gated
        HandleFiring(aimPoint, aimLocked);

        // 5) Tick movement FSM. States will read GetDecision() and transition via helper
        _stateMachine.Tick();
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

    public NavMeshAgent GetAgent() => _agent;
    public Perception GetPerception() => _perception;
    public RobotStats GetStats() => stats;
    public RobotHealth GetHealth() => _health;

    // New accessor for helpers and states
    public DecisionResult GetDecision() => _lastDecision;
    #endregion

    #region Lower / Upper application
    private void ApplyLowerBodyRotation()
    {
        // Desired facing: if the agent is moving, face its velocity.
        // Later, we can allow DecisionLayer to provide an explicit facing for strafing.
        Vector3 vel = _agent.desiredVelocity;
        vel.y = 0f;

        if (vel.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(vel.normalized, Vector3.up);
            lowerBody.rotation = Quaternion.RotateTowards(lowerBody.rotation, target, lowerTurnSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Computes the current aim point, usually the enemy chest. Returns null when no valid target.
    /// </summary>
    private Vector3? ComputeAimPoint(DecisionResult decision)
    {
        var enemy = decision.FireEnemy;
        if (enemy == null) return null;

        Vector3 p = enemy.transform.position + Vector3.up * 0.5f;
        return p;
    }

    /// <summary>
    /// Rotate the upper body toward the aim point, clamped within the allowed firing arc relative to lower body.
    /// Returns true if within aim tolerance.
    /// </summary>
    private bool ApplyUpperBodyAiming(Vector3? aimPointOpt)
    {
        if (aimPointOpt == null) return false;

        Vector3 aimPoint = aimPointOpt.Value;

        // Desired yaw direction on the horizontal plane
        Vector3 toAim = aimPoint - upperBody.position;
        toAim.y = 0f;
        if (toAim.sqrMagnitude < 0.0001f) return false;

        Quaternion desiredUpper = Quaternion.LookRotation(toAim.normalized, Vector3.up);

        // Free 360° turret rotation
        upperBody.rotation = Quaternion.RotateTowards(upperBody.rotation, desiredUpper, upperTurnSpeed * Time.deltaTime);

        // Aim lock check against the desired yaw
        float currentDelta = Mathf.Abs(SignedDeltaAngle(upperBody.eulerAngles.y, desiredUpper.eulerAngles.y));
        return currentDelta <= aimToleranceDeg;
    }
    #endregion

    #region Firing
    private void HandleFiring(Vector3? aimPointOpt, bool aimLocked)
    {
        if (weapon == null)
        {
            //if (drawDebug) Debug.Log($"{name} skip fire: no weapon assigned");
            return;
        }

        if (aimPointOpt == null)
        {
            //if (drawDebug) Debug.Log($"{name} skip fire: no fire target (DecisionLayer returned null)");
            return;
        }
        // Cooldown
        if (!weapon.CanFire)
        {
            //if (drawDebug) Debug.Log($"{name} skip fire: weapon cooling down");
            return;
        }

        // Aim lock
        if (!aimLocked)
        {
            //if (drawDebug) Debug.Log($"{name} skip fire: aim not locked (turret still rotating)");
            return;
        }

        // Range gate
        Vector3 muzzle = firePoint != null ? firePoint.position : upperBody.position;
        Vector3 aimPoint = aimPointOpt.Value;
        float distance = Vector3.Distance(muzzle, aimPoint);
        if (distance > _weaponRange)
        {
            //if (drawDebug) Debug.Log($"{name} skip fire: target too far ({distance:F1}m > range {_weaponRange:F1}m)");
            return;
        }

        // Build an obstacle-only mask. Fallback to Perception's obstacleMask if our local one is empty.
        LayerMask obstacleMask = fireObstaclesMask.value != 0 ? fireObstaclesMask : _perception.obstacleMask;
        // Line of fire gate: check ONLY obstacles. Do not include Robots.
        Vector3 dir = (aimPoint - muzzle).normalized;
        float rayLen = distance;

        // Optional: draw the intent line
        if (drawDebug) Debug.DrawLine(muzzle, aimPoint, Color.green, 0f, false);

        // If any obstacle is between muzzle and target, block
        if (Physics.Raycast(muzzle, dir, out RaycastHit hit, rayLen, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebug)
            {
                Debug.DrawLine(muzzle, hit.point, Color.red, 0f, false);
                //Debug.Log($"{name} skip fire: LOS blocked by {hit.collider.name} (layer {hit.collider.gameObject.layer})");
            }
            return;
        }
        // Fire!
        if (weapon.TryFireAt(aimPoint))
        {
            if (drawDebug) Debug.Log($"{name} fired projectile at {(_lastDecision.FireEnemy != null ? _lastDecision.FireEnemy.name : "unknown")}");
            Debug.DrawLine(muzzle, aimPoint, Color.cyan, 0f, false);
        }
        else
        {
            if (drawDebug) Debug.Log($"{name} attempted fire but weapon refused (TryFireAt returned false)");
        }
    }
    #endregion



    #region Helpers
    private static float SignedDeltaAngle(float fromYaw, float toYaw)
    {
        float delta = Mathf.DeltaAngle(fromYaw, toYaw);
        return delta;
    }
    #endregion

    #region Lifecycle
    private void HandleDeath()
    {
        if (_agent != null) _agent.isStopped = true;
        OnDestroy();
        // Optional: effects, UI, cleanup
    }

    private void OnDestroy()
    {
        StateTransitionHelper.Forget(this);
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        if (upperBody == null || lowerBody == null || firePoint == null) return;

        // Draw firing arc relative to lower body
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        DrawArc(lowerBody.position, lowerBody.forward, maxUpperYawFromLower, 2.0f);

        // Draw current upper forward and re-center band
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(upperBody.position, upperBody.position + upperBody.forward * 2.5f);

        // Draw aim line
        if (_lastDecision.FireEnemy != null)
        {

            Vector3 aimPoint = _lastDecision.FireEnemy.transform.position + Vector3.up * 0.5f;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(firePoint.position, aimPoint);
        }
    }

    private void DrawArc(Vector3 origin, Vector3 forward, float halfAngle, float radius)
    {
        int steps = 24;
        float start = -halfAngle;
        float end = halfAngle;
        Vector3 prev = origin + Quaternion.Euler(0f, start, 0f) * forward * radius;
        for (int i = 1; i <= steps; i++)
        {
            float t = Mathf.Lerp(start, end, i / (float)steps);
            Vector3 next = origin + Quaternion.Euler(0f, t, 0f) * forward * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        // Base forward
        Gizmos.DrawLine(origin, origin + forward * radius);
    }
    #endregion
}




