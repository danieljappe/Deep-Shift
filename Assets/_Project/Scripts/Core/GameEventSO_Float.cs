using UnityEngine;
using UnityEngine.Events;

namespace DeepShift.Core
{
    [CreateAssetMenu(menuName = "DeepShift/Events/Float Event", fileName = "GameEvent_Float")]
    public class GameEventSO_Float : GameEventSOTyped<float> { }

    public class GameEventListener_Float : MonoBehaviour, IGameEventListener<float>
    {
        [SerializeField] private GameEventSO_Float _event;
        [SerializeField] private UnityEvent<float> _response;

        private void OnEnable() => _event.RegisterListener(this);
        private void OnDisable() => _event.UnregisterListener(this);

        public void OnEventRaised(float value) => _response.Invoke(value);
    }
}
