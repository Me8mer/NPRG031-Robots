using UnityEngine;

/// <summary>
/// Defines the robot's *frame* (upper body or chassis).
/// Determines durability and base carrying weight.
/// </summary>
[CreateAssetMenu(menuName = "RobotParts/Frame")]
public class FrameDefinition : ScriptableObject
{
    [Tooltip("Unique identifier for this frame. Used for serialization/build data.")]
    public string id;

    [Tooltip("Base health pool provided by this frame.")]
    public int baseHealth;

    [Tooltip("Base armor pool provided by this frame.")]
    public int baseArmor;

    [Tooltip("Base carrying weight of the frame (affects speed when combined with other parts).")]
    public int baseWeight;
}
