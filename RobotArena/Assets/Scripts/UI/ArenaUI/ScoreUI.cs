using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ArenaScoreboard : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private RectTransform rowParent;          
    [SerializeField] private ArenaScoreboardRow rowPrefab;     

    [Header("Behavior")]
    [SerializeField] private bool autoFindRobotsOnStart = false;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private RobotController skipRobot;

    [Header("Optional external provider")]
    [Tooltip("Optional MonoBehaviour that implements IScoreProvider. If assigned, scores will be polled in Update.")]
    [SerializeField] private MonoBehaviour scoreProviderBehaviour;

    [Header("Message UI")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float defaultMessageLinger = 2f;
    private Coroutine _messageRoutine;

    //private IScoreProvider _provider;
    private readonly Dictionary<RobotController, ArenaScoreboardRow> _rows = new();
    private readonly Dictionary<RobotController, int> _wins = new();

    private void Awake()
    {
        //if (scoreProviderBehaviour != null)
        //    _provider = scoreProviderBehaviour as IScoreProvider;
    }

    private void Start()
    {
        if (autoFindRobotsOnStart)
        {
            var robots = FindObjectsOfType<RobotController>();
            BuildRows(robots, null);
        }
    }

    public void ClearMessage() => SetMessage(string.Empty);

    public IEnumerator FlashMessage(string msg, float seconds)
    {
        SetMessage(msg);
        yield return new WaitForSeconds(Mathf.Max(0f, seconds));
        SetMessage(string.Empty);
    }

    public void ShowWinner(RobotController rc)
    {
        if (!messageText) return;
        string name = rc ? rc.name : "Unknown";
        int wins = GetWins(rc);
        messageText.text = $"Winner: {name}   Score: {wins}";
    }
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


    public void SetMessage(string msg)
    {
        if (messageText) messageText.text = msg ?? string.Empty;
    }



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
            var display = displayNames != null && i < displayNames.Count && !string.IsNullOrWhiteSpace(displayNames[i])
                ? displayNames[i]
                : (rc ? rc.name : $"Player {i + 1}");

            row.Bind(display, 0);

            _rows[rc] = row;
            _wins[rc] = 0;
        }
    }

    public void SetWins(RobotController rc, int wins)
    {
        if (rc == null || !_rows.TryGetValue(rc, out var row)) return;
        wins = Mathf.Max(0, wins);
        _wins[rc] = wins;
        row.SetWins(wins);
    }

    public void AddWin(RobotController rc, int amount = 1)
    {
        if (rc == null || !_rows.TryGetValue(rc, out var row)) return;
        int newWins = (_wins.TryGetValue(rc, out var w) ? w : 0) + amount;
        newWins = Mathf.Max(0, newWins);
        _wins[rc] = newWins;
        row.SetWins(newWins);
    }

    public int GetWins(RobotController rc)
    {
        return rc != null && _wins.TryGetValue(rc, out var w) ? w : 0;
    }

    public void ResetAllScores()
    {
        foreach (var kv in _rows) kv.Value.SetWins(0);
        var keys = new List<RobotController>(_wins.Keys);
        foreach (var k in keys) _wins[k] = 0;
    }

    private void ClearRows()
    {
        foreach (Transform child in rowParent)
            Destroy(child.gameObject);

        _rows.Clear();
        _wins.Clear();
    }
}
