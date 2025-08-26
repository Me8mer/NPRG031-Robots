using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages selectable color palettes for robot parts in the Builder UI.
/// Handles populating dropdowns, applying chosen tints to the <see cref="RobotAssembler"/>,
/// and providing safe defaults if serialized arrays are empty or mismatched.
/// </summary>
public class ColorManager : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown frameColorDropdown;
    [SerializeField] private TMP_Dropdown lowerColorDropdown;
    [SerializeField] private TMP_Dropdown weaponColorDropdown;
    [SerializeField] private TMP_Dropdown coreColorDropdown;

    [Header("Assembler")]
    [SerializeField] private RobotAssembler assembler;

    [Header("Custom Palette (optional)")]
    [Tooltip("Unity may serialize empty arrays here and override defaults — EnsurePalette will fix this at runtime.")]
    [SerializeField] private string[] colorNames;
    [SerializeField] private Color[] colors;

    // Built-in fallback palette
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
        EnsurePalette(); // Fix palette if Unity serialized it as empty
    }

    private void OnValidate()
    {
        // Warn in editor if palette arrays are mismatched
        if (colorNames != null && colors != null && colorNames.Length != colors.Length)
        {
            Debug.LogWarning($"ColorManager on {name}: colorNames({colorNames.Length}) != colors({colors.Length}). Will fallback to defaults at runtime.");
        }
    }

    /// <summary>
    /// Initializes dropdowns and applies the default color selection.
    /// Call this when opening the Builder UI.
    /// </summary>
    public void Init()
    {
        EnsurePalette();

        Populate(frameColorDropdown);
        Populate(lowerColorDropdown);
        Populate(weaponColorDropdown);
        Populate(coreColorDropdown);

        SafeReset(frameColorDropdown);
        SafeReset(lowerColorDropdown);
        SafeReset(weaponColorDropdown);
        SafeReset(coreColorDropdown);

        ApplyAll();
    }

    /// <summary>Applies the currently selected colors from all dropdowns.</summary>
    public void ApplyAll()
    {
        Apply(frameColorDropdown, assembler.SetFrameTint);
        Apply(lowerColorDropdown, assembler.SetLowerTint);
        Apply(weaponColorDropdown, assembler.SetWeaponTint);
        Apply(coreColorDropdown, assembler.SetCoreTint);
    }

    // Expose selected indices
    public int FrameColorIndex => frameColorDropdown ? frameColorDropdown.value : 0;
    public int LowerColorIndex => lowerColorDropdown ? lowerColorDropdown.value : 0;
    public int WeaponColorIndex => weaponColorDropdown ? weaponColorDropdown.value : 0;
    public int CoreColorIndex => coreColorDropdown ? coreColorDropdown.value : 0;

    /// <summary>Returns the color from palette by index (clamped).</summary>
    public Color GetColor(int index)
    {
        EnsurePalette();
        if (colors == null || colors.Length == 0) return Color.white;
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }

    // ---------- Internals ----------

    /// <summary>
    /// Ensures that the palette is valid.
    /// If arrays are null, empty, or mismatched, resets to defaults.
    /// </summary>
    private void EnsurePalette()
    {
        bool invalid = (colorNames == null || colors == null
                        || colorNames.Length == 0 || colors.Length == 0
                        || colorNames.Length != colors.Length);

        if (invalid)
        {
            colorNames = (string[])DefaultColorNames.Clone();
            colors = (Color[])DefaultColors.Clone();
        }
    }

    /// <summary>
    /// Populates a dropdown with the current palette.
    /// </summary>
    private void Populate(TMP_Dropdown dd)
    {
        if (!dd) { Debug.LogWarning($"ColorManager on {name}: missing dropdown reference."); return; }
        EnsurePalette();

        dd.ClearOptions();
        var opts = new List<TMP_Dropdown.OptionData>(colorNames.Length);
        for (int i = 0; i < colorNames.Length; i++)
            opts.Add(new TMP_Dropdown.OptionData(colorNames[i]));
        dd.AddOptions(opts);

        dd.SetValueWithoutNotify(0);
        dd.RefreshShownValue();
    }

    /// <summary>
    /// Resets a dropdown to a safe valid index so its label is never out of bounds.
    /// </summary>
    private void SafeReset(TMP_Dropdown dd)
    {
        if (!dd) return;
        if (dd.options != null && dd.options.Count > 0)
            dd.SetValueWithoutNotify(Mathf.Clamp(dd.value, 0, dd.options.Count - 1));
        else
            dd.SetValueWithoutNotify(0);
        dd.RefreshShownValue();
    }

    /// <summary>
    /// Applies the selected color from a dropdown using a provided tint setter.
    /// </summary>
    private void Apply(TMP_Dropdown dd, Action<Color> applyFn)
    {
        if (applyFn == null || !dd) return;
        EnsurePalette();

        int count = Mathf.Min(colors.Length, dd.options?.Count ?? 0);
        if (count <= 0) return;

        int idx = Mathf.Clamp(dd.value, 0, count - 1);
        applyFn(colors[idx]);
    }

    // ---------- Tools ----------
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

    /// <summary>
    /// Finds the palette index closest to the given color (RGB distance).
    /// </summary>
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

    /// <summary>
    /// Sets dropdown indices directly (used when loading saved builds).
    /// </summary>
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
