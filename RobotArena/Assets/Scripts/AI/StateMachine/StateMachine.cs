using UnityEngine;
/// <summary>
/// Lightweight finite‑state‑machine that owns a single
/// <see cref="RobotController"/> and swaps <see cref="IState"/>s
/// on demand.
/// </summary>
public class StateMachine
{
    private IState _current;
    private RobotController _owner;

    /// <summary>Injects the owner so states can reach shared systems.</summary>
    public void SetOwner(RobotController owner) => _owner = owner;

    /// <summary>Initializes the FSM with <paramref name="startingState"/>.</summary>
    public void Initialize(IState startingState)
    {
        _current = startingState;
        _current.Enter();
    }

    /// <summary>Delegates one frame of logic to the current state.</summary>
    public void Tick()
    {
        _current?.Tick();
    }

    /// <summary>
    /// Switches to <paramref name="next"/> if it is different from
    /// the current state.
    /// </summary>
    public void ChangeState(IState next)
    {
        if (next == _current) return;

        _current?.Exit();
        _current = next;
        _current.Enter();
    }

    // <summary>Helper for states that need the RobotController.<summary>
    public RobotController Owner => _owner;
}

