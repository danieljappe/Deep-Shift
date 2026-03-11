using UnityEngine;
using UnityEngine.InputSystem;
using DeepShift.Core;

namespace DeepShift.Mining
{
    /// <summary>
    /// Handles charged drill input (Mouse1 / Gamepad South). The player must hold the button for
    /// <see cref="_chargeTime"/> seconds while facing a destructible tile to break it.
    /// A world-space progress bar is displayed above the target tile while charging.
    /// When an ore tile is destroyed, an <see cref="OrePickup"/> is spawned at that position.
    /// Attach to the same GameObject as <see cref="PlayerController"/>.
    /// </summary>
    public class DrillController : MonoBehaviour
    {
        [Header("Drill Settings")]
        [SerializeField] private float _chargeTime = 0.8f;

        [Header("References")]
        [SerializeField] private MineGrid _mineGrid;

        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Int _onOrePickedUp;

        // ── Private state ──────────────────────────────────────────────────────

        private PlayerController _player;
        private InputAction      _drillAction;
        private float            _chargeProgress;

        // Charge indicator — built once in Awake, repositioned each frame
        private GameObject _indicatorRoot;
        private Transform  _indicatorFill;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            _drillAction = new InputAction("Drill", InputActionType.Button);
            _drillAction.AddBinding("<Mouse>/leftButton");
            _drillAction.AddBinding("<Gamepad>/buttonSouth");
            _drillAction.Enable();

            BuildChargeIndicator();
        }

        private void OnDestroy()
        {
            _drillAction.Disable();
            if (_indicatorRoot != null)
                Destroy(_indicatorRoot);
        }

        private void Update()
        {
            if (_player == null || _mineGrid == null) return;

            Vector2Int cell   = _mineGrid.WorldToGrid(transform.position);
            Vector2Int dir    = new Vector2Int(
                Mathf.RoundToInt(_player.AimDirection.x),
                Mathf.RoundToInt(_player.AimDirection.y));
            int tx = cell.x + dir.x;
            int ty = cell.y + dir.y;

            MineGrid.TileInstance? target = _mineGrid.GetTile(tx, ty);
            bool canDrill = target.HasValue
                         && !target.Value.isDestroyed
                         && target.Value.data != null
                         && target.Value.data.isDestructible;

            if (_drillAction.IsPressed() && canDrill)
            {
                _chargeProgress += Time.deltaTime;
                SetIndicator(tx, ty, _chargeProgress / _chargeTime);

                if (_chargeProgress >= _chargeTime)
                {
                    ExecuteDrill(tx, ty);
                    _chargeProgress = 0f;
                    HideIndicator();
                }
            }
            else
            {
                _chargeProgress = 0f;
                HideIndicator();
            }
        }

        // ── Drill execution ────────────────────────────────────────────────────

        private void ExecuteDrill(int tx, int ty)
        {
            MineGrid.TileInstance? before = _mineGrid.GetTile(tx, ty);
            if (!before.HasValue) return;

            OreDataSO ore       = before.Value.data.containedOre;
            bool      destroyed = _mineGrid.HitTile(tx, ty);

            if (destroyed && ore != null)
                SpawnOrePickup(_mineGrid.GridToWorld(tx, ty), ore);
        }

        private void SpawnOrePickup(Vector3 worldPos, OreDataSO ore)
        {
            var go = new GameObject($"OrePickup_{ore.displayName}");
            go.transform.position = worldPos;

            // Visual — coloured square the same shade as the ore tile
            var sr      = go.AddComponent<SpriteRenderer>();
            sr.sprite   = ore.sprite != null ? ore.sprite : BuildSquareSprite();
            sr.color    = ore.tileColor;
            sr.sortingLayerName = "Player";
            sr.sortingOrder     = 5;

            // Kinematic Rigidbody2D required so OnTriggerEnter2D fires reliably
            // when the player's dynamic body (using MovePosition) enters the trigger.
            var rb          = go.AddComponent<Rigidbody2D>();
            rb.bodyType     = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            // Trigger collider for auto-collect
            var col       = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.35f;

            // Pickup behaviour
            var pickup = go.AddComponent<OrePickup>();
            pickup.Initialize(ore, _onOrePickedUp);
        }

        // ── Charge indicator ───────────────────────────────────────────────────

        /// <summary>Creates the two-quad (background + fill) charge indicator once at startup.</summary>
        private void BuildChargeIndicator()
        {
            _indicatorRoot = new GameObject("DrillChargeIndicator");
            _indicatorRoot.SetActive(false);

            // Background bar
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(_indicatorRoot.transform, false);
            bg.transform.localScale    = new Vector3(0.9f, 0.14f, 1f);
            bg.transform.localPosition = Vector3.zero;
            Destroy(bg.GetComponent<Collider>());

            var bgMat   = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            bgMat.color = new Color(0.08f, 0.08f, 0.08f);
            var bgR     = bg.GetComponent<Renderer>();
            bgR.material          = bgMat;
            bgR.sortingLayerName  = "Player";
            bgR.sortingOrder      = 15;

            // Fill bar (scales from 0 → 0.9 on X)
            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(_indicatorRoot.transform, false);
            fill.transform.localScale    = new Vector3(0f, 0.10f, 1f);
            fill.transform.localPosition = new Vector3(-0.45f, 0f, -0.01f);
            Destroy(fill.GetComponent<Collider>());

            var fillMat   = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            fillMat.color = new Color(1f, 0.65f, 0f); // orange
            var fillR     = fill.GetComponent<Renderer>();
            fillR.material         = fillMat;
            fillR.sortingLayerName = "Player";
            fillR.sortingOrder     = 16;

            _indicatorFill = fill.transform;
        }

        private void SetIndicator(int tx, int ty, float fillAmount)
        {
            if (_indicatorRoot == null) return;

            _indicatorRoot.SetActive(true);
            _indicatorRoot.transform.position =
                _mineGrid.GridToWorld(tx, ty) + new Vector3(0f, 0.62f, 0f);

            float clamped = Mathf.Clamp01(fillAmount);

            // Grow the fill quad left-to-right: pivot is at its centre so
            // we offset X so the left edge stays fixed at -0.45.
            _indicatorFill.localScale    = new Vector3(clamped * 0.9f, 0.10f, 1f);
            _indicatorFill.localPosition = new Vector3(-0.45f + clamped * 0.45f, 0f, -0.01f);
        }

        private void HideIndicator()
        {
            _indicatorRoot?.SetActive(false);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Creates a 1×1 white square sprite used as the ore pickup fallback visual.</summary>
        private static Sprite BuildSquareSprite()
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 2f);
        }
    }
}
