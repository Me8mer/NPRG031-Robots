using UnityEngine;

public class PanelManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject playerLoaderPanel;

    public void ShowMainMenu() => ShowOnly(mainMenuPanel);
    public void ShowBuild() => ShowOnly(buildPanel);
    public void ShowLoad() => ShowOnly(loadPanel);
    public void ShowPlayerLoad() => ShowOnly(playerLoaderPanel);

    private void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (buildPanel) buildPanel.SetActive(false);
        if (loadPanel) loadPanel.SetActive(false);
        if (playerLoaderPanel) playerLoaderPanel.SetActive(false);   
        if (panel) panel.SetActive(true);
    }
}
