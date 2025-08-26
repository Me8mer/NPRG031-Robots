using UnityEngine;

/// <summary>
/// Defines the robot's *lower body* (legs, wheels, tracks).
/// Governs speed, mobility, and turning ability.
/// </summary>
[CreateAssetMenu(menuName = "RobotParts/Lower")]
public class LowerDefinition : ScriptableObject
{
    [Tooltip("Unique identifier for this lower part. Used for serialization/build data.")]
    public string id;

    [Tooltip("Base movement speed contributed by this lower body.")]
    public int baseSpeed;

    [Tooltip("Base weight of this lower body (affects overall speed after assembly).")]
    public int baseWeight;

    [Tooltip("Turning speed in degrees per second.")]
    public int turningSpeed;
}
