using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Movement transitions based on the controller's latest DecisionResult.
/// </summary>
public static class StateTransitionHelper
{
    public static void HandleTransition(StateMachine fsm, RobotController controller)
    {
        var decision = controller.GetDecision();           // <-- NEW
        var currentState = controller.CurrentState;

        switch (decision.Move)
        {
            case MovementIntent.Idle:
                if (currentState == RobotState.Idle) return;
                fsm.ChangeState(new IdleState(fsm));
                return;

            case MovementIntent.Retreat:
                if (currentState == RobotState.Retreat) return;
                fsm.ChangeState(new RetreatState(fsm));
                return;

            case MovementIntent.StrafeEnemy:
                // Keep it simple like before: if already in Attack (strafe), do not re-instantiate.
                if (currentState == RobotState.Strafe) return;
                if (decision.MoveEnemy != null)
                {
                    fsm.ChangeState(new StrafeState(fsm, decision.MoveEnemy.transform));
                }
                else
                {
                    fsm.ChangeState(new IdleState(fsm));
                }
                return;

            case MovementIntent.ChasePickup:
                // Only avoid re-instantiation if we are already in Chase and still have a pickup target
                if (currentState == RobotState.Chase && decision.MovePickup != null) return;
                if (decision.MovePickup != null)
                {
                    fsm.ChangeState(new ChaseState(fsm, decision.MovePickup.transform));
                }
                else
                {
                    fsm.ChangeState(new IdleState(fsm));
                }
                return;

            case MovementIntent.ChaseEnemy:
                if (currentState == RobotState.Chase && decision.MoveEnemy != null) return;
                if (decision.MoveEnemy != null)
                {
                    fsm.ChangeState(new ChaseState(fsm, decision.MoveEnemy.transform));
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
        public static float ComputeAttackRing(RobotController self, Transform target, float cushion = 0.25f)
        {
            if (self == null || target == null) return 0.1f;

            float desired = Mathf.Max(0.1f, self.GetAttackRangeMeters());

            float myR = 0.5f;
            var myAgent = self.GetAgent();
            if (myAgent != null) myR = myAgent.radius;

            float theirR = 0.5f;
            var targetAgent = target.GetComponentInParent<NavMeshAgent>();
            if (targetAgent != null) theirR = targetAgent.radius;

            return desired + myR + theirR + Mathf.Max(0f, cushion);
        }

        public static bool InEffectiveAttackRange(RobotController self, Transform target, float toleranceMeters = 0.5f)
        {
            float ring = ComputeAttackRing(self, target);
            float dist = Vector3.Distance(self.transform.position, target.position);
            return dist <= ring + Mathf.Max(0f, toleranceMeters);
        }
    }
}
