using System.Collections.Generic;
using UnityEngine;

public class ArenaCameraController : MonoBehaviour
{
    [Header("Follow settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 15, -15);
    [SerializeField] private float followSmooth = 5f;
    [SerializeField] private float lookSmooth = 5f;
    [SerializeField] private float distance = 15f;
    [SerializeField] private float height = 8f;
    [SerializeField] private float orbitSpeed = 90f; 


    [Header("Input")]
    [SerializeField] private KeyCode orbitLeftKey = KeyCode.A;
    [SerializeField] private KeyCode orbitRightKey = KeyCode.D;
    [SerializeField] private KeyCode nextKey = KeyCode.Tab;

    [SerializeField] private RobotController skipRobot;

    private List<RobotController> _robots = new();
    private int _currentIndex = -1;
    private Transform _target;
    private float _yaw = 0f;

    void Start()
    {

        //RefreshRobots(); //There are no robots at the start yet.
    }
    public void Initialize(List<RobotController> robots, RobotController skip = null)
    {
        _robots.Clear();
        if (robots != null) _robots.AddRange(robots);
        if (skip != null) _robots.Remove(skip);
        _robots.RemoveAll(r => r == null);
        _currentIndex = (_robots.Count > 0) ? 0 : -1;
        SetTarget(_currentIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(nextKey))
        {
            CycleTarget();
        }

        if (Input.GetKey(orbitLeftKey))
        {
            _yaw -= orbitSpeed * Time.deltaTime;
        }
        if (Input.GetKey(orbitRightKey))
        {
            _yaw += orbitSpeed * Time.deltaTime;
        }

        if (_target != null)
        {
            FollowTarget();
        }
    }

    private void FollowTarget()
    {
        // Compute offset from yaw and distance/height
        Vector3 offset = Quaternion.Euler(0, _yaw, 0) * Vector3.back * distance + Vector3.up * height;
        Vector3 desiredPos = _target.position + offset;

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);

        Vector3 lookDir = (_target.position - transform.position).normalized;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, lookSmooth * Time.deltaTime);
        }
    }

    private void CycleTarget()
    {

        RefreshRobots();
     
        _currentIndex = (_currentIndex + 1) % _robots.Count;
        SetTarget(_currentIndex);
    }

    private void SetTarget(int index)
    {
        if (index < 0 || index >= _robots.Count) return;
        _target = _robots[index].transform;
        _yaw = 0f;
        Debug.Log($"Camera now following {_robots[index].name}");
    }

    public void RefreshRobots()
    {
        _robots.Clear();
        _robots.AddRange(FindObjectsByType<RobotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        _robots.Remove(skipRobot);
    }
}
