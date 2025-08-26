using UnityEngine;

/// <summary>
/// Utility for applying tints (colors) to all renderers under a root transform.
/// Uses MaterialPropertyBlock so that materials are not duplicated at runtime.
/// </summary>
public static class TintUtility
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>
    /// Applies a tint to all child renderers of a given transform.
    /// 
    /// - First tries the HDRP/Lit "_BaseColor" property.
    /// - If not found, falls back to the standard "_Color".
    /// - Uses <see cref="MaterialPropertyBlock"/> so the tint is instance-only
    ///   without modifying the shared material.
    /// </summary>
    public static void ApplyTintToRenderers(Transform root, Color tint)
    {
        if (root == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || r.sharedMaterial == null) continue;

            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            if (r.sharedMaterial.HasProperty(BaseColorId))
            {
                mpb.SetColor(BaseColorId, tint);
            }
            else if (r.sharedMaterial.HasProperty(ColorId))
            {
                mpb.SetColor(ColorId, tint);
            }
            else
            {
                continue;
            }

            r.SetPropertyBlock(mpb);
        }
    }
}
