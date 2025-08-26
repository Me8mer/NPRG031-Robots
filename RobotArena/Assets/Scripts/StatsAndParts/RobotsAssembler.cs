using UnityEngine;

/// <summary>
/// Handles assembly of robots from catalog parts (frame, lower body, weapon, core).
/// 
/// Supports two workflows:
/// - <see cref="Apply"/>: used by the Builder UI for preview assembly from indices
/// - <see cref="AssembleFromBuild"/>: used by ArenaBootstrap to spawn robots from saved build data
/// 
/// Also manages tinting for each part.
/// </summary>
[DisallowMultipleComponent]
public class RobotAssembler : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BodyPartsCatalog catalog;

    [Header("Sockets on the rig")]
    [Tooltip("Where the frame prefab is spawned.")]
    [SerializeField] private Transform frameSocket;
    [Tooltip("Where the lower body prefab is spawned.")]
    [SerializeField] private Transform lowerSocket;

    [Header("Tint defaults")]
    [SerializeField] private Color defaultFrameColor = Color.white;
    [SerializeField] private Color defaultLowerColor = Color.white;
    [SerializeField] private Color defaultWeaponColor = Color.white;
    [SerializeField] private Color defaultCoreColor = Color.white;

    // Selection indices (used by Builder and saving)
    public int SelectedFrameIndex { get; private set; }
    public int SelectedLowerIndex { get; private set; }
    public int SelectedWeaponIndex { get; private set; }
    public int SelectedCoreIndex { get; private set; }

    // Spawned part instances
    private GameObject _frameInst, _lowerInst, _weaponInst, _coreInst;

    // Mount transforms (extracted from prefabs)
    private Transform _weaponMount, _coreMount, _lowerMount;

    // Visual transforms (used for tint application)
    private Transform _frameVisual, _lowerVisual, _weaponVisual, _coreVisual;

    // Tint values applied by UI or loaded builds
    private Color _frameTint = Color.clear;
    private Color _lowerTint = Color.clear;
    private Color _weaponTint = Color.clear;
    private Color _coreTint = Color.clear;

    /// <summary>Returns true if catalog has all required parts loaded.</summary>
    public bool HasValidData =>
        catalog != null &&
        catalog.FramesCount > 0 &&
        catalog.LowersCount > 0 &&
        catalog.WeaponsCount > 0 &&
        catalog.CoresCount > 0;

    private void Awake()
    {
        // Auto-find catalog in scene if not assigned
        if (!catalog)
            catalog = FindFirstObjectByType<BodyPartsCatalog>();
    }

    // -------------------------------------------------------------------------
    // UI / Builder Entry Point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assembles a robot from indices into the catalog.
    /// Used by the Builder UI to update preview robots.
    /// </summary>
    public void Apply(int frameIndex, int lowerIndex, int weaponIndex, int coreIndex)
    {
        if (!catalog)
        {
            Debug.LogError("RobotAssembler: catalog is null.");
            return;
        }

        // Clamp indices to safe ranges
        SelectedFrameIndex = Mathf.Clamp(frameIndex, 0, catalog.FramesCount - 1);
        SelectedLowerIndex = Mathf.Clamp(lowerIndex, 0, catalog.LowersCount - 1);
        SelectedWeaponIndex = Mathf.Clamp(weaponIndex, 0, catalog.WeaponsCount - 1);
        SelectedCoreIndex = Mathf.Clamp(coreIndex, 0, catalog.CoresCount - 1);

        // 1) Frame first — provides sockets and visual anchor
        _frameInst = Swap(_frameInst, frameSocket, catalog.GetFramePrefab(SelectedFrameIndex));
        _weaponMount = FindInChildren(_frameInst?.transform, "WeaponMount");
        _coreMount = FindInChildren(_frameInst?.transform, "CoreMount");
        _lowerMount = FindInChildren(_frameInst?.transform, "LowerMount");
        _frameVisual = FindInChildren(_frameInst?.transform, "FrameVisual");
        ApplyFrameTint();

        // 2) Lower under the frame’s LowerMount (fallback to legacy lowerSocket)
        if (_lowerMount != null)
        {
            _lowerInst = Swap(_lowerInst, _lowerMount, catalog.GetLowerPrefab(SelectedLowerIndex));
        }
        _lowerVisual = FindInChildren(_lowerInst?.transform, "LowerVisual");
        ApplyLowerTint();

        // 3) Weapon on frame’s weapon mount
        if (_weaponMount != null)
        {
            _weaponInst = Swap(_weaponInst, _weaponMount, catalog.GetWeaponPrefab(SelectedWeaponIndex));
            _weaponVisual = FindInChildren(_weaponInst?.transform, "WeaponVisual");
            ApplyWeaponTint();
        }
        else
        {
            _weaponInst = DestroyIfExists(_weaponInst);
        }

        // 4) Core on frame’s core mount
        if (_coreMount != null)
        {
            _coreInst = Swap(_coreInst, _coreMount, catalog.GetCorePrefab(SelectedCoreIndex));
            _coreVisual = FindInChildren(_coreInst?.transform, "CoreVisual");
            ApplyCoreTint();
        }
        else
        {
            _coreInst = DestroyIfExists(_coreInst);
        }
    }

    // -------------------------------------------------------------------------
    // Arena Entry Point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assembles a robot from saved build data, wiring up required references
    /// for the <see cref="RobotController"/>.
    /// </summary>
    /// <param name="data">Build data to assemble from.</param>
    /// <param name="lowerBody">Output: lower body transform for controller.</param>
    /// <param name="upperBody">Output: upper body transform for controller.</param>
    /// <param name="weapon">Output: weapon component reference.</param>
    /// <param name="firePoint">Output: weapon muzzle transform.</param>
    public bool AssembleFromBuild(RobotBuildData data,
                                  out Transform lowerBody,
                                  out Transform upperBody,
                                  out WeaponBase weapon,
                                  out Transform firePoint)
    {
        lowerBody = null; upperBody = null; weapon = null; firePoint = null;

        if (data == null)
        {
            Debug.LogError($"RobotAssembler on {name}: build data is NULL.");
            return false;
        }
        if (!catalog)
        {
            Debug.LogError($"RobotAssembler on {name}: catalog is NULL.");
            return false;
        }
        if (!frameSocket || !lowerSocket)
        {
            Debug.LogError($"RobotAssembler on {name}: missing sockets — frameSocket:{(frameSocket ? frameSocket.name : "NULL")} lowerSocket:{(lowerSocket ? lowerSocket.name : "NULL")}");
            return false;
        }

        // Resolve IDs → indices (fallback to saved indices if ID lookup fails)
        int iFrame = ResolveIndex(catalog.FindFrameIndexById(data.frameId), data.frameIndex, catalog.FramesCount);
        int iLower = ResolveIndex(catalog.FindLowerIndexById(data.lowerId), data.lowerIndex, catalog.LowersCount);
        int iWeapon = ResolveIndex(catalog.FindWeaponIndexById(data.weaponId), data.weaponIndex, catalog.WeaponsCount);
        int iCore = ResolveIndex(catalog.FindCoreIndexById(data.coreId), data.coreIndex, catalog.CoresCount);

        // Validate prefabs exist
        var pfFrame = catalog.GetFramePrefab(iFrame);
        var pfLower = catalog.GetLowerPrefab(iLower);
        var pfWeapon = catalog.GetWeaponPrefab(iWeapon);
        var pfCore = catalog.GetCorePrefab(iCore);

        if (!pfFrame) { Debug.LogError($"RobotAssembler: Frame prefab missing at index {iFrame} (id '{data.frameId}')."); return false; }
        if (!pfLower) { Debug.LogError($"RobotAssembler: Lower prefab missing at index {iLower} (id '{data.lowerId}')."); return false; }
        if (!pfWeapon) Debug.LogWarning($"RobotAssembler: Weapon prefab missing at index {iWeapon} (id '{data.weaponId}').");
        if (!pfCore) Debug.LogWarning($"RobotAssembler: Core prefab missing at index {iCore} (id '{data.coreId}').");

        // Actually assemble robot
        Apply(iFrame, iLower, iWeapon, iCore);

        // Wire references for controller
        // Wire references for controller
        upperBody = FindInChildren(_frameInst?.transform, "UpperBody")
                    ?? _frameInst?.transform
                    ?? transform;
        lowerBody = FindInChildren(_frameInst?.transform, "LowerBody")
                    ?? FindInChildren(_lowerInst?.transform, "LowerBody")
                    ?? _lowerInst?.transform
                    ?? transform;

        weapon = _weaponInst ? _weaponInst.GetComponentInChildren<WeaponBase>() : null;
        firePoint = _weaponInst ? FindInChildren(_weaponInst.transform, "Muzzle") : null;
        if (firePoint == null) firePoint = upperBody;

        // Apply saved tints
        SetFrameTint(data.frameColor.ToColor());
        SetLowerTint(data.lowerColor.ToColor());
        SetWeaponTint(data.weaponColor.ToColor());
        SetCoreTint(data.coreColor.ToColor());

        return true;
    }

    // -------------------------------------------------------------------------
    // Tint helpers
    // -------------------------------------------------------------------------
    public void SetFrameTint(Color c) { _frameTint = c; ApplyFrameTint(); }
    public void SetLowerTint(Color c) { _lowerTint = c; ApplyLowerTint(); }
    public void SetWeaponTint(Color c) { _weaponTint = c; ApplyWeaponTint(); }
    public void SetCoreTint(Color c) { _coreTint = c; ApplyCoreTint(); }

    private void ApplyFrameTint()
    {
        if (_frameInst == null) return;
        var target = _frameVisual != null ? _frameVisual : _frameInst.transform;
        var tint = (_frameTint == Color.clear) ? defaultFrameColor : _frameTint;
        TintUtility.ApplyTintToRenderers(target, tint);
    }
    private void ApplyLowerTint()
    {
        if (_lowerInst == null) return;
        var root = _lowerVisual != null ? _lowerVisual : _lowerInst.transform;
        var tint = (_lowerTint == Color.clear) ? defaultLowerColor : _lowerTint;
        TintUtility.ApplyTintToRenderers(root, tint);
    }
    private void ApplyWeaponTint()
    {
        if (_weaponInst == null) return;
        var root = _weaponVisual != null ? _weaponVisual : _weaponInst.transform;
        var tint = (_weaponTint == Color.clear) ? defaultWeaponColor : _weaponTint;
        TintUtility.ApplyTintToRenderers(root, tint);
    }
    private void ApplyCoreTint()
    {
        if (_coreInst == null) return;
        var root = _coreVisual != null ? _coreVisual : _coreInst.transform;
        var tint = (_coreTint == Color.clear) ? defaultCoreColor : _coreTint;
        TintUtility.ApplyTintToRenderers(root, tint);
    }

    // -------------------------------------------------------------------------
    // Internal utilities
    // -------------------------------------------------------------------------
    private static int ResolveIndex(int byId, int fallbackIndex, int count)
    {
        int idx = byId >= 0 ? byId : fallbackIndex;
        return Mathf.Clamp(idx, 0, Mathf.Max(0, count - 1));
    }

    private static GameObject Swap(GameObject current, Transform socket, GameObject prefab)
    {
        if (current != null) Destroy(current);
        if (socket == null || prefab == null) return null;
        var inst = Instantiate(prefab, socket);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        return inst;
    }

    private static GameObject DestroyIfExists(GameObject go)
    {
        if (go != null) Destroy(go);
        return null;
    }

    /// <summary>
    /// Recursively searches a hierarchy for a child transform by name.
    /// </summary>
    private static Transform FindInChildren(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindInChildren(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }
}
