using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PickupSpawner : MonoBehaviour
{
    [Header("Prefab and Area")]
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private BoxCollider area;          // big box covering your arena floor

    private float navMeshSampleRadius = 3f;   // how far to search for NavMesh from the random point
    private int navMeshSampleTries = 12;      // how many random tries per spawn
    private float minEdgeClearance = 1f;  

    [Header("Counts")]
    [SerializeField] private int maxActive = 3;

    [Header("Timing")]
    [SerializeField] private float initialDelay = 5f;
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float spawnIntervalJitter = 10f;

    private readonly HashSet<GameObject> _active = new HashSet<GameObject>();
    private Coroutine _loop;

    void OnEnable() => _loop = StartCoroutine(Loop());
    void OnDisable() { if (_loop != null) StopCoroutine(_loop); _active.Clear(); }

    private IEnumerator Loop()
    {
        if (!pickupPrefab || !area) yield break;
        yield return new WaitForSeconds(initialDelay);

        while (enabled)
        {
            // clean destroyed or deactivated
            _active.RemoveWhere(go => go == null || !go.activeInHierarchy);

            if (_active.Count < maxActive && TryGetSpawnPosition(out var pos))
            {
                var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
                _active.Add(go);
                float wait = minSpawnInterval + Random.Range(0f, Mathf.Max(0f, spawnIntervalJitter));
                yield return new WaitForSeconds(wait);
            }
            else
            {
                yield return null;
            }
        }
    }

    private bool TryGetSpawnPosition(out Vector3 pos)
    {
        for (int i = 0; i < navMeshSampleTries; i++)
        {
            Vector3 candidate = RandomPointInBox(area);

            // project to NavMesh so it is never outside or through walls, provided walls are baked
            if (!NavMesh.SamplePosition(candidate, out var hit, navMeshSampleRadius, NavMesh.AllAreas))
                continue;

            // optional: keep a small distance from NavMesh edges to avoid hugging walls
            if (minEdgeClearance > 0f && NavMesh.FindClosestEdge(hit.position, out var edge, NavMesh.AllAreas))
            {
                if (edge.distance < minEdgeClearance) continue;
            }

            pos = hit.position;
            return true;
        }
        pos = default;
        return false;
    }

    private static Vector3 RandomPointInBox(BoxCollider box)
    {
        Vector3 c = box.center;
        Vector3 s = box.size;
        Vector3 local = new Vector3(
            Random.Range(c.x - s.x * 0.5f, c.x + s.x * 0.5f),
            c.y,
            Random.Range(c.z - s.z * 0.5f, c.z + s.z * 0.5f)
        );
        return box.transform.TransformPoint(local);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!area) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.matrix = area.transform.localToWorldMatrix;
        Gizmos.DrawCube(area.center, area.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
