using UnityEngine;

/// <summary>
/// Handles switching between different UI panels in the menu system.
/// Only one panel is visible at a time.
/// </summary>
public class PanelManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject playerLoaderPanel;

    /// <summary>Show the main menu panel.</summary>
    public void ShowMainMenu() => ShowOnly(mainMenuPanel);

    /// <summary>Show the robot builder panel.</summary>
    public void ShowBuild() => ShowOnly(buildPanel);

    /// <summary>Show the load panel (saved robots).</summary>
    public void ShowLoad() => ShowOnly(loadPanel);

    /// <summary>
    /// Show the player loader panel (used to assign players/robots before battle).
    /// Automatically refreshes the list if <see cref="PlayerLoaderUI"/> exists.
    /// </summary>
    public void ShowPlayerLoad()
    {
        ShowOnly(playerLoaderPanel);
        if (playerLoaderPanel)
        {
            Debug.Log("PanelManager: Refreshing player loader list...");
            var loader = playerLoaderPanel.GetComponentInChildren<PlayerLoaderUI>(true);
            if (loader) loader.RefreshList();
        }
    }

    /// <summary>
    /// Activates only the requested panel and hides all others.
    /// </summary>
    private void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (buildPanel) buildPanel.SetActive(false);
        if (loadPanel) loadPanel.SetActive(false);
        if (playerLoaderPanel) playerLoaderPanel.SetActive(false);

        if (panel) panel.SetActive(true);
    }
}
