using System;
using UnityEngine;

[System.Serializable]
public struct PartOption
{
    public string id;
    public GameObject prefab;
}

[System.Serializable]
public struct FrameOption
{
    public string id;
    public GameObject prefab;
    public int baseHealth;
    public int baseArmor;
    public int baseSpeed; // abstract speed points for builder display
}

public class PreviewAssembler : MonoBehaviour
{
    [Header("Sockets in the Preview Rig")]
    [SerializeField] private Transform frameSocket;
    [SerializeField] private Transform lowerSocket;

    [Header("Options")]
    [SerializeField] private FrameOption[] frames;
    [SerializeField] private PartOption[] lowers;
    [SerializeField] private PartOption[] weapons;
    [SerializeField] private PartOption[] cores;

    // Color tint support (URP Lit uses _BaseColor; Legacy Standard uses _Color)
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    // Remember current tint so re-applying the frame keeps the color
    [SerializeField] private Color defaultFrameColor = Color.white;
    [SerializeField] private Color defaultLowerColor = Color.white;
    [SerializeField] private Color defaultWeaponColor = Color.white;
    [SerializeField] private Color defaultCoreColor = Color.white;
    private Color _frameTint = Color.clear; // Color.clear means "not set yet"
    private Color _lowerTint = Color.clear;
    private Color _weaponTint = Color.clear;
    private Color _coreTint = Color.clear;
    private Transform _lowerVisual;
    private Transform _weaponVisual;
    private Transform _coreVisual;

    // Cache to tint only the frame visual if it exists, otherwise tint whole frame
    private Transform _frameVisual;

    // Instances we spawn
    private GameObject _frameInst;
    private GameObject _lowerInst;
    private GameObject _weaponInst;
    private GameObject _coreInst;

    // Resolved mount points inside the current frame instance
    private Transform _weaponMount;
    private Transform _coreMount;
    private Transform _lowerMount;


    // Current selected indices (useful for save/load later)
    public int SelectedFrameIndex { get; private set; }
    public int SelectedLowerIndex { get; private set; }
    public int SelectedWeaponIndex { get; private set; }
    public int SelectedCoreIndex { get; private set; }

    public bool HasValidData =>
        frames != null && frames.Length > 0 &&
        lowers != null && lowers.Length > 0 &&
        weapons != null && weapons.Length > 0 &&
        cores != null && cores.Length > 0;

    public FrameOption GetCurrentFrame()
    {
        int i = Mathf.Clamp(SelectedFrameIndex, 0, Mathf.Max(0, frames.Length - 1));
        return frames[i];
    }

    public void Apply(int frameIndex, int lowerIndex, int weaponIndex, int coreIndex)
    {
        SelectedFrameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, frames.Length - 1));
        SelectedLowerIndex = Mathf.Clamp(lowerIndex, 0, Mathf.Max(0, lowers.Length - 1));
        SelectedWeaponIndex = Mathf.Clamp(weaponIndex, 0, Mathf.Max(0, weapons.Length - 1));
        SelectedCoreIndex = Mathf.Clamp(coreIndex, 0, Mathf.Max(0, cores.Length - 1));

        // 1) Frame first, since it provides the mounts
        _frameInst = Swap(_frameInst, frameSocket, SafeGet(frames, SelectedFrameIndex)?.prefab);

        // Resolve mounts INSIDE the newly spawned frame instance
        _weaponMount = FindInChildren(_frameInst?.transform, "WeaponMount");
        _coreMount = FindInChildren(_frameInst?.transform, "CoreMount");
        _lowerMount = FindInChildren(_frameInst?.transform, "LowerMount");

        // Optional: find a specific visual root to tint only the shell
        _frameVisual = FindInChildren(_frameInst?.transform, "FrameVisual");

        // Re-apply current tint to the new frame
        ApplyFrameTint();

        // Resolve mounts inside the new frame instance
        _weaponMount = FindInChildren(_frameInst?.transform, "WeaponMount");
        _coreMount = FindInChildren(_frameInst?.transform, "CoreMount");

        // 2) Lower to its own socket
        _lowerInst = Swap(_lowerInst, lowerSocket, SafeGet(lowers, SelectedLowerIndex)?.prefab);
        _lowerVisual = FindInChildren(_lowerInst?.transform, "LowerVisual");
        ApplyLowerTint();

        // 3) Weapon to frame’s WeaponMount
        if (_weaponMount != null)
        {
            _weaponInst = Swap(_weaponInst, _weaponMount, SafeGet(weapons, SelectedWeaponIndex)?.prefab);
            _weaponVisual = FindInChildren(_weaponInst?.transform, "WeaponVisual");
            ApplyWeaponTint();
        }
        else
        {
            _weaponInst = DestroyIfExists(_weaponInst); // cannot attach without a mount
        }

        // 4) Core to frame’s CoreMount
        if (_coreMount != null)
        {
            _coreInst = Swap(_coreInst, _coreMount, SafeGet(cores, SelectedCoreIndex)?.prefab);
            _coreVisual = FindInChildren(_coreInst?.transform, "CoreVisual");
            ApplyCoreTint();
        }
        else
        {
            _coreInst = DestroyIfExists(_coreInst);
        }
    }



    public void SetFrameTint(Color c) { _frameTint = c; ApplyFrameTint(); }
    public void SetLowerTint(Color c) { _lowerTint = c; ApplyLowerTint(); }
    public void SetWeaponTint(Color c) { _weaponTint = c; ApplyWeaponTint(); }
    public void SetCoreTint(Color c) { _coreTint = c; ApplyCoreTint(); }

    private void ApplyFrameTint()
    {
        if (_frameInst == null) return;
        var target = _frameVisual != null ? _frameVisual : _frameInst.transform;
        var tint = (_frameTint == Color.clear) ? defaultFrameColor : _frameTint;
        ApplyTintToRenderers(target, tint);
    }

    private void ApplyLowerTint()
    {
        if (_lowerInst == null) return;
        var root = _lowerVisual != null ? _lowerVisual : _lowerInst.transform;
        var tint = (_lowerTint == Color.clear) ? defaultLowerColor : _lowerTint;
        ApplyTintToRenderers(root, tint);
    }
    private void ApplyWeaponTint()
    {
        if (_weaponInst == null) return;
        var root = _weaponVisual != null ? _weaponVisual : _weaponInst.transform;
        var tint = (_weaponTint == Color.clear) ? defaultWeaponColor : _weaponTint;
        ApplyTintToRenderers(root, tint);
    }
    private void ApplyCoreTint()
    {
        if (_coreInst == null) return;
        var root = _coreVisual != null ? _coreVisual : _coreInst.transform;
        var tint = (_coreTint == Color.clear) ? defaultCoreColor : _coreTint;
        ApplyTintToRenderers(root, tint);
    }

    // Tints all renderers under 'root' with MPB, without duplicating materials.
    private static void ApplyTintToRenderers(Transform root, Color tint)
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
                // Material has no tintable color property. Skip.
                continue;
            }
            r.SetPropertyBlock(mpb);
        }
    }

    private static GameObject Swap(GameObject current, Transform socket, GameObject prefab)
    {
        Debug.Log($"Spawning {prefab.name} under {socket.name}");
        Debug.Log($"Swap called with prefab: {(prefab ? prefab.name : "NULL")} | socket: {(socket ? socket.name : "NULL")}");
        if (current != null) UnityEngine.Object.Destroy(current);
        if (socket == null || prefab == null) return null;

        var inst = UnityEngine.Object.Instantiate(prefab, socket);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        Debug.Log($"Swap called with prefab: {(prefab ? prefab.name : "NULL")} | socket: {(socket ? socket.name : "NULL")}");

        return inst;
    }

    private static GameObject DestroyIfExists(GameObject go)
    {
        if (go != null) UnityEngine.Object.Destroy(go);
        return null;
    }

    private static FrameOption? SafeGet(FrameOption[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
    }

    private static PartOption? SafeGet(PartOption[] arr, int index)
    {
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
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

    public string GetFrameId(int index)
    {
        index = Mathf.Clamp(index, 0, frames.Length - 1);
        return frames[index].id;
    }
    public string GetLowerId(int index)
    {
        index = Mathf.Clamp(index, 0, lowers.Length - 1);
        return lowers[index].id;
    }
    public string GetWeaponId(int index)
    {
        index = Mathf.Clamp(index, 0, weapons.Length - 1);
        return weapons[index].id;
    }
    public string GetCoreId(int index)
    {
        index = Mathf.Clamp(index, 0, cores.Length - 1);
        return cores[index].id;
    }

    public int FindFrameIndexById(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || frames == null) return 0;
        for (int i = 0; i < frames.Length; i++) if (string.Equals(frames[i].id, id, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }
    public int FindLowerIndexById(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || lowers == null) return 0;
        for (int i = 0; i < lowers.Length; i++) if (string.Equals(lowers[i].id, id, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }
    public int FindWeaponIndexById(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || weapons == null) return 0;
        for (int i = 0; i < weapons.Length; i++) if (string.Equals(weapons[i].id, id, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }
    public int FindCoreIndexById(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || cores == null) return 0;
        for (int i = 0; i < cores.Length; i++) if (string.Equals(cores[i].id, id, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }


}
