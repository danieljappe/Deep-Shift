using UnityEngine;
using DeepShift.UI;

namespace DeepShift.Weapons
{
    /// <summary>
    /// World-space ammo pickup. Spawned in code via <see cref="SpawnAt"/>; no prefab required.
    /// Collected automatically when any GameObject with a <see cref="RangedWeaponController"/> overlaps.
    /// Note: <c>GetComponent</c> returns disabled components, so ammo is collected even when the
    /// ranged weapon slot is inactive.
    /// </summary>
    public class AmmoPickup : MonoBehaviour
    {
        private int _ammoAmount;

        // ── Static factory ─────────────────────────────────────────────────────

        /// <summary>
        /// Spawns an ammo pickup at <paramref name="worldPos"/> containing <paramref name="amount"/> bolts.
        /// </summary>
        public static void SpawnAt(Vector3 worldPos, int amount)
        {
            var go = new GameObject("AmmoPickup");
            go.transform.position = worldPos;

            // Visual — 1×1 white square, cyan tint
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 2f);

            var sr              = go.AddComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.color            = Color.cyan;
            sr.sortingLayerName = "Player";
            sr.sortingOrder     = 5;

            var rb          = go.AddComponent<Rigidbody2D>();
            rb.bodyType     = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.35f;

            var pickup = go.AddComponent<AmmoPickup>();
            pickup.Initialize(amount);
        }

        // ── Instance methods ───────────────────────────────────────────────────

        /// <summary>Stores the ammo value. Called by <see cref="SpawnAt"/> immediately after AddComponent.</summary>
        public void Initialize(int amount) => _ammoAmount = amount;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var weapon = other.GetComponent<RangedWeaponController>();
            if (weapon == null) return;

            weapon.AddAmmo(_ammoAmount);
            FloatingText.Spawn(transform.position, $"+{_ammoAmount} Bolts");
            Destroy(gameObject);
        }
    }
}
