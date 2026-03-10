using UnityEngine;
using UnityEngine.Events;

namespace DeepShift.Core
{
    [CreateAssetMenu(menuName = "DeepShift/Events/Bool Event", fileName = "GameEvent_Bool")]
    public class GameEventSO_Bool : GameEventSOTyped<bool> { }

    public class GameEventListener_Bool : MonoBehaviour, IGameEventListener<bool>
    {
        [SerializeField] private GameEventSO_Bool _event;
        [SerializeField] private UnityEvent<bool> _response;

        private void OnEnable() => _event.RegisterListener(this);
        private void OnDisable() => _event.UnregisterListener(this);

        public void OnEventRaised(bool value) => _response.Invoke(value);
    }
}
