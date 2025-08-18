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

}
