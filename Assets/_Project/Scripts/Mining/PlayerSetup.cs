using UnityEngine;

namespace DeepShift.Mining
{
    /// <summary>
    /// Bootstraps the required components on the player GameObject at runtime.
    /// Attach this to an empty GameObject; it self-assembles the physics and rendering setup.
    /// Remove this component (and configure components manually) before final art integration.
    /// </summary>
    public class PlayerSetup : MonoBehaviour
    {
        private void Awake()
        {
            // ── Inventory ─────────────────────────────────────────────────────
            if (GetComponent<PlayerInventory>() == null)
                gameObject.AddComponent<PlayerInventory>();

            // ── Rigidbody2D ───────────────────────────────────────────────────
            var rb              = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale     = 0f;  // top-down — no gravity
            rb.freezeRotation   = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // ── Collider ──────────────────────────────────────────────────────
            gameObject.AddComponent<BoxCollider2D>();

            // ── Renderer (placeholder) ────────────────────────────────────────
            // Generate a 1×1 white square sprite at 1 pixel-per-unit so it fills
            // exactly one tile. Tinted orange as a VEKTRA brand placeholder.
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f
            );

            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.color            = new Color(1f, 0.45f, 0f); // VEKTRA orange
            sr.sortingLayerName = "Player";
        }
    }
}
