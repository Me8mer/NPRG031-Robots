using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArenaGameLoop gameLoop;     // Drag your ArenaGameLoop here (optional)


    private string mainMenuScene = "BuilderScene";

    /// <summary>Resume gameplay and hide this panel.</summary>
    public void ResumeGame()
    {
        if (gameLoop != null) gameLoop.Resume();   // uses your loop's pause API if present
        Time.timeScale = 1f;                       // safety net, in case loop is missing
        gameObject.SetActive(false);
    }


    /// <summary>Return to the main menu scene.</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        // Usually we want to forget previously selected robots when leaving the arena:
        // SelectedRobotsStore.Clear();

        if (string.IsNullOrWhiteSpace(mainMenuScene))
        {
            Debug.LogError("PauseMenu: Main Menu scene name is empty. Set it in the inspector.");
            return;
        }
        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Single);
    }

    /// <summary>Optional: toggle pause from a button.</summary>
    public void TogglePause()
    {
        if (gameLoop != null) gameLoop.TogglePause();
        else
        {
            // Basic fallback if there's no loop script handling pause:
            bool paused = Time.timeScale <= 0f;
            Time.timeScale = paused ? 1f : 0f;
            gameObject.SetActive(!paused);
        }
    }

}
