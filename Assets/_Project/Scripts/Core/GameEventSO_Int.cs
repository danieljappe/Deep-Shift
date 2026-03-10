using UnityEngine;
using UnityEngine.Events;

namespace DeepShift.Core
{
    [CreateAssetMenu(menuName = "DeepShift/Events/Int Event", fileName = "GameEvent_Int")]
    public class GameEventSO_Int : GameEventSOTyped<int> { }

    public class GameEventListener_Int : MonoBehaviour, IGameEventListener<int>
    {
        [SerializeField] private GameEventSO_Int _event;
        [SerializeField] private UnityEvent<int> _response;

        private void OnEnable() => _event.RegisterListener(this);
        private void OnDisable() => _event.UnregisterListener(this);

        public void OnEventRaised(int value) => _response.Invoke(value);
    }
}
