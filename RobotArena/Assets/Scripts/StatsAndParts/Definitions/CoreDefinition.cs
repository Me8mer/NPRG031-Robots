using UnityEngine;

/// <summary>
/// Defines the robot's *core* component.
/// Provides base armor and attack speed stats.
/// </summary>
[CreateAssetMenu(menuName = "RobotParts/Core")]
public class CoreDefinition : ScriptableObject
{
    [Tooltip("Unique identifier for this core. Used for serialization/build data.")]
    public string id;

    [Tooltip("Flat armor value added by this core.")]
    public int armor;

    [Tooltip("Base attack speed modifier provided by this core (shots per minute).")]
    public int attackSpeed;
}
