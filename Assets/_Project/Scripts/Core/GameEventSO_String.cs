using UnityEngine;
using UnityEngine.Events;

namespace DeepShift.Core
{
    [CreateAssetMenu(menuName = "DeepShift/Events/String Event", fileName = "GameEvent_String")]
    public class GameEventSO_String : GameEventSOTyped<string> { }

    public class GameEventListener_String : MonoBehaviour, IGameEventListener<string>
    {
        [SerializeField] private GameEventSO_String _event;
        [SerializeField] private UnityEvent<string> _response;

        private void OnEnable() => _event.RegisterListener(this);
        private void OnDisable() => _event.UnregisterListener(this);

        public void OnEventRaised(string value) => _response.Invoke(value);
    }
}
