using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// UI scoreboard that tracks and displays wins per robot across rounds.
/// Supports dynamic row creation and temporary round messages.
/// </summary>
public class ArenaScoreboard : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private RectTransform rowParent;
    [SerializeField] private ArenaScoreboardRow rowPrefab;

    [Header("Behavior")]
    [Tooltip("If true, robots in scene will be auto-detected on Start.")]
    [SerializeField] private bool autoFindRobotsOnStart = false;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private RobotController skipRobot;

    [Header("Optional external provider")]
    [Tooltip("Optional MonoBehaviour implementing IScoreProvider. If assigned, scores will be polled in Update.")]
    [SerializeField] private MonoBehaviour scoreProviderBehaviour;

    [Header("Message UI")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float defaultMessageLinger = 2f;
    private Coroutine _messageRoutine;

    private readonly Dictionary<RobotController, ArenaScoreboardRow> _rows = new();
    private readonly Dictionary<RobotController, int> _wins = new();

    private void Start()
    {
        if (autoFindRobotsOnStart)
        {
            // FIX: replaced obsolete FindObjectsOfType with FindObjectsByType
            var robots = FindObjectsByType<RobotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            BuildRows(robots, null);
        }
    }

    // ---------- Messaging ----------

    /// <summary>Clears the current message immediately.</summary>
    public void ClearMessage() => SetMessage(string.Empty);

    /// <summary>
    /// Shows a message for <paramref name="seconds"/> seconds and then clears it.
    /// </summary>
    public IEnumerator FlashMessage(string msg, float seconds)
    {
        SetMessage(msg);
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
        SetMessage(string.Empty);
    }

    /// <summary>Shows a permanent winner message.</summary>
    public void ShowWinner(RobotController rc)
    {
        if (!messageText) return;
        string name = rc ? rc.name : "Unknown";
        int wins = GetWins(rc);
        messageText.text = $"Winner: {name}   Score: {wins}";
    }

    /// <summary>
    /// Shows a message for a duration, or for <see cref="defaultMessageLinger"/> if seconds < 0.
    /// </summary>
    public void ShowMessageFor(string msg, float seconds = -1f)
    {
        if (_messageRoutine != null) StopCoroutine(_messageRoutine);
        _messageRoutine = StartCoroutine(CoMessage(msg, seconds < 0f ? defaultMessageLinger : seconds));
    }

    private IEnumerator CoMessage(string msg, float lingerSeconds)
    {
        SetMessage(msg);
        yield return new WaitForSeconds(Mathf.Max(0f, lingerSeconds));
        ClearMessage();
        _messageRoutine = null;
    }

    /// <summary>Sets the message text directly (empty = clears).</summary>
    public void SetMessage(string msg)
    {
        if (messageText) messageText.text = msg ?? string.Empty;
    }

    // ---------- Rows / Scores ----------

    /// <summary>
    /// Builds scoreboard rows for each robot, instantiating UI row prefabs.
    /// Existing rows are cleared.
    /// </summary>
    public void BuildRows(IList<RobotController> robots, IList<string> displayNames)
    {
        ClearRows();
        if (robots == null || robots.Count == 0 || rowPrefab == null || rowParent == null)
        {
            Debug.LogWarning("ArenaScoreboard: nothing to build or missing wiring.");
            return;
        }

        int count = Mathf.Clamp(robots.Count, 0, Mathf.Max(2, maxPlayers));
        for (int i = 0; i < count; i++)
        {
            var rc = robots[i];
            if (skipRobot != null && rc == skipRobot) continue;

            var row = Instantiate(rowPrefab, rowParent);

            var display = (displayNames != null && i < displayNames.Count && !string.IsNullOrWhiteSpace(displayNames[i]))
                ? displayNames[i]
                : (rc ? rc.name : $"Player {i + 1}");

            row.Bind(display, 0);

            _rows[rc] = row;
            _wins[rc] = 0;
        }
    }

    /// <summary>Sets wins for a robot and updates its row.</summary>
    public void SetWins(RobotController rc, int wins)
    {
        if (rc == null || !_rows.TryGetValue(rc, out var row)) return;
        wins = Mathf.Max(0, wins);
        _wins[rc] = wins;
        row.SetWins(wins);
    }

    /// <summary>Adds <paramref name="amount"/> wins to a robot’s total.</summary>
    public void AddWin(RobotController rc, int amount = 1)
    {
        if (rc == null || !_rows.TryGetValue(rc, out var row)) return;
        int newWins = (_wins.TryGetValue(rc, out var w) ? w : 0) + amount;
        newWins = Mathf.Max(0, newWins);
        _wins[rc] = newWins;
        row.SetWins(newWins);
    }

    /// <summary>Gets the current win count for a robot.</summary>
    public int GetWins(RobotController rc)
    {
        return rc != null && _wins.TryGetValue(rc, out var w) ? w : 0;
    }

    /// <summary>Resets all scores back to zero.</summary>
    public void ResetAllScores()
    {
        foreach (var kv in _rows) kv.Value.SetWins(0);
        var keys = new List<RobotController>(_wins.Keys);
        foreach (var k in keys) _wins[k] = 0;
    }

    /// <summary>Clears all UI rows and internal dictionaries.</summary>
    private void ClearRows()
    {
        foreach (Transform child in rowParent)
            Destroy(child.gameObject);

        _rows.Clear();
        _wins.Clear();
    }
}
