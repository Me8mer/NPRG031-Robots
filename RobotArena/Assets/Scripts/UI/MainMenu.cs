using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private PanelManager panels;

    // Called by the Start Game button
    public void OnClickStartGame()
    {
        if (panels) panels.ShowPlayerLoad();
        else Debug.LogWarning("MainMenuUI: PanelManager reference not set.");
    }

    // Called by the Exit Game button
    public void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
