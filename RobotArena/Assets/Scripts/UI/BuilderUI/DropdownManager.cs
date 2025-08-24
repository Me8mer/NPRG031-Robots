using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropdownManager : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown bodyFrameDropdown;
    [SerializeField] private TMP_Dropdown lowerDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private TMP_Dropdown coreDropdown;

    [Header("Data")]
    [SerializeField] private BodyPartsCatalog catalog;

    public void Init()
    {
        if (!catalog)
        {
            Debug.LogError("DropdownManager.Init: catalog is null.");
            return;
        }

        Fill(bodyFrameDropdown, catalog.FramesCount, i => Label(catalog.GetFrameId(i), "Frame", i));
        Fill(lowerDropdown, catalog.LowersCount, i => Label(catalog.GetLowerId(i), "Lower", i));
        Fill(weaponDropdown, catalog.WeaponsCount, i => Label(catalog.GetWeaponId(i), "Weapon", i));
        Fill(coreDropdown, catalog.CoresCount, i => Label(catalog.GetCoreId(i), "Core", i));

        // Reset to first valid option
        SafeSet(bodyFrameDropdown, 0);
        SafeSet(lowerDropdown, 0);
        SafeSet(weaponDropdown, 0);
        SafeSet(coreDropdown, 0);
    }

    public int BodyFrameIndex => bodyFrameDropdown ? bodyFrameDropdown.value : 0;
    public int LowerIndex => lowerDropdown ? lowerDropdown.value : 0;
    public int WeaponIndex => weaponDropdown ? weaponDropdown.value : 0;
    public int CoreIndex => coreDropdown ? coreDropdown.value : 0;

    public void SetPartIndices(int frame, int lower, int weapon, int core)
    {
        SetIndex(bodyFrameDropdown, frame);
        SetIndex(lowerDropdown, lower);
        SetIndex(weaponDropdown, weapon);
        SetIndex(coreDropdown, core);
    }

    // ---------- internals ----------
    private static string Label(string id, string fallback, int idx)
        => string.IsNullOrWhiteSpace(id) ? $"{fallback} {idx + 1}" : id;

    private static void Fill(TMP_Dropdown dd, int count, System.Func<int, string> makeLabel)
    {
        if (!dd) return;
        var list = new List<TMP_Dropdown.OptionData>(Mathf.Max(0, count));
        for (int i = 0; i < count; i++) list.Add(new TMP_Dropdown.OptionData(makeLabel(i)));
        dd.ClearOptions();
        dd.AddOptions(list);
        dd.RefreshShownValue();
    }

    private static void SafeSet(TMP_Dropdown dd, int value)
    {
        if (!dd) return;
        int max = (dd.options?.Count ?? 0) - 1;
        dd.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, max)));
        dd.RefreshShownValue();
    }

    private static void SetIndex(TMP_Dropdown dd, int idx)
    {
        if (!dd) return;
        int max = (dd.options?.Count ?? 0) - 1;
        if (max < 0) return;
        dd.SetValueWithoutNotify(Mathf.Clamp(idx, 0, max));
        dd.RefreshShownValue();
    }
}
