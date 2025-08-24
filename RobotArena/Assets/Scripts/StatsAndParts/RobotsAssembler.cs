using UnityEngine;

[DisallowMultipleComponent]
public class RobotAssembler : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BodyPartsCatalog catalog;

    [Header("Sockets on the rig")]
    [SerializeField] private Transform frameSocket;
    [SerializeField] private Transform lowerSocket;

    [Header("Tint defaults")]
    [SerializeField] private Color defaultFrameColor = Color.white;
    [SerializeField] private Color defaultLowerColor = Color.white;
    [SerializeField] private Color defaultWeaponColor = Color.white;
    [SerializeField] private Color defaultCoreColor = Color.white;

    public int SelectedFrameIndex { get; private set; }
    public int SelectedLowerIndex { get; private set; }
    public int SelectedWeaponIndex { get; private set; }
    public int SelectedCoreIndex { get; private set; }

    // spawned instances
    private GameObject _frameInst, _lowerInst, _weaponInst, _coreInst;

    // mounts and visuals
    private Transform _weaponMount, _coreMount, _lowerMount;
    private Transform _frameVisual, _lowerVisual, _weaponVisual, _coreVisual;

    // dynamic tints set by UI or by load
    private Color _frameTint = Color.clear;
    private Color _lowerTint = Color.clear;
    private Color _weaponTint = Color.clear;
    private Color _coreTint = Color.clear;

    public bool HasValidData =>
        catalog != null &&
        catalog.FramesCount > 0 &&
        catalog.LowersCount > 0 &&
        catalog.WeaponsCount > 0 &&
        catalog.CoresCount > 0;

    // Add this near the top of the class
    private void Awake()
    {
        // Auto-find catalog in scene if not assigned on the prefab
        if (!catalog)
            catalog = FindFirstObjectByType<BodyPartsCatalog>();
    }


    // -------- Builder entry: assemble by indices (used by BuilderUI/preview) --------
    public void Apply(int frameIndex, int lowerIndex, int weaponIndex, int coreIndex)
    {
        if (!catalog) { Debug.LogError("RobotAssembler: catalog is null."); return; }

        SelectedFrameIndex = Mathf.Clamp(frameIndex, 0, catalog.FramesCount - 1);
        SelectedLowerIndex = Mathf.Clamp(lowerIndex, 0, catalog.LowersCount - 1);
        SelectedWeaponIndex = Mathf.Clamp(weaponIndex, 0, catalog.WeaponsCount - 1);
        SelectedCoreIndex = Mathf.Clamp(coreIndex, 0, catalog.CoresCount - 1);

        // 1) Frame first, provides mounts and visuals
        _frameInst = Swap(_frameInst, frameSocket, catalog.GetFramePrefab(SelectedFrameIndex));
        _weaponMount = FindInChildren(_frameInst?.transform, "WeaponMount");
        _coreMount = FindInChildren(_frameInst?.transform, "CoreMount");
        _lowerMount = FindInChildren(_frameInst?.transform, "LowerMount");
        _frameVisual = FindInChildren(_frameInst?.transform, "FrameVisual");
        ApplyFrameTint();

        // 2) Lower on its own socket
        _lowerInst = Swap(_lowerInst, lowerSocket, catalog.GetLowerPrefab(SelectedLowerIndex));
        _lowerVisual = FindInChildren(_lowerInst?.transform, "LowerVisual");
        ApplyLowerTint();

        // 3) Weapon into frame mount
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

        // 4) Core into frame mount
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

    // -------- Arena entry: assemble from saved build and return wiring points --------
    public bool AssembleFromBuild(
        RobotBuildData data,
        out Transform lowerBody,
        out Transform upperBody,
        out WeaponBase weapon,
        out Transform firePoint)
    {

        lowerBody = null; upperBody = null; weapon = null; firePoint = null;
        //if (!catalog || data == null) return false;

        if (data == null)
        {
            Debug.LogError($"RobotAssembler on {name}: build data is NULL.");
            return false;
        }
        if (!catalog)
        {
            Debug.LogError($"RobotAssembler on {name}: BodyPartsCatalog is NULL (assign it on the prefab or keep one in the scene).");
            return false;
        }
        if (!frameSocket || !lowerSocket)
        {
            Debug.LogError($"RobotAssembler on {name}: missing sockets — frameSocket:{(frameSocket ? frameSocket.name : "NULL")} lowerSocket:{(lowerSocket ? lowerSocket.name : "NULL")}");
            return false;
        }



        // Resolve indices by id with safe fallback to saved index
        int iFrameById = catalog.FindFrameIndexById(data.frameId);
        int iLowerById = catalog.FindLowerIndexById(data.lowerId);
        int iWeaponById = catalog.FindWeaponIndexById(data.weaponId);
        int iCoreById = catalog.FindCoreIndexById(data.coreId);

        if (iFrameById < 0) Debug.LogWarning($"RobotAssembler: frameId '{data.frameId}' not found. Using saved index {data.frameIndex}.");
        if (iLowerById < 0) Debug.LogWarning($"RobotAssembler: lowerId '{data.lowerId}' not found. Using saved index {data.lowerIndex}.");
        if (iWeaponById < 0) Debug.LogWarning($"RobotAssembler: weaponId '{data.weaponId}' not found. Using saved index {data.weaponIndex}.");
        if (iCoreById < 0) Debug.LogWarning($"RobotAssembler: coreId '{data.coreId}' not found. Using saved index {data.coreIndex}.");

        int iFrame = ResolveIndex(iFrameById, data.frameIndex, catalog.FramesCount);
        int iLower = ResolveIndex(iLowerById, data.lowerIndex, catalog.LowersCount);
        int iWeapon = ResolveIndex(iWeaponById, data.weaponIndex, catalog.WeaponsCount);
        int iCore = ResolveIndex(iCoreById, data.coreIndex, catalog.CoresCount);

        /////
        ///DEBUG
        /// // 2) Validate prefabs exist for chosen indices
        var pfFrame = catalog.GetFramePrefab(iFrame);
        var pfLower = catalog.GetLowerPrefab(iLower);
        var pfWeapon = catalog.GetWeaponPrefab(iWeapon);
        var pfCore = catalog.GetCorePrefab(iCore);

        if (!pfFrame) { Debug.LogError($"RobotAssembler: Frame prefab missing at index {iFrame} (id '{data.frameId}')."); return false; }
        if (!pfLower) { Debug.LogError($"RobotAssembler: Lower prefab missing at index {iLower} (id '{data.lowerId}')."); return false; }
        // Weapon/Core are optional if your frame supports it, so warn only
        if (!pfWeapon) Debug.LogWarning($"RobotAssembler: Weapon prefab missing at index {iWeapon} (id '{data.weaponId}').");
        if (!pfCore) Debug.LogWarning($"RobotAssembler: Core prefab missing at index {iCore} (id '{data.coreId}').");




        Apply(iFrame, iLower, iWeapon, iCore);

        // UpperBody is part of the frame hierarchy
        upperBody = FindInChildren(_frameInst?.transform, "UpperBody") ?? _frameInst?.transform ?? transform;
        // LowerBody comes from the lower prefab
        lowerBody = FindInChildren(_lowerInst?.transform, "LowerBody") ?? transform;

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

    // -------- Public helpers used by Save/Load and UI (keep existing API names) --------
    public string GetFrameId(int index) => catalog ? catalog.GetFrameId(index) : "";
    public string GetLowerId(int index) => catalog ? catalog.GetLowerId(index) : "";
    public string GetWeaponId(int index) => catalog ? catalog.GetWeaponId(index) : "";
    public string GetCoreId(int index) => catalog ? catalog.GetCoreId(index) : "";

    public int FindFrameIndexById(string id) => catalog ? catalog.FindFrameIndexById(id) : 0;
    public int FindLowerIndexById(string id) => catalog ? catalog.FindLowerIndexById(id) : 0;
    public int FindWeaponIndexById(string id) => catalog ? catalog.FindWeaponIndexById(id) : 0;
    public int FindCoreIndexById(string id) => catalog ? catalog.FindCoreIndexById(id) : 0;

    public void SetFrameTint(Color c) { _frameTint = c; ApplyFrameTint(); }
    public void SetLowerTint(Color c) { _lowerTint = c; ApplyLowerTint(); }
    public void SetWeaponTint(Color c) { _weaponTint = c; ApplyWeaponTint(); }
    public void SetCoreTint(Color c) { _coreTint = c; ApplyCoreTint(); }

    // -------- tint apply --------
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

    // -------- internals --------
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
