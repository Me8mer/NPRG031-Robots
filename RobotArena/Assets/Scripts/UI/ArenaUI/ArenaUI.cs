//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class ArenaUI : MonoBehaviour
//{
//    [Header("Wiring")]
//    [SerializeField] private RectTransform listParent;   // The VerticalLayoutGroup parent
//    [SerializeField] private ArenaUIRow rowPrefab;       // Prefab with ArenaUIRow on it
//    [SerializeField] private TMP_Text headerText;        // Optional: "Players: 3 alive"

//    [Header("Behavior")]
//    [SerializeField] private bool autoFindRobotsOnStart = true;

//    private readonly Dictionary<RobotController, ArenaUIRow> _rows = new();

//    private void Start()
//    {
//        if (autoFindRobotsOnStart)
//        {
//            var robots = FindObjectsOfType<RobotController>();
//            foreach (var rc in robots)
//                RegisterRobot(rc);
//        }

//        RefreshHeader();
//    }

//    private void Update()
//    {
//        // Update all rows once per frame
//        foreach (var kv in _rows)
//        {
//            if (kv.Value) kv.Value.Refresh();
//        }
//        RefreshHeader();
//    }

//    public void RegisterRobot(RobotController rc, string displayName = null)
//    {
//        if (rc == null || _rows.ContainsKey(rc) || rowPrefab == null || listParent == null)
//            return;

//        var row = Instantiate(rowPrefab, listParent);
//        row.gameObject.name = $"Row_{rc.name}";
//        row.Bind(rc, displayName);

//        // Also update header when this robot dies
//        var health = rc.GetHealth();
//        if (health != null) health.OnDeath += RefreshHeader;

//        _rows[rc] = row;
//        RefreshHeader();
//    }

//    public void UnregisterRobot(RobotController rc)
//    {
//        if (rc == null) return;
//        if (_rows.TryGetValue(rc, out var row))
//        {
//            if (row) Destroy(row.gameObject);
//            _rows.Remove(rc);
//        }
//        RefreshHeader();
//    }

//    private void RefreshHeader()
//    {
//        if (!headerText) return;

//        int total = _rows.Count;
//        int alive = 0;

//        foreach (var kv in _rows)
//        {
//            var rc = kv.Key;
//            if (rc == null) continue;

//            var h = rc.GetHealth();
//            if (h != null && h.CurrentHealth > 0f) alive++;
//        }

//        headerText.text = total > 0 ? $"Players: {alive}/{total} alive" : "Players: 0";
//    }
//}

//public class ArenaUIRow : MonoBehaviour
//{
//    [Header("UI")]
//    [SerializeField] private TMP_Text nameText;
//    [SerializeField] private Slider healthBar;
//    [SerializeField] private Slider armorBar;
//    [SerializeField] private TMP_Text statusText;

//    private RobotController _controller;
//    private RobotHealth _health;
//    private float _maxHealth = 1f;
//    private float _maxArmor = 1f;
//    private bool _isBound;

//    public void Bind(RobotController controller, string displayName = null)
//    {
//        _controller = controller;
//        _health = controller != null ? controller.GetHealth() : null;

//        var stats = controller != null ? controller.GetStats() : null;
//        if (stats != null)
//        {
//            _maxHealth = Mathf.Max(1f, stats.maxHealth);
//            _maxArmor = Mathf.Max(1f, stats.maxArmor);
//        }

//        if (_health != null) _health.OnDeath += OnDeath;

//        if (nameText) nameText.text = string.IsNullOrWhiteSpace(displayName) ? controller.name : displayName;

//        // Initialize bars
//        if (healthBar) { healthBar.minValue = 0f; healthBar.maxValue = 1f; }
//        if (armorBar) { armorBar.minValue = 0f; armorBar.maxValue = 1f; }

//        _isBound = true;
//        Refresh();
//    }

//    public void Refresh()
//    {
//        if (!_isBound) return;

//        bool eliminated = _controller == null || _health == null || _health.CurrentHealth <= 0f;

//        float hpNorm = 0f;
//        float arNorm = 0f;

//        if (!eliminated)
//        {
//            hpNorm = Mathf.Clamp01(_health.CurrentHealth / _maxHealth);
//            arNorm = Mathf.Clamp01(_health.CurrentArmor / _maxArmor);
//        }

//        if (healthBar) healthBar.value = hpNorm;
//        if (armorBar) armorBar.value = arNorm;

//        if (statusText)
//            statusText.text = eliminated ? "Eliminated" : "Alive";

//        // Optional: dim the row if eliminated
//        SetRowAlpha(eliminated ? 0.5f : 1f);
//    }

//    private void OnDeath()
//    {
//        Refresh();
//    }

//    private void OnDestroy()
//    {
//        if (_health != null) _health.OnDeath -= OnDeath;
//    }

//    private void SetRowAlpha(float a)
//    {
//        var graphics = GetComponentsInChildren<Graphic>(true);
//        for (int i = 0; i < graphics.Length; i++)
//        {
//            var g = graphics[i];
//            var c = g.color;
//            c.a = a;
//            g.color = c;
//        }
//    }
//}
