using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArenaPlayerPanels : MonoBehaviour
{
    [System.Serializable]
    public class PanelSlot
    {
        public GameObject root;          // Your existing panel GameObject
        public TMP_Text nameText;        // Name label inside the panel
        public Slider healthSlider;      // 0..1
        public Slider armorSlider;       // 0..1

        private RobotController _rc;
        private RobotHealth _health;
        private float _maxHealth = 1f;
        private float _maxArmor = 1f;
        private bool _bound;

        public bool IsBound => _bound;

        public void Bind(RobotController rc, string displayName = null)
        {
            _rc = rc;
            _health = rc ? rc.GetHealth() : null;

            var stats = rc ? rc.GetStats() : null;
            if (stats != null)
            {
                _maxHealth = Mathf.Max(1f, stats.maxHealth);
                _maxArmor = Mathf.Max(1f, stats.maxArmor);
            }
            else
            {
                _maxHealth = 1f;
                _maxArmor = 1f;
            }

            if (nameText) nameText.text = string.IsNullOrWhiteSpace(displayName) ? rc.name : displayName;

            if (healthSlider) { healthSlider.minValue = 0f; healthSlider.maxValue = 1f; }
            if (armorSlider) { armorSlider.minValue = 0f; armorSlider.maxValue = 1f; }

            if (_health != null) _health.OnDeath += Refresh;

            if (root) root.SetActive(true);
            _bound = true;
            Refresh();
        }

        public void Refresh()
        {
            if (!_bound) return;

            bool eliminated = _health == null || _health.CurrentHealth <= 0f;

            float hp = eliminated ? 0f : Mathf.Clamp01(_health.CurrentHealth / _maxHealth);
            float ar = eliminated ? 0f : Mathf.Clamp01(_health.CurrentArmor / _maxArmor);

            if (healthSlider) healthSlider.value = hp;
            if (armorSlider) armorSlider.value = ar;
        }

        public void Clear()
        {
            if (_health != null) _health.OnDeath -= Refresh;
            _rc = null;
            _health = null;
            _bound = false;

            if (nameText) nameText.text = "";
            if (healthSlider) healthSlider.value = 0f;
            if (armorSlider) armorSlider.value = 0f;

            if (root) root.SetActive(false);
        }
    }

    [Header("Manual panel list")]
    [SerializeField] private List<PanelSlot> panels = new List<PanelSlot>(); // Fill with 2–4 slots in Inspector

    [Header("Optional")]
    [SerializeField] private bool autoFindRobotsOnStart = false;

    void Awake()
    {
        // Start with all panels hidden
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null) panels[i].Clear();
        }
    }

    void Start()
    {
        if (autoFindRobotsOnStart)
        {
            var robots = FindObjectsOfType<RobotController>();
            ShowForRobots(robots);
        }
    }

    void Update()
    {
        // Live refresh
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null && panels[i].IsBound) panels[i].Refresh();
        }
    }

    /// <summary>
    /// Binds the first N panels to the given robots and hides the rest.
    /// Call this when the round starts.
    /// </summary>
    public void ShowForRobots(IList<RobotController> robots, IList<string> displayNames = null)
    {
        // Clear all
        for (int i = 0; i < panels.Count; i++) panels[i].Clear();

        if (robots == null) return;

        int count = Mathf.Clamp(robots.Count, 0, panels.Count);
        for (int i = 0; i < count; i++)
        {
            var rc = robots[i];
            var name = (displayNames != null && i < displayNames.Count) ? displayNames[i] : rc.name;
            panels[i].Bind(rc, name);
        }
    }
}
