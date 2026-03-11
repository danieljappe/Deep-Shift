using UnityEngine;
using DeepShift.Core;
using DeepShift.Mining;
using DeepShift.Player;

namespace DeepShift.Hazards
{
    /// <summary>
    /// Handles two sources of gas damage:
    /// <list type="bullet">
    ///   <item>
    ///     <term>Burst</term>
    ///     <description>Subscribes to <see cref="_onHazardTriggered"/>. Fires when the player
    ///     drills a tile adjacent to a gas pocket — deals <see cref="_gasBurstDamage"/> instantly
    ///     and displays a "GAS POCKET BREACHED" warning.</description>
    ///   </item>
    ///   <item>
    ///     <term>Tick</term>
    ///     <description>Every <see cref="_tickInterval"/> seconds, checks whether the player
    ///     is standing on a <see cref="GasTileDataSO"/> cell and deals <see cref="_gasDamagePerTick"/>.</description>
    ///   </item>
    /// </list>
    /// Both sources display a brief red screen tint.
    /// Attach to any active GameObject in the Mine scene. Assign <see cref="_mineGrid"/> and
    /// <see cref="_onHazardTriggered"/> in the Inspector.
    /// </summary>
    public class GasDamageSystem : MonoBehaviour, IGameEventListener
    {
        [Header("Settings")]
        [SerializeField] private int   _gasDamagePerTick = 5;   // applied once per _tickInterval
        [SerializeField] private int   _gasBurstDamage   = 20;  // applied when drilling adjacent to gas
        [SerializeField] private float _tickInterval     = 1f;  // seconds between standing-gas checks

        [Header("References")]
        [SerializeField] private MineGrid _mineGrid;

        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO _onHazardTriggered;

        // ── Cached scene references ────────────────────────────────────────────

        private PlayerHealthSystem _playerHealth;
        private Transform          _playerTransform;

        // ── Timers ─────────────────────────────────────────────────────────────

        private float _tickTimer;
        private float _breachMessageTimer;
        private bool  _showBreachMessage;

        // ── Cached GUI style ───────────────────────────────────────────────────

        private GUIStyle _breachStyle;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            _playerHealth = FindFirstObjectByType<PlayerHealthSystem>();

            var pc = FindFirstObjectByType<DeepShift.Mining.PlayerController>();
            if (pc != null) _playerTransform = pc.transform;

            _breachStyle = new GUIStyle
            {
                fontSize  = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _breachStyle.normal.textColor = new Color(1f, 0.5f, 0f); // VEKTRA orange
        }

        private void OnEnable()  => _onHazardTriggered?.RegisterListener(this);
        private void OnDisable() => _onHazardTriggered?.UnregisterListener(this);

        private void Update()
        {
            if (_breachMessageTimer > 0f)
            {
                _breachMessageTimer -= Time.deltaTime;
                if (_breachMessageTimer <= 0f)
                    _showBreachMessage = false;
            }

            TickStandingGas();
        }

        // ── IGameEventListener (HazardTriggered) ──────────────────────────────

        /// <summary>
        /// Called when the player drills a tile adjacent to a gas pocket.
        /// Applies <see cref="_gasBurstDamage"/> instantly and shows the breach warning.
        /// </summary>
        public void OnEventRaised()
        {
            if (_playerHealth == null) return;

            _playerHealth.TakeDamage(_gasBurstDamage);
            _breachMessageTimer = 1.5f;
            _showBreachMessage  = true;
            Debug.Log($"[GasDamageSystem] GAS POCKET BREACHED — {_gasBurstDamage} damage.");
        }

        // ── Standing gas tick ─────────────────────────────────────────────────

        private void TickStandingGas()
        {
            if (_playerHealth == null || _playerTransform == null || _mineGrid == null) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < _tickInterval) return;
            _tickTimer = 0f;

            Vector2Int cell = _mineGrid.WorldToGrid(_playerTransform.position);
            MineGrid.TileInstance? tile = _mineGrid.GetTile(cell.x, cell.y);

            if (tile == null || tile.Value.isDestroyed) return;
            if (!(tile.Value.data is GasTileDataSO)) return;

            _playerHealth.TakeDamage(_gasDamagePerTick);
            Debug.Log($"[GasDamageSystem] Gas tick — {_gasDamagePerTick} damage.");
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            // Red screen tint is now handled by PlayerHealthSystem.OnGUI for all damage sources.
            // GasDamageSystem only owns the breach text message.
            if (!_showBreachMessage || _breachStyle == null) return;

            GUI.Label(
                new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.65f, 480f, 50f),
                "GAS POCKET BREACHED",
                _breachStyle);
        }
    }
}
