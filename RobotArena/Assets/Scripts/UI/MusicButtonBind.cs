using UnityEngine;
using UnityEngine.UI;

public class MusicButtonBinder : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => {
                if (MusicManager.Instance != null)
                    MusicManager.Instance.ToggleMuted();
            });
        }
    }
}
