using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple pause menu UI controller.
/// Supports resuming gameplay, returning to main menu,
/// and toggling pause state.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArenaGameLoop gameLoop; // Optional: ties into ArenaGameLoop’s pause system

    [Header("Scenes")]
    [Tooltip("Name of the main menu scene to load when exiting arena.")]
    [SerializeField] private string mainMenuScene = "BuilderScene";

    /// <summary>
    /// Resumes gameplay by unpausing <see cref="ArenaGameLoop"/> if present,
    /// unfreezing <see cref="Time.timeScale"/>, and hiding this panel.
    /// </summary>
    public void ResumeGame()
    {
        if (gameLoop != null)
            gameLoop.Resume(); // use proper pause API if available

        Time.timeScale = 1f;   // safety net
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Exits to the main menu scene.
    /// Resets time scale and loads the configured scene.
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        // If desired, forget selected robots when leaving arena:
        // SelectedRobotsStore.Clear();

        if (string.IsNullOrWhiteSpace(mainMenuScene))
        {
            Debug.LogError("PauseMenu: Main Menu scene name is empty. Set it in the inspector.");
            return;
        }

        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }

    /// <summary>
    /// Toggles pause state. If <see cref="ArenaGameLoop"/> is assigned,
    /// defers to its pause API. Otherwise, uses a fallback that directly
    /// manipulates <see cref="Time.timeScale"/>.
    /// </summary>
    public void TogglePause()
    {
        if (gameLoop != null)
        {
            gameLoop.TogglePause();
        }
        else
        {
            bool paused = Time.timeScale <= 0f;
            Time.timeScale = paused ? 1f : 0f;
            gameObject.SetActive(!paused);
        }
    }
}
