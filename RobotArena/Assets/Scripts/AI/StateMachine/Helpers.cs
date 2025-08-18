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

        // Early exit if the current state already matches the desired objective
        switch (objective.Type)
        {
            case RobotObjectiveType.Idle:
                if (currentState == RobotState.Idle) return;
                break;
            case RobotObjectiveType.Retreat:
                if (currentState == RobotState.Retreat) return;
                break;
            case RobotObjectiveType.AttackEnemy:
                if (currentState == RobotState.Attack) return;
                break;
            //case RobotObjectiveType.ChaseEnemy:
            case RobotObjectiveType.ChaseEnemy:
                if (currentState == RobotState.Chase) return;
                break;
        }

        // Perform transitions
        switch (objective.Type)
        {
            case RobotObjectiveType.AttackEnemy:
                if (objective.TargetEnemy != null)
                    fsm.ChangeState(new AttackState(fsm, objective.TargetEnemy.transform));
                else
                    fsm.ChangeState(new IdleState(fsm));
                break;

            case RobotObjectiveType.SeekPickup:
                fsm.ChangeState(new ChaseState(fsm, objective.TargetPickup.transform));
                break;

            case RobotObjectiveType.ChaseEnemy:
                fsm.ChangeState(new ChaseState(fsm, objective.TargetEnemy.transform));
                break;

            case RobotObjectiveType.Retreat:
                // Pass the current closest threat if you have it. Null still works.
                var threat = objective.TargetEnemy != null ? objective.TargetEnemy.transform : null;
                fsm.ChangeState(new RetreatState(fsm));
                break;

            case RobotObjectiveType.Idle:
            default:
                fsm.ChangeState(new IdleState(fsm));
                break;
        }
    }
}
