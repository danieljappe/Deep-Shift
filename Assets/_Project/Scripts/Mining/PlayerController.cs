using UnityEngine;
using UnityEngine.InputSystem;
using DeepShift.Core;
using DeepShift.Player;

namespace DeepShift.Mining
{
    public class PlayerController : MonoBehaviour, IGameEventListener<int>
    {
        // ── Serialized config ─────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float _moveSpeed     = 5f;

        [Header("Movement / Grid")]
        [SerializeField] private MineGrid _mineGrid;

        [Header("Interact")]
        [SerializeField] private float     _interactRadius    = 1.2f;
        [SerializeField] private LayerMask _interactableLayers;

        [Header("Event Channels")]
        /// <summary>Int event raised by enemies when they deal damage to the player.</summary>
        [SerializeField] private GameEventSO_Int _onEnemyDealDamage;

        // ── Private state ─────────────────────────────────────────────────────

        private Vector2          _aimDirection      = Vector2.down;
        private Vector2          _exactAimDirection = Vector2.down;
        private Rigidbody2D      _rb;
        private CircleCollider2D _circleCollider;
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

        private void OnEnable()
        {
            _onEnemyDealDamage?.RegisterListener(this);
            Debug.Log($"[PlayerController] OnEnable — EnemyDealDamage SO = {(_onEnemyDealDamage != null ? _onEnemyDealDamage.name : "NULL")}");
        }

        private void OnDisable() => _onEnemyDealDamage?.UnregisterListener(this);

        private void OnDestroy()
        {
            _moveAction.Disable();
            _interactAction.Disable();
        }

        /// <summary>
        /// Receives damage from the EnemyDealDamage event and forwards it to
        /// <see cref="PlayerHealthSystem"/>. Enemies must NOT call TakeDamage directly —
        /// they raise the event and this handler applies it.
        /// </summary>
        public void OnEventRaised(int damage)
        {
            Debug.Log($"[PlayerController] OnEventRaised — damage={damage}, health null={_health == null}");
            _health?.TakeDamage(damage);
        }

        private void Start()
        {
            // Moved from Awake so PlayerSetup.Awake() has already run AddComponent<Rigidbody2D>()
            _rb             = GetComponent<Rigidbody2D>();
            _circleCollider = GetComponent<CircleCollider2D>();
            _camera         = UnityEngine.Camera.main;
            _health         = GetComponent<PlayerHealthSystem>();

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
        /// snapped to the nearest of 8 grid directions. Read by the drill.
        /// </summary>
        public Vector2 AimDirection => _aimDirection;

        /// <summary>
        /// Exact normalised world-space direction from the player to the mouse cursor.
        /// Read by ranged weapons for free-angle firing.
        /// </summary>
        public Vector2 ExactAimDirection => _exactAimDirection;

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

            _exactAimDirection = toMouse.normalized;

            // Snap normalised direction to nearest of 8 grid directions (used by drill)
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
        /// Uses the leading edge of the collider (centre ± radius) to determine which tile
        /// to test, so the player snaps flush to the wall boundary rather than stopping
        /// early when the centre rounds to the wrong tile.
        /// Checks each axis independently so the player can slide along walls.
        /// </summary>
        private Vector2 BlockMovement(Vector2 velocity)
        {
            float r   = _circleCollider != null ? _circleCollider.radius : 0.3f;
            Vector2 pos = _rb.position;

            if (velocity.x != 0f)
            {
                // Test the tile that the leading X edge currently occupies.
                // When that tile is solid the edge has reached the wall boundary.
                float edgeX = pos.x + Mathf.Sign(velocity.x) * r;
                int tileX   = Mathf.RoundToInt(edgeX);
                int tileY   = Mathf.RoundToInt(pos.y);
                if (IsSolid(tileX, tileY)) velocity.x = 0f;
            }

            if (velocity.y != 0f)
            {
                float edgeY = pos.y + Mathf.Sign(velocity.y) * r;
                int tileX   = Mathf.RoundToInt(pos.x);
                int tileY   = Mathf.RoundToInt(edgeY);
                if (IsSolid(tileX, tileY)) velocity.y = 0f;
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

    }
}
