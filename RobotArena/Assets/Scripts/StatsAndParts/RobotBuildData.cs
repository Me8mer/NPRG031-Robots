using System;
using UnityEngine;

[Serializable]
public struct ColorData
{
    public float r, g, b, a;
    public ColorData(Color c) { r = c.r; g = c.g; b = c.b; a = c.a; }
    public Color ToColor() => new Color(r, g, b, a);
}

[Serializable]
public class RobotBuildData
{
    public string robotName;

    public string frameId; public int frameIndex;
    public string lowerId; public int lowerIndex;
    public string weaponId; public int weaponIndex;
    public string coreId; public int coreIndex;

    public ColorData frameColor; public int frameColorIndex;
    public ColorData lowerColor; public int lowerColorIndex;
    public ColorData weaponColor; public int weaponColorIndex;
    public ColorData coreColor; public int coreColorIndex;

    public string savedAtIso;
}
