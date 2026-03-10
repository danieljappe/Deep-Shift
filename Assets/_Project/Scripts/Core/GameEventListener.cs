using UnityEngine;
using UnityEngine.Events;

namespace DeepShift.Core
{
    public class GameEventListener : MonoBehaviour, IGameEventListener
    {
        [SerializeField] private GameEventSO _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable() => _event.RegisterListener(this);
        private void OnDisable() => _event.UnregisterListener(this);

        public void OnEventRaised() => _response.Invoke();
    }
}
