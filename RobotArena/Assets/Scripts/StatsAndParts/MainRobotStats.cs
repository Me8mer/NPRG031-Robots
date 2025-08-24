[System.Serializable]
public class RobotStats
{
    public float maxHealth;
    public float maxArmor;
    public float baseSpeed;
    public float damage;
    public float attackRange;
    public float attackSpeed;
    public float turningSpeed;
    public float sightAngle;
    public float weight;

    public void CopyFrom(RobotStats other)
    {
        if (other == null) return;
        maxHealth = other.maxHealth;
        maxArmor = other.maxArmor;
        baseSpeed = other.baseSpeed;
        damage = other.damage;
        attackRange = other.attackRange;
        attackSpeed = other.attackSpeed;
        turningSpeed = other.turningSpeed;
        sightAngle = other.sightAngle;
        weight = other.weight;
    }
}
