/// <summary>
/// Contract that every AI state must implement so it can be managed by
/// <see cref="StateMachine"/>.
/// </summary>
public interface IState
{
    /// <summary>Called once when the state becomes active.</summary>
    void Enter();

    /// <summary>Called every frame while the state is active.</summary>
    void Tick();

    /// <summary>Called once just before the state is replaced by another.</summary>
    void Exit();
}
