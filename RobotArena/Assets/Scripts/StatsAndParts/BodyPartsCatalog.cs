using UnityEngine;

/// <summary>
/// Central catalog of all available robot parts.
/// 
/// Provides lookups by index and ID, and returns both definitions (stats)
/// and prefabs (visuals). This is the backbone for assembling robots
/// and saving/loading builds.
/// </summary>
public class BodyPartsCatalog : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private FrameOption[] frames;
    [SerializeField] private LowerOption[] lowers;
    [SerializeField] private WeaponOption[] weapons;
    [SerializeField] private CoreOption[] cores;

    // ---------------- Safe access ----------------
    private static FrameOption? SafeGet(FrameOption[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
    }
    private static LowerOption? SafeGet(LowerOption[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
    }
    private static WeaponOption? SafeGet(WeaponOption[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
    }
    private static CoreOption? SafeGet(CoreOption[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
    }

    // ---------------- ID getters (for Save/Load) ----------------
    /// <summary>Returns the ID of the frame at <paramref name="index"/>.</summary>
    public string GetFrameId(int index) => SafeGet(frames, index)?.id ?? "";
    public string GetLowerId(int index) => SafeGet(lowers, index)?.id ?? "";
    public string GetWeaponId(int index) => SafeGet(weapons, index)?.id ?? "";
    public string GetCoreId(int index) => SafeGet(cores, index)?.id ?? "";

    // ---------------- Index finders (for Save/Load) ----------------
    /// <summary>
    /// Finds the index of a part by ID. Returns -1 if not found.
    /// Generic because all Option structs share `id`.
    /// </summary>
    private static int FindIndex<T>(T[] arr, string id) where T : struct
    {
        if (string.IsNullOrWhiteSpace(id) || arr == null) return -1;

        for (int i = 0; i < arr.Length; i++)
        {
            string cur = "";
            if (typeof(T) == typeof(FrameOption)) cur = ((FrameOption)(object)arr[i]).id;
            if (typeof(T) == typeof(LowerOption)) cur = ((LowerOption)(object)arr[i]).id;
            if (typeof(T) == typeof(WeaponOption)) cur = ((WeaponOption)(object)arr[i]).id;
            if (typeof(T) == typeof(CoreOption)) cur = ((CoreOption)(object)arr[i]).id;

            if (string.Equals(cur, id, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    public int FindFrameIndexById(string id) => FindIndex(frames, id);
    public int FindLowerIndexById(string id) => FindIndex(lowers, id);
    public int FindWeaponIndexById(string id) => FindIndex(weapons, id);
    public int FindCoreIndexById(string id) => FindIndex(cores, id);

    // ---------------- Counts ----------------
    public int FramesCount => frames != null ? frames.Length : 0;
    public int LowersCount => lowers != null ? lowers.Length : 0;
    public int WeaponsCount => weapons != null ? weapons.Length : 0;
    public int CoresCount => cores != null ? cores.Length : 0;

    // ---------------- Definition getters ----------------
    /// <summary>Gets the <see cref="FrameDefinition"/> at given index.</summary>
    public FrameDefinition GetFrameDef(int index) => SafeGet(frames, index)?.definition;
    public LowerDefinition GetLowerDef(int index) => SafeGet(lowers, index)?.definition;
    public WeaponDefinition GetWeaponDef(int index) => SafeGet(weapons, index)?.definition;
    public CoreDefinition GetCoreDef(int index) => SafeGet(cores, index)?.definition;

    /// <summary>Finds a Frame definition by ID (or null if not found).</summary>
    public FrameDefinition GetFrameDefById(string id)
    {
        int i = FindFrameIndexById(id);
        return (i >= 0 && frames != null && i < frames.Length) ? frames[i].definition : null;
    }
    public LowerDefinition GetLowerDefById(string id)
    {
        int i = FindLowerIndexById(id);
        return (i >= 0 && lowers != null && i < lowers.Length) ? lowers[i].definition : null;
    }
    public WeaponDefinition GetWeaponDefById(string id)
    {
        int i = FindWeaponIndexById(id);
        return (i >= 0 && weapons != null && i < weapons.Length) ? weapons[i].definition : null;
    }
    public CoreDefinition GetCoreDefById(string id)
    {
        int i = FindCoreIndexById(id);
        return (i >= 0 && cores != null && i < cores.Length) ? cores[i].definition : null;
    }

    // ---------------- Prefab getters ----------------
    /// <summary>Gets the prefab of a frame by index (for preview spawning).</summary>
    public GameObject GetFramePrefab(int index) => SafeGet(frames, index)?.prefab;
    public GameObject GetLowerPrefab(int index) => SafeGet(lowers, index)?.prefab;
    public GameObject GetWeaponPrefab(int index) => SafeGet(weapons, index)?.prefab;
    public GameObject GetCorePrefab(int index) => SafeGet(cores, index)?.prefab;

}
