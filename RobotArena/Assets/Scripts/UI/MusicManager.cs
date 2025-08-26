using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip track;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField] private bool startMuted = false;

    private AudioSource _src;
    private const string PrefKey = "MusicMuted";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _src = GetComponent<AudioSource>();
        _src.loop = true;
        _src.playOnAwake = false;
        _src.clip = track;
        _src.volume = volume;
        _src.ignoreListenerPause = true; // keeps music playing when Time.timeScale == 0

        bool muted = PlayerPrefs.GetInt(PrefKey, startMuted ? 1 : 0) == 1;
        SetMuted(muted);

        if (track != null) _src.Play();
    }

    public void SetMuted(bool muted)
    {
        if (_src == null) return;
        _src.mute = muted;
        PlayerPrefs.SetInt(PrefKey, muted ? 1 : 0);
    }

    public void ToggleMuted()
    {
        if (_src == null) return;
        SetMuted(!_src.mute);
    }

    public bool IsMuted => _src != null && _src.mute;
}
