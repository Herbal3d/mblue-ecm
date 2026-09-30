// Copyright 2026 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System;
using System.Collections.Concurrent;

namespace org.herbal3d.mblue.ecm;

/// <summary>
/// Marker interface for all event types in the ECM storage.
/// Uses a struct constraint to ensure that events are value types,
///     which can help with performance and memory usage.
/// Optimized for simple pub/sub but might want to add distance
/// based on event type in the future. For now, all events are broadcast to all subscribers.
/// </summary>
public interface IEvent { }

/// <summary>
/// Marker interface for events tied to a specific entity.
/// </summary>
public interface IEntityEvent : IEvent {
    IEntity? Entity { get; }
    IComponent? Component { get; }
}

public struct SubscriptionHandle {
    public Type EventType { get; }
    public ulong EntityId { get; }
    public Delegate Handler { get; }

    public SubscriptionHandle(Type eventType, ulong entityId, Delegate handler) {
        EventType = eventType;
        EntityId = entityId;
        Handler = handler;
    }
}

public sealed class EventBus {
    private readonly ConcurrentDictionary<Type, HashSet<SubscriptionHandle>> _subscriptions = new();

    #region Global (Unfiltered) Subscriptions

    public SubscriptionHandle Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent {
        var handle = new SubscriptionHandle(typeof(TEvent), 0UL, handler);
        lock (_subscriptions) {
            if (!_subscriptions.ContainsKey(typeof(TEvent))) {
                _subscriptions[typeof(TEvent)] = new HashSet<SubscriptionHandle>();
            }
            _subscriptions[typeof(TEvent)].Add(handle);
        }
        return handle;
    }

    public void Unsubscribe(SubscriptionHandle handle) {
        lock (_subscriptions) {
            if (_subscriptions.ContainsKey(handle.EventType)) {
                _subscriptions[handle.EventType].Remove(handle);
            }
        }
    }

    #endregion

    #region Entity-Specific (Filtered) Subscriptions

    public SubscriptionHandle SubscribeToEntity<TEvent>(IEntity pEntity, Action<TEvent> handler) where TEvent : struct, IEvent {
        var handle = new SubscriptionHandle(typeof(TEvent), pEntity.LGID, handler);
        lock (_subscriptions) {
            if (!_subscriptions.ContainsKey(typeof(TEvent))) {
                _subscriptions[typeof(TEvent)] = new HashSet<SubscriptionHandle>();
            }
            _subscriptions[typeof(TEvent)].Add(handle);
        }
        return handle;
    }

    // Remove all the event subscriptions for a specific entity (used when destroying an entity)
    public void UnsubscribeFromEntityAll(IEntity pEntity) {
        lock (_subscriptions) {
            foreach (var key in _subscriptions.Keys) {
                var keysToRemove = _subscriptions[key].Where(k => k.EntityId == pEntity.LGID).ToList();
                foreach (var sub in keysToRemove) {
                    _subscriptions[key].Remove(sub);
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Publishes an event. If the event carries an Entity ID, it triggers
    /// both the entity-specific subscribers AND global subscribers.
    /// </summary>
    public void Publish<TEvent>(TEvent pEvent) where TEvent : struct, IEvent {
        Type eventType = typeof(TEvent);
        ulong entityId = 0UL;
        if (pEvent is IEntityEvent entityEvent && entityEvent.Entity != null && entityEvent.Entity.LGID != 0UL) {
            entityId = entityEvent.Entity.LGID;
        }

        // 1. Notify global/wildcard subscribers first
        var handles = _subscriptions.ContainsKey(eventType) ? _subscriptions[eventType] : new HashSet<SubscriptionHandle>();
        foreach (var handle in handles) {
            // If the subscription is either global (EntityId == 0) or matches the specific entity ID, invoke the handler.
            if (handle.EntityId == 0UL || handle.EntityId == entityId) {
                var globalAction = handle.Handler as Action<TEvent>;
                globalAction?.Invoke(pEvent);
            }
        }
    }
}