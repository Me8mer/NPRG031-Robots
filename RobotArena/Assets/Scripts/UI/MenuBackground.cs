using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generates a simple UI gradient on a RawImage and can gently hue-shift over time.
/// Drop it on a full-screen RawImage under your Main Menu panel.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MenuBackgroundGradient : MonoBehaviour
{
    public enum Direction { Vertical, Horizontal, Radial }

    [Header("Gradient")]
    [SerializeField] private Direction direction = Direction.Vertical;
    [SerializeField] private Color topOrLeft = new Color(0.10f, 0.12f, 0.18f);   // dark blue
    [SerializeField] private Color bottomOrRight = new Color(0.18f, 0.07f, 0.20f); // purple

    [Header("Texture")]
    [SerializeField, Range(64, 1024)] private int width = 512;
    [SerializeField, Range(64, 1024)] private int height = 512;


    private RawImage _img;
    private Texture2D _tex;
    private float _hueOffset;

    void OnEnable()
    {
        _img = GetComponent<RawImage>();
        CreateTexture();
        RenderGradient();
    }

    void OnDisable()
    {
        if (_tex != null)
        {
            Destroy(_tex);
            _tex = null;
        }
    }

    private void CreateTexture()
    {
        if (_tex != null) Destroy(_tex);
        _tex = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        if (_img != null) _img.texture = _tex;
    }

    private void RenderGradient()
    {
        if (_tex == null) return;

        for (int y = 0; y < height; y++)
        {
            float v = height <= 1 ? 0f : (float)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = width <= 1 ? 0f : (float)x / (width - 1);

                float t = direction switch
                {
                    Direction.Vertical => v,
                    Direction.Horizontal => u,
                    Direction.Radial => Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 1.5f),
                    _ => v
                };

                Color c = Color.Lerp(topOrLeft, bottomOrRight, t);
                c = HueShift(c, _hueOffset);
                _tex.SetPixel(x, y, c);
            }
        }
        _tex.Apply(false, false);
    }

    private static Color HueShift(Color c, float offset)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        h = (h + offset) % 1f;
        return Color.HSVToRGB(h, s, v);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (width < 64) width = 64;
        if (height < 64) height = 64;
        if (Application.isPlaying && _tex != null)
        {
            CreateTexture();
            RenderGradient();
        }
    }
#endif
}
