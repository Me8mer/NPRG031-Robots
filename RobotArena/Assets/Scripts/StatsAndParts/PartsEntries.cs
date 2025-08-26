using UnityEngine;

/// <summary>
/// Data entry for a frame part in the catalog.
/// Stores ID, prefab, and <see cref="FrameDefinition"/>.
/// </summary>
[System.Serializable]
public struct FrameOption
{
    public string id;
    public GameObject prefab;
    public FrameDefinition definition;
}

/// <summary>
/// Data entry for a lower-body part in the catalog.
/// </summary>
[System.Serializable]
public struct LowerOption
{
    public string id;
    public GameObject prefab;
    public LowerDefinition definition;
}

/// <summary>
/// Data entry for a weapon part in the catalog.
/// </summary>
[System.Serializable]
public struct WeaponOption
{
    public string id;
    public GameObject prefab;
    public WeaponDefinition definition;
}

/// <summary>
/// Data entry for a core part in the catalog.
/// </summary>
[System.Serializable]
public struct CoreOption
{
    public string id;
    public GameObject prefab;
    public CoreDefinition definition;
}
