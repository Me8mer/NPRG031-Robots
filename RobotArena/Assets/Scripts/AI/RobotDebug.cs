using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
public class RobotDebugDraw : MonoBehaviour
{
    [SerializeField] private bool drawPath = true;
    [SerializeField] private bool drawLOS = true;
    [SerializeField] private bool drawLOF = true;

    private RobotController _rc;
    private NavMeshAgent _agent;

    private void OnEnable()
    {
        _rc = GetComponent<RobotController>();
        _agent = GetComponent<NavMeshAgent>();
    }

    private void OnDrawGizmosSelected()
    {
        if (_rc == null) return;

        // 1) Path
        if (drawPath && _agent != null && _agent.hasPath && _agent.path != null)
        {
            var corners = _agent.path.corners;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i] + Vector3.up * 0.05f, corners[i + 1] + Vector3.up * 0.05f);
            }
            Gizmos.DrawSphere(_agent.destination + Vector3.up * 0.05f, 0.1f);
        }

        // Decide target to visualize (prefer fire target, else move target)
        var decision = _rc.GetDecision();
        Transform tgt = null;
        // Fix: Use correct comparison for struct, and check for default value
        if (decision.FireEnemy != null)
        {
            tgt = decision.FireEnemy.transform;
        }
        else if (decision.MoveEnemy != null)
        {
            tgt = decision.MoveEnemy.transform;
        }
        if (tgt == null) return;

        var nav = _rc.GetNavigator();
        var tgtRc = tgt.GetComponentInParent<RobotController>();
        var targeting = _rc.GetTargeting();

        // Aim point
        Vector3 aim = tgt.position + Vector3.up * 1f;
        var aimOpt = tgtRc ? targeting.AimPoint(tgtRc) : (Vector3?)null;
        if (aimOpt != null) aim = aimOpt.Value;

        // 2) Body LOS
        if (drawLOS)
        {
            bool los = nav.HasLineOfSight(transform.position, aim);
            Gizmos.color = los ? Color.cyan : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, aim);
        }

        // 3) Muzzle LOF
        if (drawLOF)
        {
            var muzzle = _rc.GetFirePointTransform();
            if (muzzle != null)
            {
                bool lof = targeting.HasLineOfFire(muzzle.position, aim, _rc.GetFireObstaclesMask());
                Gizmos.color = lof ? Color.green : new Color(1f, 0.5f, 0f); // orange means blocked
                Gizmos.DrawLine(muzzle.position, aim);
                Gizmos.DrawSphere(muzzle.position, 0.06f);
                Gizmos.DrawSphere(aim, 0.06f);
            }
        }
    }
}
