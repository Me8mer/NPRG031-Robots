using UnityEngine;

/// <summary>
/// Defines the robot's *weapon* part.
/// Contains attack stats and weight.
/// </summary>
[CreateAssetMenu(menuName = "RobotParts/Weapon")]
public class WeaponDefinition : ScriptableObject
{
    [Tooltip("Unique identifier for this weapon. Used for serialization/build data.")]
    public string id;

    [Tooltip("Attack speed (shots per minute).")]
    public int attackSpeed;

    [Tooltip("Maximum attack range in meters.")]
    public int attackRange;

    [Tooltip("Base attack damage per shot.")]
    public int attackDamage;

    [Tooltip("Base weight of this weapon (affects mobility).")]
    public int baseWeight;
}
