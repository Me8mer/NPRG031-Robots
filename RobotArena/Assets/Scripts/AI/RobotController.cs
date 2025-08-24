using System;
using UnityEngine;
using UnityEngine.AI;


// High level movement states used by the FSM.
public enum RobotState
{
    Idle,
    Chase,
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
    [SerializeField] private float lowerTurnSpeed;
    [Tooltip("Degrees per second for upper body yaw")]
    [SerializeField] private float upperTurnSpeed;
    [Tooltip("Angle tolerance to consider aim 'locked' for firing")]
    [SerializeField] private float aimToleranceDeg = 3f;


    [Header("Combat")]
    [SerializeField] private WeaponBase weapon;
    [Tooltip("Layers that can block shots")]
    [SerializeField] private LayerMask fireObstaclesMask = 0;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;
    [SerializeField] private bool controlLocked = false;

    [Header("Data")]
    public RobotStats stats;

    // Core subsystems
    private NavMeshAgent _agent;
    private StateMachine _stateMachine;
    private Perception _perception;
    private RobotHealth _health;
    private DecisionLayer _decision;
    private CombatNavigator _navigator;
    private TargetingSolver _targeting;

    private float _speedBoostMultiplier = 1f;
    private float _maxMoveSpeed;   // final, weight-adjusted max speed computed once


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

        if (stats == null) stats = new RobotStats();
        _navigator = new CombatNavigator(this);
        _targeting = new TargetingSolver(this);

        if (_agent != null) _agent.updateRotation = false;
        _targeting = new TargetingSolver(this);
        _navigator = new CombatNavigator(this);
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
        if (controlLocked) return;
        _lastDecision = _decision.Decide();

        // 2) Apply lower body facing from movement intent or agent velocity
        ApplyLowerBodyRotation();

        // 3) Aim upper body based on current fire target
        Vector3? aimPoint = ComputeAimPoint(_lastDecision);
        bool aimLocked = ApplyUpperBodyAiming(aimPoint);

        // 4) Handle firing regardless of current movement state, but only when properly gated
        HandleFiring(aimPoint, aimLocked);
        if (_agent) _agent.speed = GetEffectiveSpeed(GetStateSpeedModifier());

        // 5) Tick movement FSM. States will read GetDecision() and transition via helper
        _stateMachine.Tick();
    }
    #endregion

    #region Public API
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
    public LayerMask GetFireObstaclesMask()
    {
        // Same fallback rule as HandleFiring
        return (fireObstaclesMask.value != 0) ? fireObstaclesMask : _perception.obstacleMask;
    }
    public float GetEffectiveSpeed(float stateModifier)
    {
        float baseMax = _maxMoveSpeed > 0f
            ? _maxMoveSpeed
            : (stats.baseSpeed / Mathf.Max(0.001f, stats.weight));
        return baseMax * stateModifier * _speedBoostMultiplier;
    }
    public void ApplyTimedSpeedBoost(float percent, float seconds)
    {
        if (percent <= 0f) return;
        float mult = 1f + percent / 100f;
        StartCoroutine(SpeedBoostRoutine(mult, Mathf.Max(0f, seconds)));
    }
    private System.Collections.IEnumerator SpeedBoostRoutine(float mult, float seconds)
    {
        _speedBoostMultiplier *= mult;
        if (_agent) _agent.speed = GetEffectiveSpeed(GetStateSpeedModifier());
        yield return new WaitForSeconds(seconds);
        _speedBoostMultiplier /= mult;
        if (_agent) _agent.speed = GetEffectiveSpeed(GetStateSpeedModifier());
    }

    public void ApplyTimedDamageBoost(float percent, float seconds)
    {
        if (percent <= 0f) return;
        float mult = 1f + percent / 100f;
        stats.damage *= mult;
        StartCoroutine(DamageBoostRoutine(mult, Mathf.Max(0f, seconds)));
    }
    private float GetStateSpeedModifier()
    {
        switch (CurrentState)
        {
            case RobotState.Chase: return 1.5f;
            case RobotState.Retreat: return 5f;
            case RobotState.Strafe: return 1f;
            default: return 1f;
        }
    }

    private System.Collections.IEnumerator DamageBoostRoutine(float mult, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        stats.damage /= mult;
    }
    public float GetAttackRangeMeters()
    {
        if (weapon != null) return weapon.EffectiveRange;
        return stats.attackRange; // fallback
    }

    public NavMeshAgent GetAgent() => _agent;
    public Perception GetPerception() => _perception;
    public RobotStats GetStats()
    {
        if (stats == null) stats = new RobotStats();
        return stats;
    }
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
            lowerBody.rotation = Quaternion.RotateTowards(lowerBody.rotation, target, stats.turningSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Computes the current aim point above ground. Returns null when no valid target.
    /// </summary>
    private Vector3? ComputeAimPoint(DecisionResult decision)
    {
        return _targeting.AimPoint(decision.FireEnemy);
    }

    public void ApplyMovementFromStats()
    {
        // Bake weight into the max speed ONCE
        float baseSpeed = Mathf.Max(0f, stats.baseSpeed);
        float weight = Mathf.Max(0.001f, stats.weight);
        _maxMoveSpeed = baseSpeed / weight;

        // Push to NavMeshAgent so default movement uses this max
        if (_agent != null)
        {
            _agent.speed = _maxMoveSpeed;
            _agent.acceleration = Mathf.Max(_agent.acceleration, _maxMoveSpeed * 4f);
            _agent.angularSpeed = Mathf.Max(120f, stats.turningSpeed); // use your turning stat if it’s in deg/s
        }
    }



    /// <summary>
    /// Rotate the upper body toward the aim point, clamped within the allowed firing arc relative to lower body.
    /// Returns true if within aim tolerance.
    /// </summary>
    /// <summary>
    /// Rotate the upper body toward the aim point on the horizontal plane and pitch the muzzle to the true aim point.
    /// Returns true if the muzzle is within aim tolerance.
    /// </summary>
    private bool ApplyUpperBodyAiming(Vector3? aimPointOpt)
    {
        if (aimPointOpt == null || upperBody == null || firePoint == null) return false;
        Vector3 aimPoint = aimPointOpt.Value;

        // 1) Yaw turret (upper body) on the horizontal plane
        Vector3 flatDir = new Vector3(aimPoint.x, upperBody.position.y, aimPoint.z) - upperBody.position;
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion yawOnly = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            upperBody.rotation = Quaternion.RotateTowards(upperBody.rotation, yawOnly, stats.turningSpeed * Time.deltaTime);

        }

        // 2) Pitch muzzle to the real 3D aim point
        Vector3 trueDir = aimPoint - firePoint.position;
        if (trueDir.sqrMagnitude > 0.0001f)
        {
            firePoint.rotation = Quaternion.LookRotation(trueDir.normalized, Vector3.up);
        }

        // 3) Lock check: use muzzle alignment for accuracy
        float angleToTarget = Vector3.Angle(firePoint.forward, trueDir);
        bool locked = angleToTarget <= aimToleranceDeg;

        // Debug lines
        if (drawDebug)
        {
            Debug.DrawLine(firePoint.position, aimPoint, locked ? Color.cyan : Color.green, 0f, false);
            Debug.DrawRay(aimPoint, Vector3.up * 0.25f, Color.magenta, 0f, false);
        }
        return locked;
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
        if (!_targeting.HasLineOfFire(muzzle, aimPoint, obstacleMask))
        {
            if (drawDebug) Debug.DrawLine(muzzle, aimPoint, Color.red, 0f, false);
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
            //if (drawDebug) Debug.Log($"{name} attempted fire but weapon refused (TryFireAt returned false)");
        }
    }
    #endregion



    #region Helpers
    private static float SignedDeltaAngle(float fromYaw, float toYaw)
    {
        float delta = Mathf.DeltaAngle(fromYaw, toYaw);
        return delta;
    }


    public void WarpTo(Vector3 pos, Quaternion rot)
    {
        if (_agent) _agent.Warp(pos);
        transform.SetPositionAndRotation(pos, rot);
        if (lowerBody) lowerBody.rotation = rot;
        if (upperBody) upperBody.rotation = rot;
    }

    public void WireParts(Transform lower, Transform upper, Transform fire, WeaponBase wpn)
    {
        lowerBody = lower != null ? lower : transform;
        upperBody = upper != null ? upper : transform;
        firePoint = fire != null ? fire : upperBody;
        weapon = wpn;

        // Refresh cached weapon range now that we have a weapon
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
        if (!drawDebug) return;
        if (upperBody == null || lowerBody == null || firePoint == null) return;

        // Draw firing arc relative to lower body
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);

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




