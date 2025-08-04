/// Utility to handle global state transitions based on the current RobotObjective
/// </summary>
public static class StateTransitionHelper
{
    public static void HandleTransition(StateMachine fsm, RobotController controller)
    {
        var objective = controller.GetObjective();
        switch (objective.Type)
        {
            case RobotObjectiveType.AttackEnemy:
                fsm.ChangeState(new AttackState(fsm, objective.TargetEnemy));
                break;

            case RobotObjectiveType.SeekPickup:
                fsm.ChangeState(new ChaseState(fsm, objective.TargetPickup.transform));
                break;

            case RobotObjectiveType.ChaseEnemy:
                fsm.ChangeState(new ChaseState(fsm, objective.TargetEnemy.transform));
                break;

            case RobotObjectiveType.Retreat:
                fsm.ChangeState(new RetreatState(fsm));
                break;

            case RobotObjectiveType.Idle:
            default:
                fsm.ChangeState(new IdleState(fsm));
                break;
        }
    }
}
