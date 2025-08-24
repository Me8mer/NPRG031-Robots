using UnityEngine;

[CreateAssetMenu(menuName = "RobotParts/Frame")]
public class FrameDefinition : ScriptableObject
{
    public string id;
    public int baseHealth;
    public int baseArmor;
    public int baseWeight;
}
