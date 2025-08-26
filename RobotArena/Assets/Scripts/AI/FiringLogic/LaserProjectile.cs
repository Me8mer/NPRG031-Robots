using UnityEngine;

/// <summary>
/// Variant of projectile that leaves a laser-like trail using <see cref="TrailRenderer"/>.
/// </summary>
[RequireComponent(typeof(Projectile))]
public class LaserProjectile : MonoBehaviour
{
    [Header("Trail")]
    [SerializeField] private Gradient color;
    [SerializeField] private float startWidth = 0.05f;
    [SerializeField] private float endWidth = 0.0f;
    [SerializeField] private Material trailMaterial;

    private TrailRenderer _trail;

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        if (!_trail) _trail = gameObject.AddComponent<TrailRenderer>();

        _trail.time = 2f;
        _trail.minVertexDistance = 0.02f;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.alignment = LineAlignment.View;
        _trail.textureMode = LineTextureMode.Stretch;

        _trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, startWidth),
            new Keyframe(1f, endWidth)
        );

        if (trailMaterial) _trail.material = trailMaterial;
        if (color.colorKeys.Length > 0) _trail.colorGradient = color;
    }

    private void OnEnable()
    {
        if (_trail) _trail.Clear();
    }
}
