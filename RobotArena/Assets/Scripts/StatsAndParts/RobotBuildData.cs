using System;
using UnityEngine;

/// <summary>
/// Serializable color data for saving/loading without UnityEngine.Color serialization issues.
/// </summary>
[Serializable]
public struct ColorData
{
    public float r, g, b, a;

    public ColorData(Color c)
    {
        r = c.r;
        g = c.g;
        b = c.b;
        a = c.a;
    }

    /// <summary>Converts back into a UnityEngine.Color struct.</summary>
    public Color ToColor() => new Color(r, g, b, a);
}

/// <summary>
/// Serializable build blueprint for a robot.
/// Stores selected part IDs + indices, applied colors, and save metadata.
/// </summary>
[Serializable]
public class RobotBuildData
{
    [Header("Meta")]
    /// <summary>Player-given robot name.</summary>
    public string robotName;

    [Header("Parts (IDs + indices)")]
    /// <summary>Frame part ID and catalog index.</summary>
    public string frameId; public int frameIndex;
    /// <summary>Lower-body part ID and catalog index.</summary>
    public string lowerId; public int lowerIndex;
    /// <summary>Weapon part ID and catalog index.</summary>
    public string weaponId; public int weaponIndex;
    /// <summary>Core part ID and catalog index.</summary>
    public string coreId; public int coreIndex;

    [Header("Colors (chosen + palette index)")]
    public ColorData frameColor; public int frameColorIndex;
    public ColorData lowerColor; public int lowerColorIndex;
    public ColorData weaponColor; public int weaponColorIndex;
    public ColorData coreColor; public int coreColorIndex;

    [Header("Save metadata")]
    /// <summary>ISO8601 timestamp string when this build was saved.</summary>
    public string savedAtIso;
}
