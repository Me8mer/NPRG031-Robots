using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropdownManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown bodyFrameDropdown;
    [SerializeField] private TMP_Dropdown lowerDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private TMP_Dropdown coreDropdown;
    [SerializeField] private PreviewAssembler assembler;

    public void Init()
    {
        if (assembler == null || !assembler.HasValidData) return;

        //Fill(lowerDropdown, GetIds(assembler, PartType.Lower));
        //Fill(weaponDropdown, GetIds(assembler, PartType.Weapon));
        //Fill(coreDropdown, GetIds(assembler, PartType.Core));

        bodyFrameDropdown.value = 0;
        lowerDropdown.value = 0;
        weaponDropdown.value = 0;
        coreDropdown.value = 0;
    }

    public int BodyFrameIndex => bodyFrameDropdown.value;
    public int LowerIndex => lowerDropdown.value;
    public int WeaponIndex => weaponDropdown.value;
    public int CoreIndex => coreDropdown.value;

    // -------- internals ----------
    private enum PartType { Lower, Weapon, Core }

    public void SetPartIndices(int frame, int lower, int weapon, int core)
    {
        SetIndex(bodyFrameDropdown, frame);
        SetIndex(lowerDropdown, lower);
        SetIndex(weaponDropdown, weapon);
        SetIndex(coreDropdown, core);
    }

    private void SetIndex(TMP_Dropdown dd, int idx)
    {
        if (!dd) return;
        int max = (dd.options?.Count ?? 0) - 1;
        if (max < 0) return;
        dd.SetValueWithoutNotify(Mathf.Clamp(idx, 0, max));
        dd.RefreshShownValue();
    }

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
        dd.RefreshShownValue();
    }
}
