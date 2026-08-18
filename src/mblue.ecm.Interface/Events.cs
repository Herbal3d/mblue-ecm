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
    IEntity Entity { get; }
    IComponent? Component { get; }
}

public sealed class EventBus {
    // The key is a tuple: (Type of Event, Entity ID).
    // For global (unfiltered) subscriptions, we use 0 as the wildcard.
    private readonly ConcurrentDictionary<(Type EventType, ulong EntityId), Delegate> _handlers = new();

    #region Global (Unfiltered) Subscriptions

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent {
        var key = (typeof(TEvent), 0UL);
        _handlers.AddOrUpdate(
            key,
            handler,
            (_, existing) => Delegate.Combine(existing, handler)
        );
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IEvent {
        var key = (typeof(TEvent), 0UL);
        _handlers.AddOrUpdate(
            key,
            handler,
            (_, existing) => Delegate.Remove(existing, handler)!
        );
    }

    #endregion

    #region Entity-Specific (Filtered) Subscriptions

    public void SubscribeToEntity<TEvent>(IEntity pEntity, Action<TEvent> handler) where TEvent : struct, IEntityEvent {
        var key = (typeof(TEvent), pEntity.LGID);
        _handlers.AddOrUpdate(
            key,
            handler,
            (_, existing) => Delegate.Combine(existing, handler)
        );
    }

    public void UnsubscribeFromEntity<TEvent>(IEntity pEntity, Action<TEvent> handler) where TEvent : struct, IEntityEvent {
        var key = (typeof(TEvent), pEntity.LGID);
        _handlers.AddOrUpdate(
            key,
            handler,
            (_, existing) => Delegate.Remove(existing, handler)!
        );
    }

    #endregion

    /// <summary>
    /// Publishes an event. If the event carries an Entity ID, it triggers
    /// both the entity-specific subscribers AND global subscribers.
    /// </summary>
    public void Publish<TEvent>(TEvent pEvent) where TEvent : struct, IEvent {
        Type eventType = typeof(TEvent);

        // 1. Notify global/wildcard subscribers first
        var globalKey = (eventType, 0UL);
        if (_handlers.TryGetValue(globalKey, out var globalDel)) {
            var globalAction = (Action<TEvent>)globalDel;
            globalAction(pEvent);
        }

        // 2. If it is an entity-specific event, notify targeted subscribers
        if (pEvent is IEntityEvent entityEvent && entityEvent.Entity.LGID != 0UL) {
            var entityKey = (eventType, entityEvent.Entity.LGID);
            if (_handlers.TryGetValue(entityKey, out var entityDel)) {
                var entityAction = (Action<TEvent>)entityDel;
                entityAction(pEvent);
            }
        }
    }
}