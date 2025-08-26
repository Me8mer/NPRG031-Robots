using UnityEngine;

/// <summary>
/// Lightweight finite-state machine that owns a single
/// <see cref="RobotController"/> and manages transitions
/// between <see cref="IState"/>s.
/// </summary>
public class StateMachine
{
    private IState _current;
    private RobotController _owner;

    /// <summary>Injects the owner so states can access shared systems.</summary>
    public void SetOwner(RobotController owner) => _owner = owner;

    /// <summary>Initializes the FSM with the given starting state.</summary>
    public void Initialize(IState startingState)
    {
        _current = startingState;
        _current.Enter();
    }

    /// <summary>Executes one frame of logic on the current state.</summary>
    public void Tick()
    {
        _current?.Tick();
    }

    /// <summary>
    /// Switches to <paramref name="next"/> if it is different
    /// from the current state.
    /// </summary>
    public void ChangeState(IState next)
    {
        if (next == _current) return;

        _current?.Exit();
        _current = next;
        _current.Enter();
    }

    /// <summary>Owner robot that this FSM controls.</summary>
    public RobotController Owner => _owner;
}
