using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// High level movement states used by the FSM.
/// These values are set by states (Chase, Strafe, Retreat, Idle) and used
/// to apply speed modifiers and help debug robot behavior.
/// </summary>
public enum RobotState
{
    Idle,
    Chase,
    Retreat,
    Strafe
}

/// <summary>
/// Central hub that wires together:
/// - Navigation (<see cref="NavMeshAgent"/>)
/// - Perception (enemy/pickup awareness)
/// - Health
/// - Decision layer (AI brain)
/// - FSM (movement states)
/// - Weapon targeting & firing
/// 
/// Movement is delegated to the FSM states.
/// Firing is handled here every frame, independent of the current movement state.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Perception))]
[RequireComponent(typeof(RobotHealth))]
public class RobotController : MonoBehaviour
{
    [Header("Body Parts")]
    [Tooltip("Transform representing the rotating base / chassis (lower body).")]
    [SerializeField] private Transform lowerBody;
    [Tooltip("Transform representing the rotating turret / upper body.")]
    [SerializeField] private Transform upperBody;
    [Tooltip("Point where projectiles spawn from (usually child of turret).")]
    [SerializeField] private Transform firePoint;

    [Header("Rotation and Aim")]
    [Tooltip("Degrees per second for lower body yaw (movement facing).")]
    [SerializeField] private float lowerTurnSpeed;
    [Tooltip("Degrees per second for upper body yaw (turret facing).")]
    [SerializeField] private float upperTurnSpeed;
    [Tooltip("Angle tolerance in degrees to consider aim 'locked' for firing.")]
    [SerializeField] private float aimToleranceDeg = 3f;

    [Header("Combat")]
    [Tooltip("Weapon assigned to this robot (handles projectile spawning).")]
    [SerializeField] private WeaponBase weapon;
    [Tooltip("Layers that can block shots (overrides Perception.obstacleMask if set).")]
    [SerializeField] private LayerMask fireObstaclesMask = 0;

    [Header("Debug")]
    [Tooltip("If true, draw debug lines/rays for aiming & firing.")]
    [SerializeField] private bool drawDebug = true;
    [Tooltip("If true, disables all decision + FSM updates (used for death or pause).")]
    [SerializeField] private bool controlLocked = false;

    [Header("Data")]
    [Tooltip("Stats for this robot (health, speed, damage, etc.). Usually filled by RobotStatsBuilder.")]
    public RobotStats stats;

    // --- Core subsystems ---
    private NavMeshAgent _agent;
    private StateMachine _stateMachine;
    private Perception _perception;
    private RobotHealth _health;
    private DecisionLayer _decision;
    private CombatNavigator _navigator;
    private TargetingSolver _targeting;

    // --- Temporary modifiers (pickups, buffs) ---
    private float _speedBoostMultiplier = 1f;
    private float _damageBoostMultiplier = 1f;

    private float _maxMoveSpeed;   // Cached max speed (base stats adjusted by weight)

    /// <summary>Current FSM movement state.</summary>
    public RobotState CurrentState { get; private set; } = RobotState.Idle;

    /// <summary>Latest decision from AI/player decision layer.</summary>
    private DecisionResult _lastDecision;

    // Cache: effective weapon range (prefab override > stats)
    private float _weaponRange;

    #region Unity Lifecycle
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _perception = GetComponent<Perception>();
        _health = GetComponent<RobotHealth>();

        if (stats == null) stats = new RobotStats();
        _navigator = new CombatNavigator(this);
        _targeting = new TargetingSolver(this);

        if (_agent != null) _agent.updateRotation = false; // rotation handled manually

        _weaponRange = weapon != null ? weapon.EffectiveRange : stats.attackRange;
    }

    private void Start()
    {
        _decision = new PlayerDecisionLayer(this);
        _health.OnDeath += HandleDeath;

        // Initialize FSM with Idle state
        _stateMachine = new StateMachine();
        _stateMachine.SetOwner(this);
        _stateMachine.Initialize(new IdleState(_stateMachine));

        // Fallback wiring if inspector not set
        if (lowerBody == null) lowerBody = transform;
        if (upperBody == null) upperBody = transform;
        if (firePoint == null) firePoint = upperBody;
    }

    private void Update()
    {
        if (controlLocked) return;

        // 1) Ask decision layer for this frame's intent (move + fire target)
        _lastDecision = _decision.Decide();

        // 2) Rotate lower body toward velocity vector
        ApplyLowerBodyRotation();

        // 3) Aim upper body + muzzle at fire target (if any)
        Vector3? aimPoint = ComputeAimPoint(_lastDecision);
        bool aimLocked = ApplyUpperBodyAiming(aimPoint);

        // 4) Handle firing if aim is locked and weapon ready
        HandleFiring(aimPoint, aimLocked);

        // 5) Continuously update speed from stats + modifiers + state
        if (_agent) _agent.speed = GetEffectiveSpeed(GetStateSpeedModifier());

        // 6) Advance movement FSM
        _stateMachine.Tick();
    }
    #endregion

    #region Public API
    /// <summary>Locks or unlocks control (used for pause/death).</summary>
    public void SetControlLocked(bool locked)
    {
        controlLocked = locked;
        if (_agent)
        {
            _agent.isStopped = locked;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }
    }

    public void SetCurrentState(RobotState st) => CurrentState = st;
    public Transform GetFirePointTransform() => firePoint != null ? firePoint : upperBody;
    public TargetingSolver GetTargeting() => _targeting;
    public CombatNavigator GetNavigator() => _navigator;

    /// <summary>
    /// Returns mask of layers that can block fire.  
    /// Uses local override if set, otherwise Perception.obstacleMask.
    /// </summary>
    public LayerMask GetFireObstaclesMask() =>
        (fireObstaclesMask.value != 0) ? fireObstaclesMask : _perception.obstacleMask;

    /// <summary>
    /// Effective movement speed for current state (base stats × multipliers).
    /// </summary>
    public float GetEffectiveSpeed(float stateModifier)
    {
        if (_maxMoveSpeed <= 0f) _maxMoveSpeed = stats.RequireBakedSpeed();
        return _maxMoveSpeed * Mathf.Max(0f, stateModifier) * _speedBoostMultiplier;
    }

    /// <summary>Effective damage output (base × multipliers).</summary>
    public float GetEffectiveDamage() => stats.damage * _damageBoostMultiplier;

    /// <summary>Temporarily increases movement speed (percent boost, for seconds).</summary>
    public void ApplyTimedSpeedBoost(float percent, float seconds)
    {
        if (percent <= 0f) return;
        float mult = 1f + percent / 100f;
        StartCoroutine(SpeedBoostRoutine(mult, Mathf.Max(0f, seconds)));
    }

    /// <summary>Temporarily increases damage (percent boost, for seconds).</summary>
    public void ApplyTimedDamageBoost(float percent, float seconds)
    {
        if (percent <= 0f) return;
        float mult = 1f + percent / 100f;
        StartCoroutine(DamageBoostRoutine(mult, Mathf.Max(0f, seconds)));
    }

    /// <summary>Effective attack range (weapon override > stats).</summary>
    public float GetAttackRangeMeters() =>
        weapon != null ? weapon.EffectiveRange : stats.attackRange;

    public NavMeshAgent GetAgent() => _agent;
    public Perception GetPerception() => _perception;
    public RobotHealth GetHealth() => _health;

    /// <summary>Returns current stats (ensures non-null).</summary>
    public RobotStats GetStats()
    {
        if (stats == null) stats = new RobotStats();
        return stats;
    }

    /// <summary>Latest decision made by AI/player layer.</summary>
    public DecisionResult GetDecision() => _lastDecision;
    #endregion

    #region Lower / Upper Body Rotation
    /// <summary>
    /// Rotates lower body to face movement direction (NavMeshAgent velocity).
    /// </summary>
    private void ApplyLowerBodyRotation()
    {
        Vector3 vel = _agent.desiredVelocity;
        vel.y = 0f;

        if (vel.sqrMagnitude > 0.001f)
        {
            Quaternion target = Quaternion.LookRotation(vel.normalized, Vector3.up);
            lowerBody.rotation = Quaternion.RotateTowards(
                lowerBody.rotation, target, stats.turningSpeed * Time.deltaTime);
        }
    }

    /// <summary>Computes current aim point from decision's fire target.</summary>
    private Vector3? ComputeAimPoint(DecisionResult decision) =>
        _targeting.AimPoint(decision.FireEnemy);

    /// <summary>
    /// Rotates turret (upper body) toward aim point and pitches muzzle.
    /// Returns true if within <see cref="aimToleranceDeg"/> for firing.
    /// </summary>
    private bool ApplyUpperBodyAiming(Vector3? aimPointOpt)
    {
        if (aimPointOpt == null || upperBody == null || firePoint == null) return false;
        Vector3 aimPoint = aimPointOpt.Value;

        // --- Turret yaw ---
        Vector3 flatDir = new Vector3(aimPoint.x, upperBody.position.y, aimPoint.z) - upperBody.position;
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion yawOnly = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            upperBody.rotation = Quaternion.RotateTowards(
                upperBody.rotation, yawOnly, stats.turningSpeed * Time.deltaTime);
        }

        // --- Muzzle pitch ---
        Vector3 trueDir = aimPoint - firePoint.position;
        if (trueDir.sqrMagnitude > 0.0001f)
            firePoint.rotation = Quaternion.LookRotation(trueDir.normalized, Vector3.up);

        // Aim lock check
        float angleToTarget = Vector3.Angle(firePoint.forward, trueDir);
        bool locked = angleToTarget <= aimToleranceDeg;

        if (drawDebug)
        {
            Debug.DrawLine(firePoint.position, aimPoint, locked ? Color.cyan : Color.green, 0f);
            Debug.DrawRay(aimPoint, Vector3.up * 0.25f, Color.magenta, 0f);
        }
        return locked;
    }

    /// <summary>
    /// Bakes weight-adjusted speed into NavMeshAgent and updates acceleration/turn rates.
    /// Call after stats are recomputed (e.g., when assembling robot).
    /// </summary>
    public void ApplyMovementFromStats()
    {
        _maxMoveSpeed = stats.RequireBakedSpeed();
        if (_agent != null)
        {
            _agent.speed = _maxMoveSpeed;
            _agent.acceleration = Mathf.Max(_agent.acceleration, _maxMoveSpeed * 4f);
            _agent.angularSpeed = Mathf.Max(120f, stats.turningSpeed);
        }
    }
    #endregion

    #region Buff Coroutines
    private System.Collections.IEnumerator SpeedBoostRoutine(float mult, float seconds)
    {
        _speedBoostMultiplier *= mult;
        if (_agent) _agent.speed = GetEffectiveSpeed(GetStateSpeedModifier());
        yield return new WaitForSeconds(seconds);
        _speedBoostMultiplier /= mult;
        if (_agent) _agent.speed = GetEffectiveSpeed(GetStateSpeedModifier());
    }

    private System.Collections.IEnumerator DamageBoostRoutine(float mult, float seconds)
    {
        _damageBoostMultiplier *= mult;
        yield return new WaitForSeconds(seconds);
        _damageBoostMultiplier /= mult;
    }

    /// <summary>State-specific movement speed multipliers.</summary>
    private float GetStateSpeedModifier()
    {
        return CurrentState switch
        {
            RobotState.Chase => 1.5f,
            RobotState.Retreat => 5f,
            RobotState.Strafe => 1f,
            _ => 1f
        };
    }
    #endregion

    #region Firing
    /// <summary>
    /// Attempts to fire at <paramref name="aimPointOpt"/> if:
    /// - weapon exists and ready
    /// - aim is locked
    /// - target is in range and unobstructed
    /// </summary>
    private void HandleFiring(Vector3? aimPointOpt, bool aimLocked)
    {
        if (weapon == null || aimPointOpt == null || !weapon.CanFire || !aimLocked) return;

        Vector3 muzzle = firePoint != null ? firePoint.position : upperBody.position;
        Vector3 aimPoint = aimPointOpt.Value;
        float distance = Vector3.Distance(muzzle, aimPoint);
        if (distance > _weaponRange) return;

        LayerMask obstacleMask = fireObstaclesMask.value != 0 ? fireObstaclesMask : _perception.obstacleMask;
        if (!_targeting.HasLineOfFire(muzzle, aimPoint, obstacleMask))
        {
            if (drawDebug) Debug.DrawLine(muzzle, aimPoint, Color.red, 0f);
            return;
        }

        if (weapon.TryFireAt(aimPoint))
        {
            if (drawDebug)
                Debug.Log($"{name} fired projectile at {(_lastDecision.FireEnemy != null ? _lastDecision.FireEnemy.name : "unknown")}");
            Debug.DrawLine(muzzle, aimPoint, Color.cyan, 0f);
        }
    }
    #endregion

    #region Helpers
    /// <summary>Teleports robot to a position+rotation (resets NavMeshAgent + body rotations).</summary>
    public void WarpTo(Vector3 pos, Quaternion rot)
    {
        if (_agent) _agent.Warp(pos);
        transform.SetPositionAndRotation(pos, rot);
        if (lowerBody) lowerBody.rotation = rot;
        if (upperBody) upperBody.rotation = rot;
    }

    /// <summary>Assigns body part transforms and weapon, refreshing cached weapon range.</summary>
    public void WireParts(Transform lower, Transform upper, Transform fire, WeaponBase wpn)
    {
        lowerBody = lower != null ? lower : transform;
        upperBody = upper != null ? upper : transform;
        firePoint = fire != null ? fire : upperBody;
        weapon = wpn;
        _weaponRange = weapon != null ? weapon.EffectiveRange : stats.attackRange;
    }
    #endregion

    #region Lifecycle
    private void HandleDeath()
    {
        if (_agent != null) _agent.isStopped = true;
        OnDestroy();
    }

    private void OnDestroy()
    {
        StateTransitionHelper.Forget(this);
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!drawDebug || upperBody == null || lowerBody == null || firePoint == null) return;

        // Show turret facing line
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(upperBody.position, upperBody.position + upperBody.forward * 2.5f);

        // Show current fire target aim line
        if (_lastDecision.FireEnemy != null)
        {
            var ap = _targeting != null ? _targeting.AimPoint(_lastDecision.FireEnemy) : null;
            if (ap.HasValue)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(firePoint.position, ap.Value);
            }
        }
    }
    #endregion
}
