using TMPro;
using UnityEngine;

/// <summary>
/// UI row in the scoreboard, showing one robot’s name and win count.
/// </summary>
public class ArenaScoreboardRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text winsText;

    private int _wins;

    /// <summary>
    /// Initializes this row with display name and starting wins.
    /// </summary>
    public void Bind(string displayName, int initialWins)
    {
        if (nameText) nameText.text = displayName;
        SetWins(initialWins);
    }

    /// <summary>
    /// Updates the win count label.
    /// </summary>
    public void SetWins(int wins)
    {
        _wins = Mathf.Max(0, wins);
        if (winsText) winsText.text = _wins.ToString();
    }
}
