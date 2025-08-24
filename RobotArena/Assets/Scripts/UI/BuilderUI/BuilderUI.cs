using UnityEngine;

public class BuilderUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private PanelManager panels;
    [SerializeField] private DropdownManager dropdowns;
    [SerializeField] private ColorManager colors;
    [SerializeField] private StatsPanel stats;
    [SerializeField] private SaveLoadManager saveLoad;
    [SerializeField] private RobotAssembler assembler;


    private void Awake()
    {
        panels.ShowMainMenu();
    }

    public void OnNewRobot()
    {
        panels.ShowBuild();

        dropdowns.Init();
        colors.Init();

        ApplyAll();

        saveLoad.ClearStatus();
        saveLoad.ResetEdit();
    }

    public void OnBackToMenu() => panels.ShowMainMenu();

    public void OnClickSave() => saveLoad.TrySave();

    public void OnLoadRobot()
    {
        panels.ShowLoad();
        saveLoad.PopulateLoadList();
    }

    public void OnBackFromLoad() => panels.ShowMainMenu();

    public void OnOpenSelectedBuild()
    {
        saveLoad.OnOpenSelectedBuild();
        //panels.ShowBuild();
    }

    // --- Core apply flow ---
    private void ApplyAll()
    {
        if (!assembler) return;

        assembler.Apply(
            dropdowns.BodyFrameIndex,
            dropdowns.LowerIndex,
            dropdowns.WeaponIndex,
            dropdowns.CoreIndex
        );

        stats.UpdateStats();
        colors.ApplyAll();
    }

    // Hook from dropdown events
    public void OnPartChanged(int _) => ApplyAll();
    public void OnColorChanged(int _) => colors.ApplyAll();
}
