using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays health/armor bars and names for active robots during the match.
/// Supports up to 2–4 player slots, configured manually in the Inspector.
/// </summary>
public class ArenaPlayerPanels : MonoBehaviour
{
    /// <summary>
    /// One UI panel slot showing a single robot’s health/armor.
    /// </summary>
    [System.Serializable]
    public class PanelSlot
    {
        [Header("UI References")]
        public GameObject root;     // Panel root GameObject
        public TMP_Text nameText;   // Robot name label
        public Slider healthSlider; // Health bar (0..1 normalized)
        public Slider armorSlider;  // Armor bar (0..1 normalized)

        private RobotController _rc;
        private RobotHealth _health;
        private float _maxHealth = 1f;
        private float _maxArmor = 1f;
        private bool _bound;

        /// <summary>True if this panel is currently bound to a robot.</summary>
        public bool IsBound => _bound;

        /// <summary>
        /// Binds this panel to a robot, wiring health/armor bars.
        /// </summary>
        public void Bind(RobotController rc, string displayName = null)
        {
            _rc = rc;
            _health = rc ? rc.GetHealth() : null;

            var stats = rc ? rc.GetStats() : null;
            _maxHealth = stats != null ? Mathf.Max(1f, stats.maxHealth) : 1f;
            _maxArmor = stats != null ? Mathf.Max(1f, stats.maxArmor) : 1f;

            if (nameText) nameText.text = string.IsNullOrWhiteSpace(displayName) ? rc.name : displayName;

            if (healthSlider) { healthSlider.minValue = 0f; healthSlider.maxValue = 1f; }
            if (armorSlider) { armorSlider.minValue = 0f; armorSlider.maxValue = 1f; }

            if (_health != null) _health.OnDeath += Refresh;

            if (root) root.SetActive(true);
            _bound = true;
            Refresh();
        }

        /// <summary>
        /// Updates slider values based on current robot health/armor.
        /// Called every frame from <see cref="ArenaPlayerPanels"/>.
        /// </summary>
        public void Refresh()
        {
            if (!_bound) return;

            bool eliminated = _health == null || _health.CurrentHealth <= 0f;
            float hp = eliminated ? 0f : Mathf.Clamp01(_health.CurrentHealth / _maxHealth);
            float ar = eliminated ? 0f : Mathf.Clamp01(_health.CurrentArmor / _maxArmor);

            if (healthSlider) healthSlider.value = hp;
            if (armorSlider) armorSlider.value = ar;
        }

        /// <summary>
        /// Unbinds this panel and hides it.
        /// </summary>
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
    [Tooltip("Fill with 2–4 slots in Inspector (root, name, sliders).")]
    [SerializeField] private List<PanelSlot> panels = new();

    [Header("Optional")]
    [Tooltip("If enabled, auto-finds robots in scene on Start and binds them.")]
    [SerializeField] private bool autoFindRobotsOnStart = false;

    private void Awake()
    {
        // Hide all panels at startup
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null) panels[i].Clear();
        }
    }

    private void Start()
    {
        if (autoFindRobotsOnStart)
        {
            // replaced obsolete FindObjectsOfType with FindObjectsByType
            var robots = FindObjectsByType<RobotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            ShowForRobots(robots);
        }
    }

    private void Update()
    {
        // Live refresh of health/armor bars
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null && panels[i].IsBound)
                panels[i].Refresh();
        }
    }

    /// <summary>
    /// Binds the first N panels to the given robots and hides the rest.
    /// Call this when the match starts.
    /// </summary>
    /// <param name="robots">Robots to show (will be clamped to available panels).</param>
    /// <param name="displayNames">Optional override display names for robots.</param>
    public void ShowForRobots(IList<RobotController> robots, IList<string> displayNames = null)
    {
        // Clear all first
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
