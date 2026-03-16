using UnityEngine;
using UnityEngine.SceneManagement;
using DeepShift.Core;

namespace DeepShift.Core
{
    /// <summary>
    /// Persistent singleton that drives all scene transitions for the game.
    /// Lives in the Bootstrap scene alongside <see cref="GameManager"/> and
    /// <see cref="DeepShift.Economy.EconomyManager"/>. Survives all scene loads
    /// via <c>DontDestroyOnLoad</c>.
    ///
    /// <para>Scene flow:</para>
    /// <list type="bullet">
    ///   <item>Bootstrap Start → SurfaceCamp</item>
    ///   <item><c>ShiftStarted</c> → Mine</item>
    ///   <item><c>ShiftComplete</c> → SurfaceCamp</item>
    /// </list>
    ///
    /// <para>
    /// Build Settings must include (in order): Bootstrap, SurfaceCamp, Mine.
    /// Scene name strings are configurable in the Inspector.
    /// </para>
    /// </summary>
    public class SceneController : MonoBehaviour, IGameEventListener
    {
        public static SceneController Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string _surfaceCampScene = "SurfaceCamp";
        [SerializeField] private string _mineScene        = "Mine";

        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO _onShiftStarted;
        [SerializeField] private GameEventSO _onShiftEnded;

        // ── Nested listener (avoids implementing IGameEventListener twice) ──────

        private ShiftCompleteListener _shiftCompleteListener;

        private sealed class ShiftCompleteListener : IGameEventListener
        {
            private readonly SceneController _sc;
            public ShiftCompleteListener(SceneController sc) => _sc = sc;
            public void OnEventRaised() => _sc.EnterSurfaceCamp();
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _shiftCompleteListener = new ShiftCompleteListener(this);
        }

        private void Start()
        {
            // All persistent managers are now initialised — load the first playable scene.
            EnterSurfaceCamp();
        }

        private void OnEnable()
        {
            _onShiftStarted?.RegisterListener(this);
            _onShiftEnded?.RegisterListener(_shiftCompleteListener);
        }

        private void OnDisable()
        {
            _onShiftStarted?.UnregisterListener(this);
            _onShiftEnded?.UnregisterListener(_shiftCompleteListener);
        }

        // ── IGameEventListener — ShiftStarted ─────────────────────────────────

        /// <summary>Loads the Mine scene and sets game state to InShift.</summary>
        public void OnEventRaised()
        {
            GameManager.Instance?.ChangeState(GameState.InShift);
            SceneManager.LoadScene(_mineScene);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Loads the SurfaceCamp scene. Called by event (ShiftEnded) and directly by <see cref="DeepShift.UI.HoistChoiceUI"/>.</summary>
        public void EnterSurfaceCamp()
        {
            GameManager.Instance?.ChangeState(GameState.SurfaceCamp);
            SceneManager.LoadScene(_surfaceCampScene);
        }
    }
}
