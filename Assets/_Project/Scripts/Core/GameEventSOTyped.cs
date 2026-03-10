using System.Collections.Generic;
using UnityEngine;

namespace DeepShift.Core
{
    /// <summary>
    /// Generic base for typed ScriptableObject events. Not used directly — create concrete subclasses.
    /// </summary>
    public abstract class GameEventSOTyped<T> : ScriptableObject
    {
        private readonly List<IGameEventListener<T>> _listeners = new();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised(value);
        }

        public void RegisterListener(IGameEventListener<T> listener) => _listeners.Add(listener);
        public void UnregisterListener(IGameEventListener<T> listener) => _listeners.Remove(listener);
    }
}
