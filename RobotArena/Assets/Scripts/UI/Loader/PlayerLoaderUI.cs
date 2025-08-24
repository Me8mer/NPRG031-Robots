using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerLoaderUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private TMP_Dropdown slot1;
    [SerializeField] private TMP_Dropdown slot2;
    [SerializeField] private TMP_Dropdown slot3;
    [SerializeField] private TMP_Dropdown slot4;

    [Header("Nav")]
    [SerializeField] private PanelManager panels;

    [Header("UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Rules")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 4;

    private const string EmptyKey = "";

    private struct RobotEntry
    {
        public string path;
        public string display;
        public RobotBuildData data;
    }

    private class Slot
    {
        public TMP_Dropdown dropdown;
        public List<string> optionPaths = new List<string>(); // parallel to dropdown.options
    }

    private readonly List<RobotEntry> _entries = new List<RobotEntry>();
    private readonly List<Slot> _slots = new List<Slot>();

    private void Awake()
    {
        _slots.Clear();
        if (slot1) _slots.Add(new Slot { dropdown = slot1 });
        if (slot2) _slots.Add(new Slot { dropdown = slot2 });
        if (slot3) _slots.Add(new Slot { dropdown = slot3 });
        if (slot4) _slots.Add(new Slot { dropdown = slot4 });

        foreach (var s in _slots)
        {
            if (!s.dropdown) continue;
            s.dropdown.onValueChanged.RemoveAllListeners();
            s.dropdown.onValueChanged.AddListener(_ => OnDropdownChanged());
        }
    }

    private void OnEnable()
    {
        Debug.Log("PlayerLoaderDropdownUI.OnEnable");
        LoadEntries();
        RepopulateAll();
        UpdateContinueState();
    }

    private void LoadEntries()
    {
        _entries.Clear();

        var files = BuildSerializer.ListBuildFiles();
        foreach (var fullPath in files)
        {
            var data = BuildSerializer.Load(fullPath);
            var display = !string.IsNullOrWhiteSpace(data?.robotName)
                ? data.robotName
                : Path.GetFileNameWithoutExtension(fullPath);

            _entries.Add(new RobotEntry
            {
                path = fullPath,
                display = display,
                data = data
            });
        }

        if (_entries.Count == 0)
        {
            SetStatus("No saved robots found.");
        }
    }

    private void RepopulateAll()
    {
        // Take a snapshot of current selections
        var currentSelections = new List<string>(_slots.Count);
        foreach (var s in _slots) currentSelections.Add(GetSelectedPath(s));

        // Build a set of selected items to exclude
        var selectedSet = new HashSet<string>(currentSelections);

        // Refill each dropdown with Empty + all not selected elsewhere + keep own selection
        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (!s.dropdown) continue;

            var keep = currentSelections[i];

            s.optionPaths.Clear();
            var options = new List<TMP_Dropdown.OptionData>();

            // Empty
            options.Add(new TMP_Dropdown.OptionData("Empty"));
            s.optionPaths.Add(EmptyKey);

            // Available robots
            foreach (var e in _entries)
            {
                bool selectedElsewhere = selectedSet.Contains(e.path) && e.path != keep;
                if (selectedElsewhere) continue;

                options.Add(new TMP_Dropdown.OptionData(e.display));
                s.optionPaths.Add(e.path);
            }

            s.dropdown.options = options;

            // Restore selection if possible
            int newIndex = s.optionPaths.IndexOf(keep);
            s.dropdown.value = newIndex >= 0 ? newIndex : 0;
            s.dropdown.RefreshShownValue();
        }

        // Enforce uniqueness in case two slots pointed to the same item
        EnsureUniqueSelections();
        UpdateContinueState();
    }

    private void EnsureUniqueSelections()
    {
        var seen = new HashSet<string>();
        foreach (var s in _slots)
        {
            var p = GetSelectedPath(s);
            if (string.IsNullOrEmpty(p)) continue;

            if (!seen.Add(p))
            {
                // Duplicate found, clear this slot to Empty
                SetSelectedPath(s, EmptyKey);
            }
        }
    }

    private void OnDropdownChanged()
    {
        // Rebuild option lists to keep the “not yet selected” rule
        RepopulateAll();
    }

    private string GetSelectedPath(Slot s)
    {
        if (s.dropdown == null) return EmptyKey;
        int idx = s.dropdown.value;
        if (idx < 0 || idx >= s.optionPaths.Count) return EmptyKey;
        return s.optionPaths[idx];
    }

    private void SetSelectedPath(Slot s, string path)
    {
        if (s.dropdown == null) return;
        int idx = s.optionPaths.IndexOf(path);
        s.dropdown.value = idx >= 0 ? idx : 0;
        s.dropdown.RefreshShownValue();
    }

    private int CountSelected()
    {
        int count = 0;
        foreach (var s in _slots)
        {
            if (!string.IsNullOrEmpty(GetSelectedPath(s))) count++;
        }
        return count;
    }

    private void UpdateContinueState()
    {
        int count = CountSelected();
        bool ok = count >= minPlayers && count <= maxPlayers;

        if (continueButton) continueButton.interactable = ok;

        if (_entries.Count == 0)
        {
            SetStatus("No saved robots found.");
            return;
        }

        SetStatus(ok
            ? $"{count} selected. Continue available."
            : $"Choose {minPlayers} to {maxPlayers} robots.");
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    // Buttons
    public void OnClickBackToMenu()
    {
        if (panels) panels.ShowMainMenu();
    }
    public void RefreshList()
    {
        LoadEntries();
        RepopulateAll();
        UpdateContinueState();
        Debug.Log($"PlayerLoaderDropdownUI: refresh → entries={_entries.Count}");
    }

    public void OnClickContinue()
    {
        var chosen = new List<RobotBuildData>();
        foreach (var s in _slots)
        {
            string p = GetSelectedPath(s);
            if (string.IsNullOrEmpty(p)) continue;

            var entry = _entries.Find(e => e.path == p);
            if (entry.data != null) chosen.Add(entry.data);
        }

        SelectedRobotsStore.Set(chosen);
        Debug.Log($"PlayerLoaderDropdownUI: Continuing with {chosen.Count} robots.");

        // Move to the next screen when ready, for now you can stay here or go back
        SceneManager.LoadScene("ArenaPrototypeScene");
    }
}
