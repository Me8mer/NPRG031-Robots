using UnityEngine;


using UnityEngine.AI;


/// <summary>
/// Utility to handle global state transitions based on the current RobotObjective
/// </summary>
public static class StateTransitionHelper
{
    /// <summary>
    /// Transitions the FSM to match the controller's current objective,
    /// but avoids re-instantiating the same state type.
    /// </summary>
    public static void HandleTransition(StateMachine fsm, RobotController controller)
    {
        var objective = controller.GetObjective();
        var currentState = controller.CurrentState;

        switch (objective.Type)
        {
            case RobotObjectiveType.Idle:
                if (currentState == RobotState.Idle) return;
                fsm.ChangeState(new IdleState(fsm));
                return;

            case RobotObjectiveType.Retreat:
                if (currentState == RobotState.Retreat) return;
                fsm.ChangeState(new RetreatState(fsm));
                return;

            case RobotObjectiveType.AttackEnemy:
                if (currentState == RobotState.Attack) return;
                if (objective.TargetEnemy != null)
                {
                    fsm.ChangeState(new AttackState(fsm, objective.TargetEnemy.transform));
                }
                else
                {
                    fsm.ChangeState(new IdleState(fsm));
                }
                return;

            case RobotObjectiveType.SeekPickup:
                if (currentState == RobotState.Chase && objective.TargetPickup != null) return;
                if (objective.TargetPickup != null)
                {
                    fsm.ChangeState(new ChaseState(fsm, objective.TargetPickup.transform));
                }
                else
                {
                    fsm.ChangeState(new IdleState(fsm));
                }
                return;

            case RobotObjectiveType.ChaseEnemy:
                if (currentState == RobotState.Chase && objective.TargetEnemy != null) return;
                if (objective.TargetEnemy != null)
                {
                    fsm.ChangeState(new ChaseState(fsm, objective.TargetEnemy.transform));
                }
                else
                {
                    fsm.ChangeState(new IdleState(fsm));
                }
                return;

            default:
                fsm.ChangeState(new IdleState(fsm));
                return;
        }
    }

    public static class CombatHelpers
    {
        /// <summary>
        /// Center-to-center distance we want to hold when attacking.
        /// Based on OUR weapon range plus both agent radii and a small cushion.
        /// </summary>
        public static float ComputeAttackRing(RobotController self, Transform target, float cushion = 0.25f)
        {
            if (self == null || target == null) return 0.1f;

            float desired = Mathf.Max(0.1f, self.GetStats().attackRange);

            float myR = 0.5f;
            var myAgent = self.GetAgent();
            if (myAgent != null) myR = myAgent.radius;

            float theirR = 0.5f;
            var targetAgent = target.GetComponentInParent<NavMeshAgent>();
            if (targetAgent != null) theirR = targetAgent.radius;

            return desired + myR + theirR + Mathf.Max(0f, cushion);
        }

        /// <summary>
        /// Are we within the effective attack distance (ring + tolerance)?
        /// </summary>
        public static bool InEffectiveAttackRange(RobotController self, Transform target, float toleranceMeters = 0.5f)
        {
            float ring = ComputeAttackRing(self, target);
            float dist = Vector3.Distance(self.transform.position, target.position);
            return dist <= ring + Mathf.Max(0f, toleranceMeters);
        }
    }

}
