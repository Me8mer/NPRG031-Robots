using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI screen for assigning saved robot builds to player slots (2–4 players).
/// Enforces unique selections across slots and validates player count
/// before allowing continuation into the arena.
/// </summary>
public class PlayerLoaderUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private TMP_Dropdown slot1;
    [SerializeField] private TMP_Dropdown slot2;
    [SerializeField] private TMP_Dropdown slot3;
    [SerializeField] private TMP_Dropdown slot4;

    [Header("Navigation")]
    [SerializeField] private PanelManager panels;

    [Header("UI Elements")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Rules")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 4;

    private const string EmptyKey = "";

    /// <summary>One saved robot entry from disk.</summary>
    private struct RobotEntry
    {
        public string path;
        public string display;
        public RobotBuildData data;
    }

    /// <summary>Wrapper for each slot’s dropdown and its available option paths.</summary>
    private class Slot
    {
        public TMP_Dropdown dropdown;
        public List<string> optionPaths = new(); // parallel to dropdown.options
    }

    private readonly List<RobotEntry> _entries = new();
    private readonly List<Slot> _slots = new();

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
        LoadEntries();
        RepopulateAll();
        UpdateContinueState();
    }

    /// <summary>
    /// Loads saved builds from disk into memory.
    /// </summary>
    private void LoadEntries()
    {
        _entries.Clear();

        var files = BuildSerializer.ListBuildFiles();
        string defaultsDir = Path.Combine(Application.streamingAssetsPath, "Robots");
        if (Directory.Exists(defaultsDir))
        {
            foreach (var f in Directory.GetFiles(defaultsDir, "*.json"))
            {
                files.Add(f);
            }
        }
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

    /// <summary>
    /// Repopulates all dropdowns with available robots,
    /// ensuring uniqueness across slots.
    /// </summary>
    private void RepopulateAll()
    {
        // Snapshot current selections
        var currentSelections = new List<string>(_slots.Count);
        foreach (var s in _slots) currentSelections.Add(GetSelectedPath(s));

        // Build set of selected items to exclude
        var selectedSet = new HashSet<string>(currentSelections);

        for (int i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i];
            if (!s.dropdown) continue;

            var keep = currentSelections[i];

            s.optionPaths.Clear();
            var options = new List<TMP_Dropdown.OptionData>();

            // Empty entry
            options.Add(new TMP_Dropdown.OptionData("Empty"));
            s.optionPaths.Add(EmptyKey);

            // Available robots not already picked elsewhere
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

        EnsureUniqueSelections();
        UpdateContinueState();
    }

    /// <summary>
    /// Ensures no two slots are pointing to the same saved robot.
    /// Clears duplicates to "Empty".
    /// </summary>
    private void EnsureUniqueSelections()
    {
        var seen = new HashSet<string>();
        foreach (var s in _slots)
        {
            var p = GetSelectedPath(s);
            if (string.IsNullOrEmpty(p)) continue;

            if (!seen.Add(p))
                SetSelectedPath(s, EmptyKey); // clear duplicate
        }
    }

    private void OnDropdownChanged() => RepopulateAll();

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
            if (!string.IsNullOrEmpty(GetSelectedPath(s))) count++;
        return count;
    }

    /// <summary>
    /// Updates continue button and status message based on selected player count.
    /// </summary>
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

    // --- Button hooks ---
    public void OnClickBackToMenu() => panels?.ShowMainMenu();

    public void RefreshList()
    {
        LoadEntries();
        RepopulateAll();
        UpdateContinueState();
    }

    /// <summary>
    /// Stores chosen robots into <see cref="SelectedRobotsStore"/>
    /// and loads the arena scene.
    /// </summary>
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
        Debug.Log($"PlayerLoaderUI: Continuing with {chosen.Count} robots.");

        SceneManager.LoadScene("ArenaPrototypeScene");
    }
}
