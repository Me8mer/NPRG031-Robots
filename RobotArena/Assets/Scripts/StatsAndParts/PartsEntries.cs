using UnityEngine;


[System.Serializable]
public struct FrameOption
{
    public string id;
    public GameObject prefab;
    public FrameDefinition definition;
}

[System.Serializable]
public struct LowerOption
{
    public string id;
    public GameObject prefab;
    public LowerDefinition definition;
}

[System.Serializable]
public struct WeaponOption
{
    public string id;
    public GameObject prefab;
    public WeaponDefinition definition;
}

[System.Serializable]
public struct CoreOption
{
    public string id;
    public GameObject prefab;
    public CoreDefinition definition;
}
