using UnityEngine;
using DeepShift.Core;

namespace DeepShift.Player
{
    /// <summary>
    /// Owns the player's HP state for the current shift.
    /// HP resets to full on each floor transition (<see cref="_onPlayerFloorChanged"/>)
    /// and on each new shift (<see cref="_onShiftStarted"/>).
    /// Attach to the same GameObject as <see cref="DeepShift.Mining.PlayerController"/>.
    /// </summary>
    public class PlayerHealthSystem : MonoBehaviour, IGameEventListener, IGameEventListener<int>
    {
        [Header("Health")]
        [SerializeField] private int _maxHealth = 100;

        [Header("Event Channels — Raise")]
        [SerializeField] private GameEventSO_Float _onPlayerHealthChanged;
        [SerializeField] private GameEventSO       _onPlayerDied;

        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO_Int _onPlayerFloorChanged; // reset HP on floor transition
        [SerializeField] private GameEventSO     _onShiftStarted;       // reset HP on new shift

        // ── State ──────────────────────────────────────────────────────────────

        private int   _currentHealth;
        private float _damageFlashTimer;

        /// <summary>True once HP reaches zero; cleared by <see cref="ResetToFull"/>.</summary>
        public bool IsDead { get; private set; }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake() => _currentHealth = _maxHealth;

        private void Update()
        {
            if (_damageFlashTimer > 0f)
                _damageFlashTimer -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_damageFlashTimer <= 0f) return;
            GUI.color = new Color(1f, 0f, 0f, 0.25f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void OnEnable()
        {
            _onPlayerFloorChanged?.RegisterListener(this);
            _onShiftStarted?.RegisterListener(this);
        }

        private void OnDisable()
        {
            _onPlayerFloorChanged?.UnregisterListener(this);
            _onShiftStarted?.UnregisterListener(this);
        }

        // ── IGameEventListener (ShiftStarted) ─────────────────────────────────

        /// <summary>Called when ShiftStarted fires — resets HP to full.</summary>
        public void OnEventRaised() => ResetToFull();

        // ── IGameEventListener<int> (PlayerFloorChanged) ──────────────────────

        /// <summary>Called when PlayerFloorChanged fires — resets HP to full.</summary>
        public void OnEventRaised(int value) => ResetToFull();

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Applies <paramref name="amount"/> damage to the player, clamped to zero.
        /// Raises <see cref="_onPlayerHealthChanged"/> with the updated ratio (0–1).
        /// Raises <see cref="_onPlayerDied"/> and sets <see cref="IsDead"/> if HP reaches zero.
        /// <br/>
        /// TODO: enemies call this method when attacking the player.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (IsDead) return;

            _currentHealth    = Mathf.Max(0, _currentHealth - amount);
            _damageFlashTimer = 0.4f;
            _onPlayerHealthChanged?.Raise((float)_currentHealth / _maxHealth);

            if (_currentHealth <= 0)
                Die();
        }

        /// <summary>
        /// Restores <paramref name="amount"/> HP, clamped to <see cref="_maxHealth"/>.
        /// Raises <see cref="_onPlayerHealthChanged"/> with the updated ratio.
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDead) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            _onPlayerHealthChanged?.Raise((float)_currentHealth / _maxHealth);
        }

        /// <summary>Returns the player's current HP.</summary>
        public int GetCurrentHP() => _currentHealth;

        /// <summary>
        /// Resets HP to maximum and clears <see cref="IsDead"/>.
        /// Called on floor transition and on beginning a new shift.
        /// </summary>
        public void ResetToFull()
        {
            IsDead         = false;
            _currentHealth = _maxHealth;
            _onPlayerHealthChanged?.Raise(1f);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void Die()
        {
            IsDead = true;
            _onPlayerDied?.Raise();
            Debug.Log("[PlayerHealthSystem] Player died.");

            // TODO: ORION — sardonic corporate sympathy line.
            // Tone: "Oh dear. VEKTRA notes your performance with concern."
        }
    }
}
