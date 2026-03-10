using System.Collections.Generic;
using UnityEngine;

namespace DeepShift.Core
{
    [CreateAssetMenu(menuName = "DeepShift/Events/Game Event", fileName = "GameEvent")]
    public class GameEventSO : ScriptableObject
    {
        private readonly List<IGameEventListener> _listeners = new();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void RegisterListener(IGameEventListener listener) => _listeners.Add(listener);
        public void UnregisterListener(IGameEventListener listener) => _listeners.Remove(listener);
    }
}
