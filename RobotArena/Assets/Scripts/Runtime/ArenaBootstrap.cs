using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Bootstraps the arena at runtime:
/// - Spawns robots from selected builds
/// - Initializes game loop and UI panels
/// - Sets up arena camera
/// </summary>
public class ArenaBootstrap : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject robotRuntimeRigPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Panels")]
    [SerializeField] private ArenaPlayerPanels playerPanels;
    [SerializeField] private ArenaScoreboard scoreboardPanel;

    [Header("Loop")]
    [SerializeField] private ArenaGameLoop gameLoop;

    private readonly List<RobotController> _spawned = new();

    private void Start()
    {
        var selected = SelectedRobotsStore.Get();
        if (selected == null || selected.Count == 0)
        {
            Debug.LogWarning("ArenaBootstrap: No selected robots found. Did you come from the loader?");
            return;
        }

        int count = Mathf.Min(selected.Count, spawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            SpawnOne(selected[i], spawnPoints[i].position, spawnPoints[i].rotation, i);
        }

        if (gameLoop != null)
        {
            gameLoop.Initialize(_spawned, spawnPoints, scoreboardPanel, skipRobot: null);
        }

        ConnectToPanels();

        var cam = FindFirstObjectByType<ArenaCameraController>();
        if (cam != null) cam.Initialize(_spawned, null);

        // Optional: clear after spawn
        // SelectedRobotsStore.Clear();
    }

    private void SpawnOne(RobotBuildData build, Vector3 pos, Quaternion rot, int index)
    {
        if (!robotRuntimeRigPrefab)
        {
            Debug.LogError("ArenaBootstrap: robotRuntimeRigPrefab not set.");
            return;
        }

        var go = Instantiate(robotRuntimeRigPrefab, pos, rot);
        go.name = string.IsNullOrWhiteSpace(build.robotName) ? $"Robot_{index + 1}" : build.robotName;

        var assembler = go.GetComponent<RobotAssembler>();
        if (!assembler.AssembleFromBuild(build, out var lowerBody, out var upperBody, out var weapon, out var firePoint))
        {
            Debug.LogError($"ArenaBootstrap: Failed to assemble robot {go.name} — bad IDs?");
            Destroy(go);
            return;
        }

        var ctrl = go.GetComponent<RobotController>();
        if (!ctrl)
        {
            Debug.LogError("ArenaBootstrap: RobotController missing on robot prefab.");
            Destroy(go);
            return;
        }

        // Inject stats before wiring
        var catalog = FindFirstObjectByType<BodyPartsCatalog>() ?? FindAnyObjectByType<BodyPartsCatalog>();
        if (catalog)
        {
            // Mutate existing stats so shared references (Health/Perception) see updated values
            RobotStatsBuilder.FillFromIds(ctrl.GetStats(), build, catalog);
        }
        else
        {
            Debug.LogWarning("ArenaBootstrap: BodyPartsCatalog not found. Using default stats.");
        }

        ctrl.ApplyMovementFromStats();
        ctrl.GetComponent<RobotHealth>()?.ApplyStats(ctrl.stats);
        ctrl.GetPerception()?.ApplyStats(ctrl.stats);
        ctrl.ApplyMovementFromStats();
        ctrl.WireParts(lowerBody, upperBody, firePoint, weapon);

        var agent = go.GetComponent<NavMeshAgent>();
        if (agent)
        {
            if (NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas))
                go.transform.position = hit.position;
            agent.Warp(go.transform.position);
        }

        _spawned.Add(ctrl);
    }

    private void ConnectToPanels()
    {
        if (playerPanels != null && _spawned.Count > 0)
        {
            var names = new List<string>(_spawned.Count);
            for (int i = 0; i < _spawned.Count; i++)
            {
                names.Add(_spawned[i] ? _spawned[i].name : $"Robot {i + 1}");
            }
            playerPanels.ShowForRobots(_spawned, names);
        }
    }
}
