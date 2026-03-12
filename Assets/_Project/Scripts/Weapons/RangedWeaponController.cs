using UnityEngine;
using UnityEngine.InputSystem;
using DeepShift.Core;
using DeepShift.Mining;
using DeepShift.Player;

namespace DeepShift.Weapons
{
    /// <summary>
    /// Handles ranged weapon input, ammo tracking, and projectile spawning.
    /// Attach to the same GameObject as <see cref="PlayerController"/> and
    /// <see cref="PlayerHealthSystem"/>. Start with this component disabled;
    /// <see cref="HotbarController"/> enables/disables it based on slot selection.
    /// </summary>
    public class RangedWeaponController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WeaponDataSO _data;

        [Header("References")]
        [SerializeField] private MineGrid _mineGrid;

        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Int _onWeaponAmmoChanged;
        [SerializeField] private GameEventSO_Int _onWeaponFired;

        // ── Private state ──────────────────────────────────────────────────────

        private InputAction        _fireAction;
        private float              _fireCooldown;
        private int                _currentAmmo;
        private PlayerController   _player;
        private PlayerHealthSystem _health;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _fireAction = new InputAction("Fire", InputActionType.Button);
            _fireAction.AddBinding("<Mouse>/leftButton");
            _fireAction.AddBinding("<Gamepad>/buttonSouth");

            // Resolved here so we avoid FindFirstObjectByType per-frame
            _player = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealthSystem>();
        }

        private void OnEnable()
        {
            _fireAction.Enable();
            _currentAmmo = _data != null ? _data.maxAmmo : 0;
            _onWeaponAmmoChanged?.Raise(_currentAmmo);
        }

        private void OnDisable()
        {
            _fireAction.Disable();
        }

        private void Update()
        {
            if (_health != null && _health.IsDead) return;

            if (_fireCooldown > 0f)
                _fireCooldown -= Time.deltaTime;

            if (_fireAction.WasPressedThisFrame())
                TryFire();
        }

        // ── Firing ────────────────────────────────────────────────────────────

        private void TryFire()
        {
            if (_data == null)                  return;
            if (_currentAmmo <= 0)              return;
            if (_fireCooldown > 0f)             return;

            _currentAmmo--;
            _fireCooldown = 1f / _data.fireRate;

            SpawnProjectile(_player != null ? _player.ExactAimDirection : Vector2.right);
            _onWeaponAmmoChanged?.Raise(_currentAmmo);
            _onWeaponFired?.Raise(_data.damage);
        }

        private void SpawnProjectile(Vector2 dir)
        {
            var go = new GameObject("Bolt");
            go.transform.position = transform.position;

            // Visual — 1×1 white square, cyan tint
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 2f);

            var sr                = go.AddComponent<SpriteRenderer>();
            sr.sprite             = sprite;
            sr.color              = Color.cyan;
            sr.sortingLayerName   = "Player";
            sr.sortingOrder       = 8;

            // Dynamic Rigidbody2D — non-kinematic so OnTriggerEnter2D fires on trigger overlap
            var rb                        = go.AddComponent<Rigidbody2D>();
            rb.bodyType                   = RigidbodyType2D.Dynamic;
            rb.gravityScale               = 0f;
            rb.collisionDetectionMode     = CollisionDetectionMode2D.Continuous;
            rb.constraints                = RigidbodyConstraints2D.FreezeRotation;

            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.15f;

            var projectile = go.AddComponent<Projectile>();
            projectile.Initialize(dir, _data.projectileSpeed, _data.damage, _mineGrid);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds ammo up to the weapon's <see cref="WeaponDataSO.maxAmmo"/> cap and raises the ammo changed event.
        /// </summary>
        public void AddAmmo(int amount)
        {
            int cap = _data != null ? _data.maxAmmo : 0;
            _currentAmmo = Mathf.Min(_currentAmmo + amount, cap);
            _onWeaponAmmoChanged?.Raise(_currentAmmo);
        }
    }
}
