using UnityEngine;

namespace DeepShift.Enemies
{
    /// <summary>
    /// World-space health bar that follows the Borer enemy automatically (child of the Borer transform).
    /// Shown briefly after taking damage; stays visible while in AGGRO state.
    /// Built entirely in code in <see cref="Awake"/> — no prefab required.
    /// Attach to the same GameObject as <see cref="BorerController"/>.
    /// </summary>
    public class BorerHealthBar : MonoBehaviour
    {
        // ── Private state ──────────────────────────────────────────────────────

        private GameObject _barRoot;
        private Transform  _fillTransform;
        private float      _hideTimer;
        private float      _showAfterDamageTime = 3f;
        private bool       _alwaysVisible;
        private int        _maxHp;
        private int        _currentHp;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildBar();
            _barRoot.SetActive(false);
        }

        private void Update()
        {
            if (_alwaysVisible) return;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
                _barRoot.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Sets the maximum and initial HP values and refreshes the fill.</summary>
        public void InitialiseBar(int maxHp, int currentHp)
        {
            _maxHp     = maxHp;
            _currentHp = currentHp;
            RefreshFill();
        }

        /// <summary>Updates the HP value, refreshes the fill, and shows the bar for <see cref="_showAfterDamageTime"/> seconds.</summary>
        public void UpdateBar(int currentHp)
        {
            _currentHp = currentHp;
            RefreshFill();
            _hideTimer = _showAfterDamageTime;
            _barRoot.SetActive(true);
        }

        /// <summary>When <c>true</c> the bar stays visible indefinitely; when <c>false</c> it hides after the timer.</summary>
        public void SetAlwaysVisible(bool visible)
        {
            _alwaysVisible = visible;
            if (_barRoot != null)
                _barRoot.SetActive(visible || _hideTimer > 0f);
        }

        // ── Bar construction ──────────────────────────────────────────────────

        private void BuildBar()
        {
            _barRoot = new GameObject("BorerHealthBar");
            _barRoot.transform.SetParent(transform, false);
            _barRoot.transform.localPosition = Vector3.zero;

            // Background quad — dark grey
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(_barRoot.transform, false);
            bg.transform.localScale    = new Vector3(0.9f, 0.10f, 1f);
            bg.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            Destroy(bg.GetComponent<Collider>());

            var bgMat   = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            bgMat.color = new Color(0.12f, 0.12f, 0.12f);
            var bgR     = bg.GetComponent<Renderer>();
            bgR.material         = bgMat;
            bgR.sortingLayerName = "Player";
            bgR.sortingOrder     = 15;

            // Fill quad — red
            var fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(_barRoot.transform, false);
            fill.transform.localScale    = new Vector3(0f, 0.07f, 1f);
            fill.transform.localPosition = new Vector3(-0.45f, 0.62f, -0.01f);
            Destroy(fill.GetComponent<Collider>());

            var fillMat   = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            fillMat.color = new Color(0.8f, 0.13f, 0f);
            var fillR     = fill.GetComponent<Renderer>();
            fillR.material         = fillMat;
            fillR.sortingLayerName = "Player";
            fillR.sortingOrder     = 16;

            _fillTransform = fill.transform;
        }

        private void RefreshFill()
        {
            if (_fillTransform == null) return;

            float ratio = _maxHp > 0 ? Mathf.Clamp01((float)_currentHp / _maxHp) : 0f;
            _fillTransform.localScale    = new Vector3(ratio * 0.9f, 0.07f, 1f);
            _fillTransform.localPosition = new Vector3(-0.45f + ratio * 0.45f, 0.62f, -0.01f);
        }
    }
}
