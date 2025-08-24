using UnityEngine;
using UnityEngine.AI;

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
            float swing = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
            if (leftLeg) leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (rightLeg) rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        }
        else
        {
            // Reset legs when idle
            if (leftLeg) leftLeg.localRotation = Quaternion.identity;
            if (rightLeg) rightLeg.localRotation = Quaternion.identity;
        }
    }
}
