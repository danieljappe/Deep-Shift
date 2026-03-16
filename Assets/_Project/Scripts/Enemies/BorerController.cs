// PREFAB SETUP:
// 1. Create empty GameObject "Borer"
// 2. Add SpriteRenderer (assign placeholder sprite, color: grey #8C8C8C)
// 3. Add CircleCollider2D (radius ~0.3, isTrigger = TRUE — enemies use event-based damage, not physics contact)
// 4. Add Rigidbody2D (Kinematic, Collision Detection: Continuous, no gravity)
// 5. Add BorerController component
// 6. Assign BorerDataSO asset to _data field
// 7. Assign event channel SOs (assets — safe to store in prefab):
//      _onDrillImpact     → Assets/_Project/ScriptableObjects/Events/Mining/DrillImpact.asset
//      _onHoistCalled     → Assets/_Project/ScriptableObjects/Events/Hoist/HoistCalled.asset
//      _onEnemyDealDamage → Assets/_Project/ScriptableObjects/Events/Enemies/EnemyDealDamage.asset
//      _onEnemyDied       → Assets/_Project/ScriptableObjects/Events/Enemies/EnemyDied.asset
//      _onOrePickedUp     → Assets/_Project/ScriptableObjects/Events/Mining/OrePickedUp.asset
// 8. MineGrid is resolved automatically at runtime — no Inspector wiring needed.
// 9. Save as prefab in Assets/_Project/Prefabs/Enemies/Borer.prefab

using System.Collections;
using UnityEngine;
using DeepShift.Core;
using DeepShift.Mining;
using DeepShift.Hoist;
using DeepShift.UI;

namespace DeepShift.Enemies
{
    /// <summary>
    /// Controls the Borer enemy — a wall-dwelling creature that detects drill vibrations
    /// and hunts the player or swarms the hoist terminal.
    ///
    /// State machine: IDLE → ALERT → AGGRO → DE_AGGRO → IDLE
    /// All player interaction is mediated through the SO event bus; no direct reference to
    /// PlayerController or PlayerHealthSystem.
    /// </summary>
    public class BorerController : MonoBehaviour,
        IGameEventListener<Vector2Int>,   // DrillImpact
        IGameEventListener,               // HoistCalled
        IDamageable
    {
        // ── Inspector fields ──────────────────────────────────────────────────

        [Header("Data")]
        /// <summary>Read-only stats asset. Never mutated at runtime.</summary>
        [SerializeField] private BorerDataSO _data;

        // MineGrid is resolved at runtime via FindFirstObjectByType — do not serialize.
        // Prefabs cannot hold references to scene objects.

        [Header("Event Channels — In")]
        /// <summary>Raised by DrillVibrationBroadcaster with the grid position of each drill hit.</summary>
        [SerializeField] private GameEventSO_Vector2Int _onDrillImpact;
        /// <summary>Raised when the player activates the hoist terminal.</summary>
        [SerializeField] private GameEventSO _onHoistCalled;

        [Header("Event Channels — Out")]
        /// <summary>Raised on lunge contact; PlayerController listens to call TakeDamage.</summary>
        [SerializeField] private GameEventSO_Int _onEnemyDealDamage;
        /// <summary>Raised when this Borer dies; used by other systems (e.g. ORION, threat budget).</summary>
        [SerializeField] private GameEventSO _onEnemyDied;
        /// <summary>Raised with chitin shard credit value on death drop; feeds the existing economy pipeline.</summary>
        [SerializeField] private GameEventSO_Int _onOrePickedUp;

        // ── Private state ─────────────────────────────────────────────────────

        private enum BorerState { Idle, Alert, Aggro, DeAggro }

        private BorerState  _state            = BorerState.Idle;
        private int         _maxHp;
        private int         _currentHp;
        private int         _vibrationCounter;
        private bool        _hoistSwarmActive;
        private float       _deAggroTimer;
        private bool        _deAggroTimerRunning;
        private float       _lungeCooldownRemaining;
        private bool        _isLunging;

        private MineGrid    _mineGrid;
        private Vector3     _spawnPosition;
        private Transform   _playerTransform;
        private Transform   _hoistTerminalTransform;

        private Rigidbody2D _rb;

        // Velocity buffered in Update, consumed in FixedUpdate — keeps physics in sync.
        private Vector2 _pendingVelocity;

        // Chitin shard credit value — placeholder until a proper drop table SO exists
        private const int ChitinShardCreditValue = 25;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            _maxHp         = _data != null ? _data.hp : 3;
            _currentHp     = _maxHp;

            // Resolve scene references at runtime — prefabs cannot store scene object refs
            _mineGrid = FindFirstObjectByType<MineGrid>();

            var playerObj = FindFirstObjectByType<PlayerController>();
            if (playerObj != null) _playerTransform = playerObj.transform;

            var hoistObj = FindFirstObjectByType<HoistTerminal>();
            if (hoistObj != null) _hoistTerminalTransform = hoistObj.transform;

            GetComponent<BorerHealthBar>()?.InitialiseBar(_maxHp, _currentHp);

            StartCoroutine(VibrationDecayRoutine());
        }

        private void OnEnable()
        {
            _onDrillImpact?.RegisterListener(this);
            _onHoistCalled?.RegisterListener(this);
        }

        private void OnDisable()
        {
            _onDrillImpact?.UnregisterListener(this);
            _onHoistCalled?.UnregisterListener(this);
        }

        private void Update()
        {
            _pendingVelocity = Vector2.zero;

            if (_state == BorerState.Aggro)
                UpdateAggro();
            else if (_state == BorerState.DeAggro)
                UpdateDeAggro();

            if (_lungeCooldownRemaining > 0f)
                _lungeCooldownRemaining -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            // Apply buffered velocity. Skip during lunge — LungeRoutine drives MovePosition directly.
            if (!_isLunging && _rb != null && _pendingVelocity != Vector2.zero)
                _rb.MovePosition(_rb.position + _pendingVelocity * Time.fixedDeltaTime);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Receives a DrillImpact event carrying the grid position of the destroyed tile.
        /// Triggers ALERT based on Manhattan distance and vibration accumulation rules.
        /// </summary>
        public void OnEventRaised(Vector2Int drillGridPos)
        {
            if (_state != BorerState.Idle) return;
            if (_data   == null)          return;

            Vector2Int myGridPos  = _mineGrid != null
                ? _mineGrid.WorldToGrid(transform.position)
                : Vector2Int.zero;

            int distance = Mathf.Abs(drillGridPos.x - myGridPos.x)
                         + Mathf.Abs(drillGridPos.y - myGridPos.y);

            if (distance <= _data.innerVibrationRadius)
            {
                TransitionToAlert();
                return;
            }

            if (distance <= _data.outerVibrationRadius)
            {
                _vibrationCounter++;
                if (_vibrationCounter >= _data.vibrationThreshold)
                    TransitionToAlert();
            }
        }

        /// <summary>
        /// Receives the HoistCalled event. All Borers on the floor immediately alert
        /// and swarm the hoist terminal regardless of distance.
        /// </summary>
        void IGameEventListener.OnEventRaised()
        {
            if (_state == BorerState.DeAggro || _state == BorerState.Aggro)
            {
                // Already active — just flag swarm so target switches to the hoist terminal.
                _hoistSwarmActive = true;
                return;
            }

            _hoistSwarmActive = true;
            TransitionToAlert();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies <paramref name="amount"/> damage to this Borer.
        /// If HP reaches zero, triggers the death sequence.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_currentHp <= 0) return;

            _currentHp -= amount;
            GetComponent<BorerHealthBar>()?.UpdateBar(_currentHp);
            FloatingText.Spawn(transform.position + Vector3.up * 0.3f, $"-{amount}");
            if (_currentHp <= 0)
                StartCoroutine(DieRoutine());
        }

        // ── State transitions ─────────────────────────────────────────────────

        private void TransitionToAlert()
        {
            _state = BorerState.Alert;
            // TODO: play alert visual (sprite colour flash or particle burst)
            // TODO: play alert SFX (chittering click-hiss)
            StartCoroutine(AlertRoutine());
        }

        private IEnumerator AlertRoutine()
        {
            float alertDuration = _data != null ? _data.alertDuration : 0.5f;
            yield return new WaitForSeconds(alertDuration);

            if (_state == BorerState.Alert)
                TransitionToAggro();
        }

        private void TransitionToAggro()
        {
            _state = BorerState.Aggro;
            _deAggroTimerRunning = false;
            _deAggroTimer        = 0f;
            GetComponent<BorerHealthBar>()?.SetAlwaysVisible(true);
            // TODO: play aggro visual (detach-from-wall animation state change)
        }

        private void TransitionToDeAggro()
        {
            _state            = BorerState.DeAggro;
            _hoistSwarmActive = false;
            GetComponent<BorerHealthBar>()?.SetAlwaysVisible(false);
            // TODO: play de-aggro visual
        }

        private void TransitionToIdle()
        {
            _state            = BorerState.Idle;
            _vibrationCounter = 0;
            _hoistSwarmActive = false;
            GetComponent<BorerHealthBar>()?.SetAlwaysVisible(false);
            // TODO: play idle/return-to-wall animation
        }

        // ── AGGRO update ──────────────────────────────────────────────────────

        private void UpdateAggro()
        {
            if (_data == null || _rb == null) return;

            Vector3 target = GetAggroTarget();

            // De-aggro check (skipped during hoist swarm)
            if (!_hoistSwarmActive && _playerTransform != null)
            {
                float distToPlayer = WorldDistanceTiles(transform.position, _playerTransform.position);

                if (distToPlayer > _data.deAggroRadius)
                {
                    if (!_deAggroTimerRunning)
                    {
                        _deAggroTimerRunning = true;
                        _deAggroTimer        = 0f;
                    }

                    _deAggroTimer += Time.deltaTime;

                    if (_deAggroTimer >= _data.deAggroDelay)
                    {
                        TransitionToDeAggro();
                        return;
                    }
                }
                else
                {
                    _deAggroTimerRunning = false;
                    _deAggroTimer        = 0f;
                }
            }

            // Lunge if in range and not on cooldown
            if (!_isLunging && _lungeCooldownRemaining <= 0f)
            {
                float distToTarget = Vector3.Distance(transform.position, target);
                if (distToTarget <= _data.lungeRange)
                {
                    StartCoroutine(LungeRoutine(target));
                    return;
                }
            }

            // Only close distance when outside lunge range — avoids pushing into the player
            // while waiting for the lunge cooldown.
            if (!_isLunging && Vector3.Distance(transform.position, target) > _data.lungeRange)
                MoveToward(target, _data.moveSpeed);
        }

        private Vector3 GetAggroTarget()
        {
            if (_hoistSwarmActive && _hoistTerminalTransform != null)
                return _hoistTerminalTransform.position;

            return _playerTransform != null ? _playerTransform.position : _spawnPosition;
        }

        private IEnumerator LungeRoutine(Vector3 target)
        {
            _isLunging = true;

            Vector3 start     = transform.position;
            Vector3 direction = (target - start).normalized;
            Vector3 lungeDest = start + direction * _data.lungeRange;

            const float lungeDuration = 0.15f;
            float elapsed = 0f;

            while (elapsed < lungeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lungeDuration);
                _rb.MovePosition(Vector3.Lerp(start, lungeDest, t));
                yield return null;
            }

            // Deal damage via event bus — PlayerController listens and calls TakeDamage
            _onEnemyDealDamage?.Raise(_data.damage);

            _isLunging                  = false;
            _lungeCooldownRemaining     = _data.lungeCooldown;
        }

        // ── DE_AGGRO update ───────────────────────────────────────────────────

        private void UpdateDeAggro()
        {
            if (_data == null) return;

            if (Vector3.Distance(transform.position, _spawnPosition) < 0.1f)
            {
                _pendingVelocity = Vector2.zero;
                _rb.MovePosition(_spawnPosition);
                TransitionToIdle();
                return;
            }

            MoveToward(_spawnPosition, _data.moveSpeed);
        }

        // ── Movement helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Buffers velocity toward <paramref name="target"/> at <paramref name="speed"/> units/s
        /// into <see cref="_pendingVelocity"/> for application in <see cref="FixedUpdate"/>.
        /// Blocks per-axis against solid tiles (same sliding logic as PlayerController).
        /// </summary>
        private void MoveToward(Vector3 target, float speed)
        {
            Vector2 direction = ((Vector2)target - (Vector2)transform.position).normalized;
            Vector2 velocity  = direction * speed;

            if (_mineGrid != null)
                velocity = BlockMovement(velocity);

            _pendingVelocity = velocity;
        }

        private Vector2 BlockMovement(Vector2 velocity)
        {
            if (_mineGrid == null) return velocity;

            Vector2Int cell = _mineGrid.WorldToGrid(transform.position);

            if (velocity.x != 0f)
            {
                int nx = cell.x + (int)Mathf.Sign(velocity.x);
                if (IsSolid(nx, cell.y)) velocity.x = 0f;
            }

            if (velocity.y != 0f)
            {
                int ny = cell.y + (int)Mathf.Sign(velocity.y);
                if (IsSolid(cell.x, ny)) velocity.y = 0f;
            }

            return velocity;
        }

        private bool IsSolid(int x, int y)
        {
            MineGrid.TileInstance? tile = _mineGrid.GetTile(x, y);
            if (tile == null)                    return true;
            if (tile.Value.isDestroyed)           return false;
            if (tile.Value.data.isWalkable)       return false;
            return true;
        }

        // ── Vibration decay ───────────────────────────────────────────────────

        private IEnumerator VibrationDecayRoutine()
        {
            while (true)
            {
                float interval = _data != null ? _data.vibrationDecayInterval : 3f;
                yield return new WaitForSeconds(interval);

                if (_state == BorerState.Idle && _vibrationCounter > 0)
                    _vibrationCounter--;
            }
        }

        // ── Death ─────────────────────────────────────────────────────────────

        private IEnumerator DieRoutine()
        {
            // Prevent double-death
            _currentHp = 0;

            _onEnemyDied?.Raise();

            // Roll chitin shard drop
            if (_data != null && Random.value < _data.chitinShardDropChance)
            {
                // TODO: spawn a proper ChitinShardPickup prefab when art pipeline is ready.
                // For now, award credits directly via the OrePickedUp event channel.
                _onOrePickedUp?.Raise(ChitinShardCreditValue);
                FloatingText.Spawn(transform.position, "Chitin Shard");
            }

            // TODO: play death animation/particle before destroy
            yield return new WaitForSeconds(0.3f);
            Destroy(gameObject);
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private float WorldDistanceTiles(Vector3 a, Vector3 b)
        {
            if (_mineGrid == null) return Vector3.Distance(a, b);
            Vector2Int ga = _mineGrid.WorldToGrid(a);
            Vector2Int gb = _mineGrid.WorldToGrid(b);
            return Mathf.Abs(ga.x - gb.x) + Mathf.Abs(ga.y - gb.y);
        }
    }
}
