using System.Collections.Generic;

/// <summary>
/// Static store for passing selected robots between menus and arena.
/// Lives across scene loads.
/// </summary>
public static class SelectedRobotsStore
{
    private static List<RobotBuildData> _selected;

    /// <summary>Stores selected robot builds (overwrites any existing selection).</summary>
    public static void Set(List<RobotBuildData> builds) => _selected = builds;

    /// <summary>Returns the currently stored builds (may be null).</summary>
    public static List<RobotBuildData> Get() => _selected;

    /// <summary>Clears the current selection.</summary>
    public static void Clear() => _selected = null;
}
