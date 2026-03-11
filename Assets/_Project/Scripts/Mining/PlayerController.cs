using UnityEngine;
using UnityEngine.InputSystem;
using DeepShift.Core;
using DeepShift.Player;

namespace DeepShift.Mining
{
    public class PlayerController : MonoBehaviour
    {
        // ── Serialized config ─────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float _moveSpeed     = 5f;

        [Header("Movement / Grid")]
        [SerializeField] private MineGrid _mineGrid;

        [Header("Interact")]
        [SerializeField] private float     _interactRadius    = 1.2f;
        [SerializeField] private LayerMask _interactableLayers;

        // ── Private state ─────────────────────────────────────────────────────

        private Vector2     _aimDirection = Vector2.down;
        private Rigidbody2D _rb;
        private UnityEngine.Camera _camera;

        private PlayerHealthSystem _health;

        // Buffered each Update, applied in FixedUpdate
        private Vector2 _pendingVelocity;

        // Input System actions — resolved once in Awake
        private InputAction _moveAction;
        private InputAction _interactAction;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Build lightweight ad-hoc actions so no Input Action Asset is required yet.
            // TODO: replace with a proper InputActionAsset / PlayerInput component in Phase 2.
            _moveAction = new InputAction("Move", InputActionType.Value,
                binding: "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w").With("Up",    "<Keyboard>/upArrow")
                .With("Down",  "<Keyboard>/s").With("Down",  "<Keyboard>/downArrow")
                .With("Left",  "<Keyboard>/a").With("Left",  "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

            _interactAction = new InputAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/buttonWest");

            _moveAction.Enable();
            _interactAction.Enable();
        }

        private void OnDestroy()
        {
            _moveAction.Disable();
            _interactAction.Disable();
        }

        private void Start()
        {
            // Moved from Awake so PlayerSetup.Awake() has already run AddComponent<Rigidbody2D>()
            _rb     = GetComponent<Rigidbody2D>();
            _camera = UnityEngine.Camera.main;
            _health = GetComponent<PlayerHealthSystem>();

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
            if (_health != null && _health.IsDead) return;

            UpdateAim();
            BuildMovement();
            HandleInteract();
        }

        private void FixedUpdate()
        {
            if (_health != null && _health.IsDead) return;
            _rb.MovePosition(_rb.position + _pendingVelocity * Time.fixedDeltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Normalised world-space direction from the player to the mouse cursor,
        /// snapped to the nearest of 8 grid directions. Read by tools (drill, etc.).
        /// </summary>
        public Vector2 AimDirection => _aimDirection;

        // ── Aim ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the mouse world position, rotates the player to face it, and updates
        /// <see cref="_aimDirection"/> snapped to the nearest of 8 grid directions.
        /// Convention: sprite art should face +Y (up); the -90° offset aligns Atan2's
        /// +X-is-zero reference with that convention.
        /// </summary>
        private void UpdateAim()
        {
            if (_camera == null || Mouse.current == null) return;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 mouseWorld  = _camera.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, -_camera.transform.position.z));

            Vector2 toMouse = (Vector2)mouseWorld - (Vector2)transform.position;
            if (toMouse.sqrMagnitude < 0.001f) return;

            // Rotate player sprite to face mouse (sprite art assumed to face +Y)
            float angle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);

            // Snap normalised direction to nearest of 8 grid directions
            _aimDirection = new Vector2(
                Mathf.RoundToInt(toMouse.normalized.x),
                Mathf.RoundToInt(toMouse.normalized.y)).normalized;
        }

        // ── Movement ──────────────────────────────────────────────────────────

        private void BuildMovement()
        {
            var input = _moveAction.ReadValue<Vector2>();

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

        // ── Interact ──────────────────────────────────────────────────────────

        private void HandleInteract()
        {
            if (!_interactAction.WasPressedThisFrame()) return;
            var hits = Physics2D.OverlapCircleAll(transform.position, _interactRadius, _interactableLayers);
            foreach (var hit in hits)
            {
                var interactable = hit.GetComponent<IInteractable>();
                if (interactable == null) continue;
                interactable.Interact();
                return;
            }
        }

        // ── Public movement API ───────────────────────────────────────────────

        /// <summary>
        /// Teleports the player to <paramref name="worldPos"/> immediately,
        /// syncing both the transform and the Rigidbody2D to avoid physics desync.
        /// </summary>
        public void TeleportTo(Vector3 worldPos)
        {
            transform.position = worldPos;
            if (_rb != null)
                _rb.position = new Vector2(worldPos.x, worldPos.y);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the tile at (<paramref name="x"/>, <paramref name="y"/>) blocks
        /// player movement: out-of-bounds cells, undestroyed rocks, and Wall tiles all block.
        /// Floor tiles and destroyed tiles are passable.
        /// </summary>
        private bool IsSolid(int x, int y)
        {
            if (_mineGrid == null) return false;

            MineGrid.TileInstance? tile = _mineGrid.GetTile(x, y);

            // Treat out-of-bounds as a solid wall
            if (tile == null) return true;

            // Destroyed tiles are open space
            if (tile.Value.isDestroyed) return false;

            // Walkable tiles (Floor, GasPocket, etc.) are always passable
            if (tile.Value.data.isWalkable) return false;

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
