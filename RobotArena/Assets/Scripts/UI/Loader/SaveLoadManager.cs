using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles saving and loading of robot builds in the Builder UI.
/// Works with <see cref="BuildSerializer"/> and coordinates with UI components.
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private RobotAssembler assembler;
    [SerializeField] private DropdownManager dropdowns;
    [SerializeField] private BodyPartsCatalog catalog;
    [SerializeField] private ColorManager colors;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private PanelManager panels;
    [SerializeField] private StatsPanel stats;

    [Header("Save UI")]
    [SerializeField] private TMP_Text saveStatusText;

    [Header("Load UI")]
    [SerializeField] private TMP_Dropdown loadDropdown;
    [SerializeField] private TMP_Text loadStatusText;

    private bool _isEditing;
    private string _editingFullPath;
    private List<string> _loadPaths = new();

    // ---------- Saving ----------

    /// <summary>
    /// Tries to save the current build (unique or overwrite if editing).
    /// </summary>
    public void TrySave()
    {
        string robotName = nameInput ? nameInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(robotName))
        {
            ShowSaveStatus("Please enter a name first.");
            return;
        }
        if (assembler == null)
        {
            ShowSaveStatus("Preview assembler not set.");
            return;
        }

        var data = BuildCurrentData(robotName);

        try
        {
            if (_isEditing && !string.IsNullOrWhiteSpace(_editingFullPath))
            {
                string path = BuildSerializer.SaveExact(data, _editingFullPath);
                ShowTransient($"Overwritten: {Path.GetFileName(path)}");
            }
            else
            {
                var result = BuildSerializer.SaveUnique(data);
                ShowTransient(result.renamed
                    ? $"Name exists. Saved as: {result.fileName}"
                    : $"Saved: {result.fileName}");
                _isEditing = false;
                _editingFullPath = null;

            }
            if (nameInput) nameInput.text = string.Empty;
        }
        catch (Exception ex)
        {
            ShowSaveStatus("Save failed. See Console.");
            Debug.LogError($"Save failed: {ex}");
        }
    }

    /// <summary>
    /// Builds a <see cref="RobotBuildData"/> object from current UI state.
    /// </summary>
    private RobotBuildData BuildCurrentData(string robotName)
    {
        int iFrame = dropdowns.BodyFrameIndex;
        int iLower = dropdowns.LowerIndex;
        int iWeapon = dropdowns.WeaponIndex;
        int iCore = dropdowns.CoreIndex;

        int iFrameCol = colors.FrameColorIndex;
        int iLowerCol = colors.LowerColorIndex;
        int iWeaponCol = colors.WeaponColorIndex;
        int iCoreCol = colors.CoreColorIndex;

        return new RobotBuildData
        {
            robotName = robotName,

            frameId = catalog ? catalog.GetFrameId(iFrame) : "",
            frameIndex = iFrame,
            lowerId = catalog ? catalog.GetLowerId(iLower) : "",
            lowerIndex = iLower,
            weaponId = catalog ? catalog.GetWeaponId(iWeapon) : "",
            weaponIndex = iWeapon,
            coreId = catalog ? catalog.GetCoreId(iCore) : "",
            coreIndex = iCore,

            frameColor = new ColorData(colors.GetColor(iFrameCol)),
            frameColorIndex = iFrameCol,
            lowerColor = new ColorData(colors.GetColor(iLowerCol)),
            lowerColorIndex = iLowerCol,
            weaponColor = new ColorData(colors.GetColor(iWeaponCol)),
            weaponColorIndex = iWeaponCol,
            coreColor = new ColorData(colors.GetColor(iCoreCol)),
            coreColorIndex = iCoreCol
        };
    }

    private void ShowSaveStatus(string msg)
    {
        if (saveStatusText != null) saveStatusText.text = msg;
    }

    // ---------- Loading ----------

    /// <summary>
    /// Populates dropdown with available saved builds.
    /// </summary>
    public void PopulateLoadList()
    {
        _loadPaths = BuildSerializer.ListBuildFiles();
        if (loadDropdown == null) return;

        loadDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();

        if (_loadPaths.Count == 0)
        {
            options.Add(new TMP_Dropdown.OptionData("No saves found"));
            loadDropdown.AddOptions(options);
            loadDropdown.interactable = false;
            if (loadStatusText) loadStatusText.text = "No saved robots in the saves folder.";
            return;
        }

        foreach (var fullPath in _loadPaths)
        {
            string file = Path.GetFileNameWithoutExtension(fullPath);
            options.Add(new TMP_Dropdown.OptionData(file));
        }

        loadDropdown.AddOptions(options);
        loadDropdown.interactable = true;
        loadDropdown.value = 0;
        if (loadStatusText) loadStatusText.text = $"Found {_loadPaths.Count} robots.";
    }

    /// <summary>
    /// Loads and applies the selected build from dropdown.
    /// </summary>
    public void OnOpenSelectedBuild()
    {
        if (_loadPaths.Count == 0 || loadDropdown == null)
        {
            if (loadStatusText) loadStatusText.text = "No saved robots found.";
            return;
        }

        int idx = Mathf.Clamp(loadDropdown.value, 0, _loadPaths.Count - 1);
        string path = _loadPaths[idx];
        var data = BuildSerializer.Load(path);
        if (data == null)
        {
            if (loadStatusText) loadStatusText.text = "Failed to load file.";
            return;
        }
        ApplyLoadedBuild(data, path);
    }

    /// <summary>
    /// Applies loaded build to UI and assembler, switching to edit mode.
    /// </summary>
    private void ApplyLoadedBuild(RobotBuildData data, string sourcePath)
    {
        _isEditing = true;
        _editingFullPath = sourcePath;

        // 1) Switch to Build view
        panels?.ShowBuild();

        // 2) Ensure UI ready
        dropdowns.Init();
        colors.Init();

        // 3) Resolve part indices (prefer IDs)
        int iFrame = catalog.FindFrameIndexById(data.frameId);
        int iLower = catalog.FindLowerIndexById(data.lowerId);
        int iWeapon = catalog.FindWeaponIndexById(data.weaponId);
        int iCore = catalog.FindCoreIndexById(data.coreId);

        dropdowns.SetPartIndices(iFrame, iLower, iWeapon, iCore);

        // 4) Assemble preview + stats
        assembler.Apply(iFrame, iLower, iWeapon, iCore);
        stats?.UpdateStats();

        // 5) Name
        if (nameInput) nameInput.text = data.robotName ?? "";

        // 6) Colors
        int paletteLen = colors.ColorsCount;
        int frameColIdx = (data.frameColorIndex >= 0 && data.frameColorIndex < paletteLen)
            ? data.frameColorIndex : colors.FindClosestColorIndex(data.frameColor.ToColor());
        int lowerColIdx = (data.lowerColorIndex >= 0 && data.lowerColorIndex < paletteLen)
            ? data.lowerColorIndex : colors.FindClosestColorIndex(data.lowerColor.ToColor());
        int weaponColIdx = (data.weaponColorIndex >= 0 && data.weaponColorIndex < paletteLen)
            ? data.weaponColorIndex : colors.FindClosestColorIndex(data.weaponColor.ToColor());
        int coreColIdx = (data.coreColorIndex >= 0 && data.coreColorIndex < paletteLen)
            ? data.coreColorIndex : colors.FindClosestColorIndex(data.coreColor.ToColor());

        colors.SetColorDropdownIndices(frameColIdx, lowerColIdx, weaponColIdx, coreColIdx, apply: true);

        // 7) Status
        ShowStickyEditing(Path.GetFileName(sourcePath));
        if (loadStatusText) loadStatusText.text = "Loaded.";
    }

    public void ResetEdit()
    {
        _isEditing = false;
        _editingFullPath = null;
    }

    // ---------- Status helpers ----------

    public void ShowTransient(string msg, float seconds = 2.5f)
    {
        if (saveStatusText) saveStatusText.text = msg;
        CancelInvoke(nameof(ClearStatus));
        if (seconds > 0f) Invoke(nameof(ClearStatus), seconds);
    }

    public void ShowStickyEditing(string fileName)
    {
        if (saveStatusText) saveStatusText.text = $"Editing: {fileName}";
        CancelInvoke(nameof(ClearStatus));
    }

    public void ClearStatus()
    {
        if (saveStatusText) saveStatusText.text = string.Empty;
    }
}
