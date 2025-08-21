using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    [Header("Assembler + UI deps")]
    [SerializeField] private PreviewAssembler assembler;
    [SerializeField] private DropdownManager dropdowns;
    [SerializeField] private ColorManager colors;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private PanelManager panels;   
    [SerializeField] private StatsPanel stats;      

    [Header("Save UI")]
    [SerializeField] private TMP_Text saveStatusText;

    [Header("Load UI")]
    [SerializeField] private TMP_Dropdown loadDropdown;
    [SerializeField] private TMP_Text loadStatusText;

    private bool _isEditing = false;
    private string _editingFullPath = null;
    private List<string> _loadPaths = new List<string>();

    // --- Save ---
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
        }
        catch (Exception ex)
        {
            ShowSaveStatus("Save failed. See Console.");
            Debug.LogError($"Save failed: {ex}");
        }
    }

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
            frameId = assembler.GetFrameId(iFrame),
            frameIndex = iFrame,
            lowerId = assembler.GetLowerId(iLower),
            lowerIndex = iLower,
            weaponId = assembler.GetWeaponId(iWeapon),
            weaponIndex = iWeapon,
            coreId = assembler.GetCoreId(iCore),
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

    // --- Load ---
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

    private void ApplyLoadedBuild(RobotBuildData data, string sourcePath)
    {
        // success state
        _isEditing = true;
        _editingFullPath = sourcePath;

        // 1) Switch to Build view only on success
        if (panels) panels.ShowBuild();

        // 2) Ensure UI is ready
        dropdowns.Init();    // populates part dropdowns
        colors.Init();       // populates color dropdowns and ensures palette

        // 3) Resolve part indices (prefer IDs)
        int iFrame = assembler.FindFrameIndexById(data.frameId);
        int iLower = assembler.FindLowerIndexById(data.lowerId);
        int iWeapon = assembler.FindWeaponIndexById(data.weaponId);
        int iCore = assembler.FindCoreIndexById(data.coreId);

        // Apply part indices to UI safely
        dropdowns.SetPartIndices(iFrame, iLower, iWeapon, iCore);

        // 4) Assemble preview and update stats
        assembler.Apply(iFrame, iLower, iWeapon, iCore);
        if (stats) stats.UpdateStats();

        // 5) Name
        if (nameInput) nameInput.text = data.robotName ?? "";

        // 6) Colors: saved index if valid, else nearest
        int paletteLen = colors.ColorsCount;

        int frameColIdx = (data.frameColorIndex >= 0 && data.frameColorIndex < paletteLen)
                           ? data.frameColorIndex : colors.FindClosestColorIndex(data.frameColor.ToColor());
        int lowerColIdx = (data.lowerColorIndex >= 0 && data.lowerColorIndex < paletteLen)
                           ? data.lowerColorIndex : colors.FindClosestColorIndex(data.lowerColor.ToColor());
        int weaponColIdx = (data.weaponColorIndex >= 0 && data.weaponColorIndex < paletteLen)
                           ? data.weaponColorIndex : colors.FindClosestColorIndex(data.weaponColor.ToColor());
        int coreColIdx = (data.coreColorIndex >= 0 && data.coreColorIndex < paletteLen)
                           ? data.coreColorIndex : colors.FindClosestColorIndex(data.coreColor.ToColor());

        // 7) Apply color indices now that dropdowns are populated
        colors.SetColorDropdownIndices(frameColIdx, lowerColIdx, weaponColIdx, coreColIdx, apply: true);

        // 8) Status
        ShowStickyEditing(System.IO.Path.GetFileName(sourcePath));
        if (loadStatusText) loadStatusText.text = "Loaded.";
    }

    public void ResetEdit()
    {
        _isEditing = false;
        _editingFullPath = null;
    }

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
