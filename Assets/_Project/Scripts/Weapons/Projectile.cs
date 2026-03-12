using UnityEngine;
using DeepShift.Core;
using DeepShift.Mining;
using DeepShift.UI;

namespace DeepShift.Weapons
{
    /// <summary>
    /// Moves in a straight line, destroys itself on solid tiles or after lifetime expires,
    /// and applies damage to any <see cref="IDamageable"/> it overlaps.
    /// Spawned entirely in code by <see cref="RangedWeaponController"/> — no prefab required.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        // ── Private state ──────────────────────────────────────────────────────

        private Vector2    _direction;
        private float      _speed;
        private int        _damage;
        private MineGrid   _mineGrid;
        private float      _elapsed;
        private float      _maxLifetime = 6f;
        private Rigidbody2D _rb;

        // ── Public factory method ──────────────────────────────────────────────

        /// <summary>
        /// Initialises the projectile. Must be called immediately after AddComponent.
        /// </summary>
        public void Initialize(Vector2 dir, float speed, int damage, MineGrid grid)
        {
            _direction = dir.normalized;
            _speed     = speed;
            _damage    = damage;
            _mineGrid  = grid;
            _rb        = GetComponent<Rigidbody2D>();
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Update()
        {
            if (_rb == null) return;

            // Move
            _rb.MovePosition(_rb.position + _direction * _speed * Time.deltaTime);

            // Tile collision — destroy on solid, un-destroyed tile
            if (_mineGrid != null)
            {
                Vector2Int cell = _mineGrid.WorldToGrid(transform.position);
                MineGrid.TileInstance? tile = _mineGrid.GetTile(cell.x, cell.y);
                if (tile.HasValue && !tile.Value.isDestroyed && tile.Value.data != null && !tile.Value.data.isWalkable)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // Lifetime
            _elapsed += Time.deltaTime;
            if (_elapsed >= _maxLifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // No self-hit
            if (other.GetComponent<PlayerController>() != null) return;

            var dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(_damage);
                FloatingText.Spawn(transform.position, $"-{_damage}");
                Destroy(gameObject);
            }
        }
    }
}
