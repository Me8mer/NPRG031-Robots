using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArenaGameLoop : MonoBehaviour
{
    [Header("Round Flow")]
    [Tooltip("Seconds to Countdwon!")]
    [SerializeField] private int countdownSeconds = 3;

    [Tooltip("Seconds to show the winner before next round")]
    [SerializeField] private float winnerPause = 8f;
    private Transform[] spawnPoints;
    private ArenaScoreboard scoreboard;
    private RobotController skipRobot;

    [Header("Pause")]
    [Tooltip("Panel (Canvas child) to show while paused")]
    [SerializeField] private GameObject pausePanel;


    // Simple score storage. Keyed by object instance so names can duplicate.
    private readonly Dictionary<RobotController, int> _scores = new();

    // Per-robot start transforms
    private struct StartPose { public Vector3 pos; public Quaternion rot; }
    private readonly Dictionary<RobotController, StartPose> _starts = new();

    private List<RobotController> _robots = new();
    private int _aliveCount;
    private bool _initialized;
    private bool _paused;

    void Start()
    {
        // ensure normal time on scene start
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (_initialized) return;

        _robots = FindObjectsByType<RobotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).ToList();
        if (_robots.Count == 0)
        {
            Debug.LogWarning("ArenaGameLoop: no robots found in scene. Did ArenaBootstrap run?");
            return;
        }

        PrepareStartsAndScoreboard();
        StartCoroutine(RunLoop());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))

        {
            TogglePause();
        }
    }

    public void Initialize(
    List<RobotController> robots,
    Transform[] spawns,
    ArenaScoreboard board,
    RobotController skipRobot = null)
    {
        _robots = robots ?? new List<RobotController>();
        spawnPoints = spawns;
        scoreboard = board;
        this.skipRobot = skipRobot;
        _initialized = true;

        PrepareStartsAndScoreboard();
        StopAllCoroutines();
        StartCoroutine(RunLoop());
    }

    private IEnumerator RunLoop()
    {
        while (true)
        {
            ResetAllRobotsToStart();
            if (scoreboard) scoreboard.SetMessage("Round starting...");
            yield return Countdown();
            UnlockAll();

            // Fight phase: wait until one (or zero) left
            _aliveCount = CountAlive();
            while (_aliveCount > 1)
            {
                if (!_paused)
                    _aliveCount = CountAlive();
                yield return null;
            }

            var winner = FindWinner();
            if (winner != null)
            {
                _scores[winner]++;
                AnnounceWinner(winner);
            }
            else
            {
                Debug.Log("Round ended with no winner.");
            }

            yield return new WaitForSeconds(winnerPause);
        }
    }

    private void ResetAllRobotsToStart()
    {
        foreach (var rc in _robots.ToArray())
        {
            // handle deleted robots gracefully
            if (rc == skipRobot || rc == null) { continue; }
            var hp = rc.GetHealth();
            if (hp != null)
            {
                if (!rc.gameObject.activeSelf) hp.Revive();
                hp.RefillToMax();
            }

            // Lock logic while we reposition and reset
            rc.SetControlLocked(true);

            // Warp to start pose
            if (_starts.TryGetValue(rc, out var sp))
            {
                rc.WarpTo(sp.pos, sp.rot);
            }

            // Reset state to Idle and clear agent path
            rc.SetCurrentState(RobotState.Idle);
            rc.GetAgent()?.ResetPath();
        }
    }

    private IEnumerator Countdown()
    {
        for (int t = countdownSeconds; t >= 1; t--)
        {
            if (scoreboard) scoreboard.ShowMessageFor($"{t}", 0.95f);
            yield return new WaitForSeconds(1f);
        }
        if (scoreboard) scoreboard.ShowMessageFor("GO!", 0.75f);
    }

    private void UnlockAll()
    {
        foreach (var rc in _robots)
        {
            if (rc == null || rc == skipRobot) continue;
            rc.SetControlLocked(false);
        }
    }

    private void OnRobotDeath()
    {
        // RobotHealth invokes this just before soft-destroy or Destroy.
        // We poll alive count in the loop, so nothing needed here.
    }

    private int CountAlive()
    {
        int alive = 0;
        foreach (var rc in _robots)
        {
            if (rc == null || rc == skipRobot) continue;
            if (rc.gameObject.activeInHierarchy)
            {
                var hp = rc.GetHealth();
                if (hp != null && hp.CurrentHealth > 0f) alive++;
            }
        }
        return alive;
    }

    private RobotController FindWinner()
    {
        foreach (var rc in _robots)
        {
            if (rc == null || rc == skipRobot) continue;
            if (rc.gameObject.activeInHierarchy)
            {
                var hp = rc.GetHealth();
                if (hp != null && hp.CurrentHealth > 0f) return rc;
            }
        }
        return null;
    }

    private void AnnounceWinner(RobotController rc)
    {
        if (!rc || !scoreboard) return;

        scoreboard.AddWin(rc, 1);
        int wins = scoreboard.GetWins(rc);
        scoreboard.ShowMessageFor($"Winner: {rc.name}   Score: {wins}", 2.0f);
    }

    private void PrepareStartsAndScoreboard()
    {
        if (_robots == null || _robots.Count == 0) return;

        if (scoreboard)
        {
            scoreboard.BuildRows(_robots, null);
            scoreboard.SetMessage("Get ready");
        }

        for (int i = 0; i < _robots.Count; i++)
        {
            var rc = _robots[i];
            var hp = rc.GetHealth();
            if (hp != null)
            {
                hp.SetDestroyOnDeath(false);
                hp.OnDeath += OnRobotDeath;
            }
            var sp = (spawnPoints != null && spawnPoints.Length > 0)
                ? spawnPoints[i % spawnPoints.Length]
                : rc.transform;

            _starts[rc] = new StartPose { pos = sp.position, rot = sp.rotation };
            if (!_scores.ContainsKey(rc)) _scores[rc] = 0;
        }
    }
    // ---------- Pause control ----------
    public void TogglePause()
    {
        if (_paused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (_paused) return;
        _paused = true;

        if (pausePanel)
        {
            // Ensure it’s visible and interactive regardless of timeScale
            pausePanel.SetActive(true);

            // If there’s an Animator on the panel, force it to unscaled time
            var anim = pausePanel.GetComponent<Animator>();
            if (anim) anim.updateMode = AnimatorUpdateMode.UnscaledTime;

            // If the panel lives on a separate canvas, make sure it renders with the main camera
            var canvas = pausePanel.GetComponentInParent<Canvas>();
            if (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
        }

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!_paused) return;
        _paused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
    }

    void OnDestroy()
    {
        // make sure we never leave the game frozen when this object goes away
        if (_paused) Time.timeScale = 1f;
    }

}

