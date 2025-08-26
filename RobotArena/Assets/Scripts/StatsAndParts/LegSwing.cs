using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple cosmetic leg swing animation based on movement speed.
/// Rotates legs back and forth with a sine wave while robot is moving.
/// </summary>
public class LegSwing : MonoBehaviour
{
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;
    [SerializeField] private float swingAngle = 15f;
    [SerializeField] private float swingSpeed = 5f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponentInParent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null) return;

        float speed = agent.velocity.magnitude;

        if (speed > 0.1f)
        {
            // Oscillate legs with opposite phase
            float swing = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
            if (leftLeg) leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLeg) rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        }
        else
        {
            // Reset legs to neutral when idle
            if (leftLeg) leftLeg.localRotation = Quaternion.identity;
            if (rightLeg) rightLeg.localRotation = Quaternion.identity;
        }
    }
}
