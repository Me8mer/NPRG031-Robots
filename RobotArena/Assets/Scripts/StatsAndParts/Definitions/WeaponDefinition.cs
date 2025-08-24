using UnityEngine;

[CreateAssetMenu(menuName = "RobotParts/Weapon")]
public class WeaponDefinition : ScriptableObject
{
    public string id;
    public int attackSpeed;
    public int attackRange;
    public int attackDamage;
    public int baseWeight;
}
