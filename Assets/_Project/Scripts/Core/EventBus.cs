using System;
using System.Collections.Generic;

namespace PolyFrontlines.Core
{
    /// <summary>
    /// Generic, static, type-safe publish/subscribe event bus.
    /// Gameplay systems communicate through typed event structs instead of
    /// holding direct references to each other. Keeps systems decoupled
    /// and independently testable.
    ///
    /// Usage:
    ///   EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
    ///   EventBus<PlayerDiedEvent>.Publish(new PlayerDiedEvent(victim, killer));
    /// </summary>
    public static class EventBus<T> where T : struct
    {
        private static readonly List<Action<T>> Subscribers = new List<Action<T>>();

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null) return;
            if (!Subscribers.Contains(handler))
                Subscribers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            Subscribers.Remove(handler);
        }

        public static void Publish(T eventData)
        {
            // Iterate a copy in case a handler subscribes/unsubscribes during dispatch.
            var snapshot = Subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Clears all subscribers for this event type.
        /// Call on scene/match teardown to avoid stale references leaking
        /// between matches (e.g. UI elements from a previous round).
        /// </summary>
        public static void Clear()
        {
            Subscribers.Clear();
        }
    }
}