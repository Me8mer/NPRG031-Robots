using System.Collections.Generic;

public static class SelectedRobotsStore
{
    private static List<RobotBuildData> _selected;

    public static void Set(List<RobotBuildData> builds) => _selected = builds;
    public static List<RobotBuildData> Get() => _selected;
    public static void Clear() => _selected = null;
}
