using UnityEngine;

namespace DeepShift.UI
{
    /// <summary>
    /// World-space floating text popup. Rises upward and fades out over its lifetime.
    /// Use <see cref="Spawn"/> to create an instance at a given world position.
    /// Requires a <see cref="TextMesh"/> component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private float _riseSpeed = 1.5f;
        [SerializeField] private float _lifetime  = 1.0f;

        private TextMesh _textMesh;
        private Color    _startColor;
        private float    _elapsed;

        private void Awake()
        {
            _textMesh   = GetComponent<TextMesh>();
            _startColor = _textMesh.color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * _riseSpeed * Time.deltaTime;

            float alpha = Mathf.Clamp01(1f - _elapsed / _lifetime);
            _textMesh.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

            if (_elapsed >= _lifetime)
                Destroy(gameObject);
        }

        // ── Static factory ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates a floating text popup at <paramref name="worldPos"/> showing
        /// <paramref name="text"/> in yellow. No prefab required.
        /// </summary>
        public static void Spawn(Vector3 worldPos, string text)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = worldPos + new Vector3(0f, 0.4f, 0f);

            var tm           = go.AddComponent<TextMesh>();
            tm.text          = text;
            tm.fontSize      = 20;
            tm.characterSize = 0.1f;
            tm.color         = new Color(1f, 0.9f, 0.2f); // yellow
            tm.alignment     = TextAlignment.Center;
            tm.anchor        = TextAnchor.MiddleCenter;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sortingLayerName = "Player";
            mr.sortingOrder     = 20;

            go.AddComponent<FloatingText>();
        }
    }
}
