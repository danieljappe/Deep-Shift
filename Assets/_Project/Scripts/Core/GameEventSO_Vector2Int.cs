using UnityEngine;
using UnityEngine.Events;

namespace DeepShift.Core
{
    /// <summary>
    /// Typed ScriptableObject event channel that carries a <see cref="Vector2Int"/> payload.
    /// Used to broadcast grid-space positions (e.g. drill impact coordinates).
    /// </summary>
    [CreateAssetMenu(menuName = "DeepShift/Events/Vector2Int Event", fileName = "GameEvent_Vector2Int")]
    public class GameEventSO_Vector2Int : GameEventSOTyped<Vector2Int> { }

    /// <summary>
    /// MonoBehaviour listener for <see cref="GameEventSO_Vector2Int"/> events.
    /// Register this on any GameObject that needs to react to a Vector2Int event via UnityEvents.
    /// </summary>
    public class GameEventListener_Vector2Int : MonoBehaviour, IGameEventListener<Vector2Int>
    {
        [SerializeField] private GameEventSO_Vector2Int _event;
        [SerializeField] private UnityEvent<Vector2Int> _response;

        private void OnEnable()  => _event.RegisterListener(this);
        private void OnDisable() => _event.UnregisterListener(this);

        /// <summary>Forwards the raised value to the inspector-wired UnityEvent response.</summary>
        public void OnEventRaised(Vector2Int value) => _response.Invoke(value);
    }
}
