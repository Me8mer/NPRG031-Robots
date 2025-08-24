using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown frameColorDropdown;
    [SerializeField] private TMP_Dropdown lowerColorDropdown;
    [SerializeField] private TMP_Dropdown weaponColorDropdown;
    [SerializeField] private TMP_Dropdown coreColorDropdown;

    [SerializeField] private RobotAssembler assembler;

    // Inspector-overridable palette. Unity may serialize empty arrays here and override code initializers.
    [SerializeField] private string[] colorNames;
    [SerializeField] private Color[] colors;

    // Safe defaults used when serialized arrays are null/empty/mismatched
    private static readonly string[] DefaultColorNames = {
        "White","Red","Blue","Green","Yellow","Purple","Orange","Gray","Cyan","Black"
    };

    private static readonly Color[] DefaultColors = {
        Color.white, Color.red, Color.blue, Color.green, Color.yellow,
        new Color(0.6f,0.2f,0.8f), // Purple
        new Color(1f,0.5f,0f),     // Orange
        Color.gray, Color.cyan, Color.black
    };

    private void Awake()
    {
        EnsurePalette(); // fix serialized-empty overrides at load
    }

    private void OnValidate()
    {
        // Warn early in editor if arrays are mismatched
        if (colorNames != null && colors != null && colorNames.Length != colors.Length)
        {
            Debug.LogWarning($"ColorManager on {name}: colorNames({colorNames.Length}) != colors({colors.Length}). Will fallback to defaults at runtime.");
        }
    }

    /// Call from BuilderUI when entering Build
    public void Init()
    {
        EnsurePalette(); // double safety in case something changed via script

        Populate(frameColorDropdown);
        Populate(lowerColorDropdown);
        Populate(weaponColorDropdown);
        Populate(coreColorDropdown);

        // Make sure the shown label is valid and visible
        SafeReset(frameColorDropdown);
        SafeReset(lowerColorDropdown);
        SafeReset(weaponColorDropdown);
        SafeReset(coreColorDropdown);

        ApplyAll();
    }

    public void ApplyAll()
    {
        Apply(frameColorDropdown, assembler.SetFrameTint);
        Apply(lowerColorDropdown, assembler .SetLowerTint);
        Apply(weaponColorDropdown, assembler.SetWeaponTint);
        Apply(coreColorDropdown, assembler.SetCoreTint);
    }

    public int FrameColorIndex => frameColorDropdown ? frameColorDropdown.value : 0;
    public int LowerColorIndex => lowerColorDropdown ? lowerColorDropdown.value : 0;
    public int WeaponColorIndex => weaponColorDropdown ? weaponColorDropdown.value : 0;
    public int CoreColorIndex => coreColorDropdown ? coreColorDropdown.value : 0;

    public Color GetColor(int index)
    {
        EnsurePalette();
        if (colors == null || colors.Length == 0) return Color.white;
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }

    // ---------- Internals ----------

    private void EnsurePalette()
    {
        bool invalid = (colorNames == null || colors == null
                        || colorNames.Length == 0 || colors.Length == 0
                        || colorNames.Length != colors.Length);

        if (invalid)
        {
            // Use clones so later modifications to instance do not mutate the static arrays
            colorNames = (string[])DefaultColorNames.Clone();
            colors = (Color[])DefaultColors.Clone();
            // Optional: log once to understand why it switched
            // Debug.Log($"ColorManager on {name}: Applied default palette because serialized palette was invalid.");
        }
    }

    private void Populate(TMP_Dropdown dd)
    {
        if (!dd) { Debug.LogWarning($"ColorManager on {name}: missing dropdown reference."); return; }
        EnsurePalette();

        dd.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>(colorNames.Length);
        for (int i = 0; i < colorNames.Length; i++)
        {
            opts.Add(new TMP_Dropdown.OptionData(colorNames[i]));
        }
        dd.AddOptions(opts);

        dd.SetValueWithoutNotify(0);
        dd.RefreshShownValue();
    }

    private void SafeReset(TMP_Dropdown dd)
    {
        if (!dd) return;
        if (dd.options != null && dd.options.Count > 0)
        {
            dd.SetValueWithoutNotify(Mathf.Clamp(dd.value, 0, dd.options.Count - 1));
        }
        else
        {
            dd.SetValueWithoutNotify(0);
        }
        dd.RefreshShownValue();
    }

    private void Apply(TMP_Dropdown dd, Action<Color> applyFn)
    {
        if (applyFn == null || !dd) return;
        EnsurePalette();

        int count = Mathf.Min(colors.Length, dd.options?.Count ?? 0);
        if (count <= 0) return;

        int idx = Mathf.Clamp(dd.value, 0, count - 1);
        applyFn(colors[idx]);
    }

    // Quick tools in the component context menu
    [ContextMenu("Force Defaults")]
    private void ForceDefaults()
    {
        colorNames = (string[])DefaultColorNames.Clone();
        colors = (Color[])DefaultColors.Clone();
        Debug.Log($"ColorManager on {name}: forced default palette.");
    }

    [ContextMenu("Log Palette")]
    private void LogPalette()
    {
        Debug.Log($"ColorManager on {name} — names:{(colorNames?.Length ?? 0)}, colors:{(colors?.Length ?? 0)}");
    }

    public int ColorsCount => colors?.Length ?? 0;

    public int FindClosestColorIndex(Color c)
    {
        EnsurePalette();
        if (colors == null || colors.Length == 0) return 0;
        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < colors.Length; i++)
        {
            float dr = c.r - colors[i].r;
            float dg = c.g - colors[i].g;
            float db = c.b - colors[i].b;
            float d = dr * dr + dg * dg + db * db;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    public void SetColorDropdownIndices(int frameIdx, int lowerIdx, int weaponIdx, int coreIdx, bool apply = true)
    {
        SetIndex(frameColorDropdown, frameIdx);
        SetIndex(lowerColorDropdown, lowerIdx);
        SetIndex(weaponColorDropdown, weaponIdx);
        SetIndex(coreColorDropdown, coreIdx);
        if (apply) ApplyAll();
    }

    private void SetIndex(TMP_Dropdown dd, int idx)
    {
        if (!dd) return;
        int max = (dd.options?.Count ?? 0) - 1;
        if (max < 0) return;
        dd.SetValueWithoutNotify(Mathf.Clamp(idx, 0, max));
        dd.RefreshShownValue();
    }

}
