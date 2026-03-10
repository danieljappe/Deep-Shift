using UnityEngine;
using UnityEngine.InputSystem;
using DeepShift.Core;

namespace DeepShift.Mining
{
    public class PlayerController : MonoBehaviour
    {
        // ── Serialized config ─────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float _moveSpeed     = 5f;

        [Header("Drill")]
        [SerializeField] private float _drillCooldown = 0.3f;
        [SerializeField] private int   _drillPower    = 1;
        [SerializeField] private MineGrid _mineGrid;

        [Header("Health")]
        [SerializeField] private int _maxHealth = 100;

        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Float _onPlayerHealthChanged;
        [SerializeField] private GameEventSO       _onPlayerDied;

        // ── Private state ─────────────────────────────────────────────────────

        private Vector2    _lastMoveDirection  = Vector2.down;
        private float      _drillCooldownTimer = 0f;
        private Rigidbody2D _rb;

        private int _currentHealth;
        private bool _isDead;

        // Buffered each Update, applied in FixedUpdate
        private Vector2 _pendingVelocity;

        // Input System actions — resolved once in Awake
        private InputAction _moveAction;
        private InputAction _drillAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _currentHealth = _maxHealth;

            // Build lightweight ad-hoc actions so no Input Action Asset is required yet.
            // TODO: replace with a proper InputActionAsset / PlayerInput component in Phase 2.
            _moveAction = new InputAction("Move", InputActionType.Value,
                binding: "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w").With("Up",    "<Keyboard>/upArrow")
                .With("Down",  "<Keyboard>/s").With("Down",  "<Keyboard>/downArrow")
                .With("Left",  "<Keyboard>/a").With("Left",  "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

            _drillAction = new InputAction("Drill", InputActionType.Button);
            _drillAction.AddBinding("<Keyboard>/space");
            _drillAction.AddBinding("<Gamepad>/buttonSouth");

            _moveAction.Enable();
            _drillAction.Enable();
        }

        private void OnDestroy()
        {
            _moveAction.Disable();
            _drillAction.Disable();
        }

        private void Start()
        {
            // Moved from Awake so PlayerSetup.Awake() has already run AddComponent<Rigidbody2D>()
            _rb = GetComponent<Rigidbody2D>();

            if (_mineGrid == null) return;

            var bootstrap = FindFirstObjectByType<MineTestBootstrap>();
            if (bootstrap != null)
            {
                Vector3 spawn = bootstrap.GetSpawnPosition();
                transform.position = spawn;
                _rb.position = new Vector2(spawn.x, spawn.y);
            }
        }

        private void Update()
        {
            if (_isDead) return;

            _drillCooldownTimer -= Time.deltaTime;

            BuildMovement();
            HandleDrill();
        }

        private void FixedUpdate()
        {
            if (_isDead) return;
            _rb.MovePosition(_rb.position + _pendingVelocity * Time.fixedDeltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies <paramref name="amount"/> damage to the player.
        /// Raises <see cref="_onPlayerHealthChanged"/> with the new health ratio (0–1).
        /// Raises <see cref="_onPlayerDied"/> and disables input if health reaches zero.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            _onPlayerHealthChanged?.Raise((float)_currentHealth / _maxHealth);

            if (_currentHealth <= 0)
                Die();
        }

        /// <summary>
        /// Restores <paramref name="amount"/> health, clamped to <see cref="_maxHealth"/>.
        /// Raises <see cref="_onPlayerHealthChanged"/> with the updated ratio.
        /// </summary>
        public void Heal(int amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            _onPlayerHealthChanged?.Raise((float)_currentHealth / _maxHealth);
        }

        // ── Movement ──────────────────────────────────────────────────────────

        private void BuildMovement()
        {
            var input = _moveAction.ReadValue<Vector2>();

            if (input.sqrMagnitude > 0f)
                _lastMoveDirection = input.normalized;

            Vector2 velocity = input * _moveSpeed;

            if (_mineGrid != null && velocity != Vector2.zero)
                velocity = BlockMovement(velocity);

            _pendingVelocity = velocity;
        }

        /// <summary>
        /// Zeroes out velocity components that would move the player into a solid tile.
        /// Checks each axis independently so the player can slide along walls.
        /// </summary>
        private Vector2 BlockMovement(Vector2 velocity)
        {
            Vector2Int cell = _mineGrid.WorldToGrid(transform.position);

            if (velocity.x != 0f)
            {
                int nx = cell.x + (int)Mathf.Sign(velocity.x);
                bool solid = IsSolid(nx, cell.y);
                Debug.Log($"[Player] Checking tile ({nx},{cell.y}) for X movement — solid: {solid}, tile: {TileNameAt(nx, cell.y)}");
                if (solid) velocity.x = 0f;
            }

            if (velocity.y != 0f)
            {
                int ny = cell.y + (int)Mathf.Sign(velocity.y);
                bool solid = IsSolid(cell.x, ny);
                Debug.Log($"[Player] Checking tile ({cell.x},{ny}) for Y movement — solid: {solid}, tile: {TileNameAt(cell.x, ny)}");
                if (solid) velocity.y = 0f;
            }

            return velocity;
        }

        // ── Drill ─────────────────────────────────────────────────────────────

        private void HandleDrill()
        {
            bool drillInput = _drillAction.IsPressed();
            if (!drillInput || _drillCooldownTimer > 0f || _mineGrid == null) return;

            _drillCooldownTimer = _drillCooldown;

            Vector2Int cell = _mineGrid.WorldToGrid(transform.position);
            Vector2Int dir  = new Vector2Int(
                Mathf.RoundToInt(_lastMoveDirection.x),
                Mathf.RoundToInt(_lastMoveDirection.y)
            );

            int tx = cell.x + dir.x;
            int ty = cell.y + dir.y;

            MineGrid.TileInstance? target = _mineGrid.GetTile(tx, ty);
            if (target == null || !target.Value.data.isDestructible) return;

            for (int i = 0; i < _drillPower; i++)
            {
                bool destroyed = _mineGrid.HitTile(tx, ty);
                if (destroyed) break; // tile gone, further hits are wasted
            }
        }

        // ── Health ────────────────────────────────────────────────────────────

        private void Die()
        {
            _isDead        = true;
            _pendingVelocity = Vector2.zero;
            _onPlayerDied?.Raise();
            Debug.Log("[PlayerController] Player died.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the tile at (<paramref name="x"/>, <paramref name="y"/>) blocks
        /// player movement: out-of-bounds cells, undestroyed rocks, and Wall tiles all block.
        /// Floor tiles and destroyed tiles are passable.
        /// </summary>
        /// <remarks>
        /// TODO: replace the tileName string check with a dedicated <c>isWalkable</c> bool
        /// on <see cref="TileDataSO"/> once the data layer is iterated on.
        /// </remarks>
        private bool IsSolid(int x, int y)
        {
            if (_mineGrid == null) return false;

            MineGrid.TileInstance? tile = _mineGrid.GetTile(x, y);

            // Treat out-of-bounds as a solid wall
            if (tile == null) return true;

            // Destroyed tiles are open space
            if (tile.Value.isDestroyed) return false;

            // Floor tiles are always walkable
            if (tile.Value.data.tileName == "Floor") return false;

            // Everything else (rocks, walls) blocks
            return true;
        }

        private string TileNameAt(int x, int y)
        {
            MineGrid.TileInstance? tile = _mineGrid.GetTile(x, y);
            if (tile == null) return "OUT_OF_BOUNDS";
            if (tile.Value.isDestroyed) return "DESTROYED";
            return tile.Value.data != null ? tile.Value.data.tileName : "NULL_DATA";
        }
    }
}
