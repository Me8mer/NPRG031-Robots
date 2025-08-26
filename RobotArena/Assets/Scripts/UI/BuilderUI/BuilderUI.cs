using UnityEngine;

/// <summary>
/// Top-level UI controller for the robot builder screen.
/// Coordinates between dropdowns, color manager, stats panel,
/// save/load manager, and the <see cref="RobotAssembler"/>.
/// </summary>
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
        // Ensure we always start back on the main menu
        if (panels) panels.ShowMainMenu();
    }

    /// <summary>
    /// Creates a brand new robot build in the UI.
    /// Initializes dropdowns and colors, applies selection,
    /// and resets save/load state.
    /// </summary>
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

    /// <summary>
    /// Opens the load panel to select a saved build.
    /// </summary>
    public void OnLoadRobot()
    {
        panels.ShowLoad();
        saveLoad.PopulateLoadList();
    }

    public void OnBackFromLoad() => panels.ShowMainMenu();

    /// <summary>
    /// Opens the selected robot build for editing.
    /// </summary>
    public void OnOpenSelectedBuild()
    {
        saveLoad.OnOpenSelectedBuild();
        // Optionally return to build panel here if desired:
        // panels.ShowBuild();
    }

    // --- Core Apply Flow ---

    /// <summary>
    /// Applies selected parts and updates all dependent panels:
    /// - Assembles robot preview
    /// - Updates stats panel
    /// - Applies tints
    /// </summary>
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

    /// <summary>Hook for dropdown OnValueChanged events.</summary>
    public void OnPartChanged(int _) => ApplyAll();

    /// <summary>Hook for color dropdown OnValueChanged events.</summary>
    public void OnColorChanged(int _) => colors.ApplyAll();
}
