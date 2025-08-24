using TMPro;
using UnityEngine;

public class ArenaScoreboardRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text winsText;

    private int _wins;

    public void Bind(string displayName, int initialWins)
    {
        if (nameText) nameText.text = displayName;
        SetWins(initialWins);
    }

    public void SetWins(int wins)
    {
        _wins = Mathf.Max(0, wins);
        if (winsText) winsText.text = _wins.ToString();
    }
}
