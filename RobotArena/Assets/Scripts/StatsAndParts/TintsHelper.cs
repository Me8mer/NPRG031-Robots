using UnityEngine;

public static class TintUtility
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

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

            if (r.sharedMaterial.HasProperty(BaseColorId)) mpb.SetColor(BaseColorId, tint);
            else if (r.sharedMaterial.HasProperty(ColorId)) mpb.SetColor(ColorId, tint);
            else continue;

            r.SetPropertyBlock(mpb);
        }
    }
}
