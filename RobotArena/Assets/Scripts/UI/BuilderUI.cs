using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class BuilderUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private GameObject loadPanel;

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown bodyFrameDropdown;
    [SerializeField] private TMP_Dropdown lowerDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private TMP_Dropdown coreDropdown;



    [SerializeField] private TMP_Dropdown frameColorDropdown;
    [SerializeField] private TMP_Dropdown lowerColorDropdown;
    [SerializeField] private TMP_Dropdown weaponColorDropdown;
    [SerializeField] private TMP_Dropdown coreColorDropdown;
    // Names shown in dropdown
    [SerializeField]
    private string[] colorNames = new[] {
    "White","Red","Blue","Green","Yellow","Purple","Orange","Gray","Cyan","Black"
    };

    // Matching color values (same order)
    [SerializeField]
    private Color[] colors = new[] {
    Color.white, Color.red, Color.blue, Color.green, Color.yellow,
    new Color(0.6f,0.2f,0.8f), // Purple
    new Color(1f,0.5f,0f),     // Orange
    Color.gray, Color.cyan, Color.black
    };

    [Header("Stats Text")]
    [SerializeField] private TMP_Text txtHealth;
    [SerializeField] private TMP_Text txtArmor;
    [SerializeField] private TMP_Text txtSpeed;

    [Header("Preview")]
    [SerializeField] private PreviewAssembler assembler;


    [Header("Naming and Save")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text saveStatusText; // optional

    [Header("Load Panel")]       // a simple panel for choosing files
    [SerializeField] private TMP_Dropdown loadDropdown;     // lists saves by file name
    [SerializeField] private TMP_Text loadStatusText;

    private bool _isEditing = false;
    private string _editingFullPath = null;
    // cache of file paths aligned with dropdown options
    private List<string> _loadPaths = new List<string>();

    public void OnClickSave()
    {
        TrySave();
    }

    private void Awake()
    {
        ShowMainMenu();
    }

    public void OnNewRobot()
    {
        ShowBuild();

        PopulateColorDropdown(frameColorDropdown);
        PopulateColorDropdown(lowerColorDropdown);
        PopulateColorDropdown(weaponColorDropdown);
        PopulateColorDropdown(coreColorDropdown);

        ApplyAll();

        ApplyCurrentFrameColor();
        ApplyCurrentLowerColor();
        ApplyCurrentWeaponColor();
        ApplyCurrentCoreColor();
    }

    public void OnBackToMenu()
    {
        ShowMainMenu();
    }

    public void OnBodyFrameChanged(int _) { ApplyAll(); }
    public void OnLowerChanged(int _) { ApplyAll(); }
    public void OnWeaponChanged(int _) { ApplyAll(); }
    public void OnCoreChanged(int _) { ApplyAll(); }

    private void PopulateDropdowns()
    {
        if (assembler == null || !assembler.HasValidData) return;

        // Build the option lists from the assembler’s arrays
        FillFromFrames(bodyFrameDropdown);
        Fill(lowerDropdown, GetIds(assembler, PartType.Lower));
        Fill(weaponDropdown, GetIds(assembler, PartType.Weapon));
        Fill(coreDropdown, GetIds(assembler, PartType.Core));

        // Reset to first item
        bodyFrameDropdown.value = 0;
        lowerDropdown.value = 0;
        weaponDropdown.value = 0;
        coreDropdown.value = 0;
    }

    private enum PartType { Lower, Weapon, Core }

    private static List<string> GetIds(PreviewAssembler a, PartType kind)
    {
        var labels = new List<string>();
        switch (kind)
        {
            case PartType.Lower:
                var lowersField = typeof(PreviewAssembler).GetField("lowers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var lowers = lowersField.GetValue(a) as PartOption[];
                if (lowers != null) foreach (var p in lowers) labels.Add(string.IsNullOrWhiteSpace(p.id) ? "Lower" : p.id);
                break;
            case PartType.Weapon:
                var weaponsField = typeof(PreviewAssembler).GetField("weapons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var weapons = weaponsField.GetValue(a) as PartOption[];
                if (weapons != null) foreach (var p in weapons) labels.Add(string.IsNullOrWhiteSpace(p.id) ? "Weapon" : p.id);
                break;
            case PartType.Core:
                var coresField = typeof(PreviewAssembler).GetField("cores", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cores = coresField.GetValue(a) as PartOption[];
                if (cores != null) foreach (var p in cores) labels.Add(string.IsNullOrWhiteSpace(p.id) ? "Core" : p.id);
                break;
        }
        return labels;
    }

    private void FillFromFrames(TMP_Dropdown dd)
    {
        var list = new List<TMP_Dropdown.OptionData>();
        var framesField = typeof(PreviewAssembler).GetField("frames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var frames = framesField.GetValue(assembler) as FrameOption[];
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                var label = string.IsNullOrWhiteSpace(frames[i].id) ? $"Frame {i + 1}" : frames[i].id;
                list.Add(new TMP_Dropdown.OptionData(label));
            }
        }
        dd.ClearOptions();
        dd.AddOptions(list);
    }

    private static void Fill(TMP_Dropdown dd, List<string> labels)
    {
        dd.ClearOptions();
        var list = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < labels.Count; i++) list.Add(new TMP_Dropdown.OptionData(labels[i]));
        dd.AddOptions(list);
    }

    private void ApplyAll()
    {
        Debug.Log("ApplyAll running!");
        if (assembler == null) return;

        assembler.Apply(
            bodyFrameDropdown ? bodyFrameDropdown.value : 0,
            lowerDropdown ? lowerDropdown.value : 0,
            weaponDropdown ? weaponDropdown.value : 0,
            coreDropdown ? coreDropdown.value : 0
        );

        var f = assembler.GetCurrentFrame();
        OnColorChanged(0);
        if (txtHealth) txtHealth.text = $"Health: {f.baseHealth}";
        if (txtArmor) txtArmor.text = $"Armor: {f.baseArmor}";
        if (txtSpeed) txtSpeed.text = $"Speed: {f.baseSpeed}";
    }


    private void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (buildPanel) buildPanel.SetActive(false);
        if (loadPanel) loadPanel.SetActive(false);
        if (panel) panel.SetActive(true);
    }

    private void ShowMainMenu() => ShowOnly(mainMenuPanel);
    private void ShowBuild() => ShowOnly(buildPanel);
    private void ShowLoad() => ShowOnly(loadPanel);



    private void PopulateColorDropdown(TMP_Dropdown dd)
    {
        if (!dd) return;
        dd.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < colorNames.Length; i++) opts.Add(new TMP_Dropdown.OptionData(colorNames[i]));
        dd.AddOptions(opts);
        dd.value = 0;
    }

    private void ApplyCurrentFrameColor()
    {
        if (!assembler || !frameColorDropdown) return;
        int idx = Mathf.Clamp(frameColorDropdown.value, 0, colors.Length - 1);
        assembler.SetFrameTint(colors[idx]);
    }
    private void ApplyCurrentLowerColor()
    {
        if (!assembler || !lowerColorDropdown) return;
        int idx = Mathf.Clamp(lowerColorDropdown.value, 0, colors.Length - 1);
        assembler.SetLowerTint(colors[idx]);
    }
    private void ApplyCurrentWeaponColor()
    {
        if (!assembler || !weaponColorDropdown) return;
        int idx = Mathf.Clamp(weaponColorDropdown.value, 0, colors.Length - 1);
        assembler.SetWeaponTint(colors[idx]);
    }
    private void ApplyCurrentCoreColor()
    {
        if (!assembler || !coreColorDropdown) return;
        int idx = Mathf.Clamp(coreColorDropdown.value, 0, colors.Length - 1);
        assembler.SetCoreTint(colors[idx]);
    }

    public void OnColorChanged(int _)
    {
        ApplyCurrentFrameColor();
        ApplyCurrentLowerColor();
        ApplyCurrentWeaponColor();
        ApplyCurrentCoreColor();

    }

    private void TrySave()
    {
        // 1) Validate name
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

        // 2) Gather current selections
        int iFrame = bodyFrameDropdown ? bodyFrameDropdown.value : 0;
        int iLower = lowerDropdown ? lowerDropdown.value : 0;
        int iWeapon = weaponDropdown ? weaponDropdown.value : 0;
        int iCore = coreDropdown ? coreDropdown.value : 0;

        // 3) Gather current colors (reuse your shared palette arrays)
        Color frameCol = colors[Mathf.Clamp(frameColorDropdown.value, 0, colors.Length - 1)];
        Color lowerCol = colors[Mathf.Clamp(lowerColorDropdown.value, 0, colors.Length - 1)];
        Color weaponCol = colors[Mathf.Clamp(weaponColorDropdown.value, 0, colors.Length - 1)];
        Color coreCol = colors[Mathf.Clamp(coreColorDropdown.value, 0, colors.Length - 1)];
        int iFrameCol = frameColorDropdown ? frameColorDropdown.value : 0;
        int iLowerCol = lowerColorDropdown ? lowerColorDropdown.value : 0;
        int iWeaponCol = weaponColorDropdown ? weaponColorDropdown.value : 0;
        int iCoreCol = coreColorDropdown ? coreColorDropdown.value : 0;


        // 4) Build the data object
        var data = new RobotBuildData
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

            frameColor = new ColorData(colors[iFrameCol]),
            frameColorIndex = iFrameCol,
            lowerColor = new ColorData(colors[iLowerCol]),
            lowerColorIndex = iLowerCol,
            weaponColor = new ColorData(colors[iWeaponCol]),
            weaponColorIndex = iWeaponCol,
            coreColor = new ColorData(colors[iCoreCol]),
            coreColorIndex = iCoreCol
        };

        // 5) Save to disk
        try
        {
            if (_isEditing && !string.IsNullOrWhiteSpace(_editingFullPath))
            {
                string path = BuildSerializer.SaveExact(data, _editingFullPath);
                ShowSaveStatus($"Overwritten: {Path.GetFileName(path)}");
                Debug.Log($"Robot overwritten: {path}");
            }
            else
            {
                var result = BuildSerializer.SaveUnique(data);
                if (result.renamed)
                {
                    ShowSaveStatus($"Name exists. Saved as: {result.fileName}");
                }
                else
                {
                    ShowSaveStatus($"Saved: {result.fileName}");
                }
                // when saving a new robot, you can reset edit mode
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

    private void ShowSaveStatus(string msg)
    {
        if (saveStatusText != null) saveStatusText.text = msg;
    }

    public void OnLoadRobot()
    {
        ShowLoad();
        PopulateLoadList();
    }

    public void OnBackFromLoad()
    {
        ShowMainMenu();
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

        // Open builder in edit mode with loaded data
        ApplyLoadedBuild(data, path);
    }

    private void PopulateLoadList()
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

    private void ApplyLoadedBuild(RobotBuildData data, string sourcePath)
    {
        _isEditing = true;
        _editingFullPath = sourcePath;

        // Go to builder UI
        ShowBuild();

        // Ensure part dropdowns are populated first
        PopulateDropdowns();

        // Resolve indices. Prefer IDs, fall back to saved indices.
        int iFrame = assembler.FindFrameIndexById(data.frameId);
        int iLower = assembler.FindLowerIndexById(data.lowerId);
        int iWeapon = assembler.FindWeaponIndexById(data.weaponId);
        int iCore = assembler.FindCoreIndexById(data.coreId);

        // Apply part indices to dropdowns
        if (bodyFrameDropdown) bodyFrameDropdown.value = iFrame;
        if (lowerDropdown) lowerDropdown.value = iLower;
        if (weaponDropdown) weaponDropdown.value = iWeapon;
        if (coreDropdown) coreDropdown.value = iCore;

        // Assemble preview
        ApplyAll();

        // Name field
        if (nameInput) nameInput.text = data.robotName ?? "";

        // Colors. Pick closest palette index.
        int iFrameCol = (data.frameColorIndex >= 0 && data.frameColorIndex < colors.Length)
                ? data.frameColorIndex : FindClosestColorIndex(data.frameColor.ToColor(), colors);
        int iLowerCol = (data.lowerColorIndex >= 0 && data.lowerColorIndex < colors.Length)
                        ? data.lowerColorIndex : FindClosestColorIndex(data.lowerColor.ToColor(), colors);
        int iWeaponCol = (data.weaponColorIndex >= 0 && data.weaponColorIndex < colors.Length)
                        ? data.weaponColorIndex : FindClosestColorIndex(data.weaponColor.ToColor(), colors);
        int iCoreCol = (data.coreColorIndex >= 0 && data.coreColorIndex < colors.Length)
                        ? data.coreColorIndex : FindClosestColorIndex(data.coreColor.ToColor(), colors);

        // Apply dropdowns then tints
        if (frameColorDropdown) frameColorDropdown.value = iFrameCol;
        if (lowerColorDropdown) lowerColorDropdown.value = iLowerCol;
        if (weaponColorDropdown) weaponColorDropdown.value = iWeaponCol;
        if (coreColorDropdown) coreColorDropdown.value = iCoreCol;

        // Apply tints to preview
        ApplyCurrentFrameColor();
        ApplyCurrentLowerColor();
        ApplyCurrentWeaponColor();
        ApplyCurrentCoreColor();
        _isEditing = true;
        _editingFullPath = sourcePath;
        // Optional status message
        ShowSaveStatus($"Editing: {Path.GetFileName(sourcePath)}");
    }

    private static int FindClosestColorIndex(Color c, Color[] palette)
    {
        if (palette == null || palette.Length == 0) return 0;
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < palette.Length; i++)
        {
            float dr = c.r - palette[i].r;
            float dg = c.g - palette[i].g;
            float db = c.b - palette[i].b;
            float d = dr * dr + dg * dg + db * db;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

}
