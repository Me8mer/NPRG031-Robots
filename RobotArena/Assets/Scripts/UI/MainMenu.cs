using UnityEngine;

/// <summary>
/// Top-level handler for the main menu buttons.
/// Delegates panel switching to <see cref="PanelManager"/>.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private PanelManager panels;

    /// <summary>
    /// Called by the "Start Game" button.
    /// Switches to the player loader panel so users can select robots.
    /// </summary>
    public void OnClickStartGame()
    {
        if (panels) panels.ShowPlayerLoad();
        else Debug.LogWarning("MainMenuUI: PanelManager reference not set.");
    }

    /// <summary>
    /// Called by the "Exit Game" button.
    /// Exits play mode in editor, quits the app in builds.
    /// </summary>
    public void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
