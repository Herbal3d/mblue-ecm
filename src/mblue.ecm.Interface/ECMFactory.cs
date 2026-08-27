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

using System.Text.Json.Nodes;

using Microsoft.Extensions.DependencyInjection;

using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.ecm {

    /// <summary>
    /// The class that manages the creation of entities and components.
    /// For components, this tracks what types of components are being created
    /// and allows for future features like component pooling.
    /// </summary>
    public class ECMFactory : IDumpable, IDisposable {

        protected readonly MBLogger<ECMFactory> _log;
        protected readonly IServiceProvider _provider;
        protected readonly EventBus _eventBus;

        protected DualIndexDictionary<string, ulong, IEntity> _entities = new DualIndexDictionary<string, ulong, IEntity>();
        protected Dictionary<Type, List<IComponent>> _components = new Dictionary<Type, List<IComponent>>();

        public ECMFactory(MBLogger<ECMFactory> pLog,
                          IServiceProvider pProvider,
                          EventBus pEventBus
                          ) {
            _log = pLog;
            _provider = pProvider;
            _eventBus = pEventBus;
        }

        // ENTITY ========================================================================
        public struct NewEntityEvent : IEntityEvent {
            public IEntity Entity { get; set; }
            public IComponent? Component { get; } = null;

            public NewEntityEvent(IEntity entity) {
                Entity = entity;
            }
        }
        public struct ReleasedEntityEvent : IEntityEvent {
            public IEntity Entity { get; set; }
            public IComponent? Component { get; } = null;

            public ReleasedEntityEvent(IEntity entity) {
                Entity = entity;
            }
        }
        public IEntity CreateEntity(params object[] parameters) {
            var ent = ActivatorUtilities.CreateInstance<IEntity>(_provider, parameters);

            // Keep track of the types of components being created. This is used for future features like component pooling.
            _entities.Add(ent.Name.Name, ent.LGID, ent);

            // this should probably happen after the entity components have been added
            // Actually, code will probably subscribe to ICmptWorld.EntityAddedEvent.
            // _eventBus.Publish(new NewEntityEvent(ent));

            return ent;
        }
        public void ReleaseEntity(IEntity ent) {
            _eventBus.Publish(new ReleasedEntityEvent(ent));
            _entities.Remove(ent.Name.Name, ent.LGID);
            ent.Dispose();
        }

        public bool TryGetEntity(ulong lgid, out IEntity ent) {
            return _entities.TryGetValue(lgid, out ent);
        }

        public bool TryGetEntity(string entName, out IEntity ent) {
            return _entities.TryGetValue(entName, out ent);
        }

        public bool TryGetEntity(EntityName entName, out IEntity ent) {
            return _entities.TryGetValue(entName.Name, out ent);
        }

        /* Not sure if this convoluted logic is the right solution.
        /// <summary>
        /// Try to find an entity with the given name. If it doesn't exist, create it using the
        /// provided callback and add it to the collection.
        /// The callback is only called if we need to create the entity, so it can be expensive to call.
        /// The callback should return a fully formed entity ready to be added to the collection.
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="ent"></param>
        /// <param name="createIt"></param>
        /// <returns>true if we created a new entry</returns>
        public delegate Entity CreateEntityCallback(params object[] parameters);
        public bool TryGetCreateEntity(EntityName entName, out IEntity? ent, params object[] parameters) {
            // m_log.Log(LogLevel.DWORLDDETAIL, "TryGetCreateEntity: n={0}", entName);
            try {
                lock (this) {
                    if (!TryGetEntity(entName, out ent)) {
                        IEntity newEntity = this.CreateEntity(entName, parameters);
                        ent = newEntity;
                    }
                }
                return true;
            } catch (Exception e) {
                _log.Log(MBLogLevel.DBADERROR, "TryGetCreateEntityLocalID: Failed to create entity: {0}", e.ToString());
            }
            ent = null;
            return false;
        }
        */

        public IEntity? FindEntity(Predicate<IEntity> pred) {
            return _entities.FindValue(pred);
        }

        // Perform an action on each entity in the collection.
        // The collection is locked for the duration of the action,
        //     so the action should be quick and not call back into the collection.
        public void ForEach(Action<IEntity> act) {
            lock (this) {
                _entities.ForEach(act);
            }
        }


        /// <summary>
        /// Create a component of the given type and add it to the entity.
        /// The component is created using the ECMFactory.
        /// </summary>
        /// <typeparam name="T">Type of the component to create</typeparam>
        /// <param name="parameters">parameters for the component constructor</param>
        public IEntity CreateAndAddComponent<T>(IEntity pEntity, params object[] parameters) where T : class, IComponent {
            var cmpt = CreateComponent<T>(parameters);
            cmpt.ContainingEntity = pEntity;
            pEntity.AddComponent<T>(cmpt);
            return pEntity;
        }


        // COMPONENTS ========================================================================
        public T CreateComponent<T>(params object[] parameters) where T : class, IComponent {
            var cmpt = ActivatorUtilities.CreateInstance<T>(_provider, parameters);

            // Keep track of the types of components being created. This is used for future features like component pooling.
            this.RememberComponent<T>(cmpt);

            return cmpt;
        }

        public void RememberComponent<T>(IComponent cmpt) where T : class, IComponent {
            lock (_components) {
                Type interfaceType = typeof(T);
                if (!_components.ContainsKey(interfaceType)) {
                    _components[interfaceType] = new List<IComponent>();
                }
                _components[interfaceType].Add(cmpt);
            }
        }

        /// <summary>
        /// REmove a component from the tracking dictionary.
        /// This should be called when a component is disposed to keep the tracking accurate.
        /// Note that is will remove the component from the list of components for the given type
        /// or, if not found there, it will check the derived types to see if the component is tracked there.
        /// This allows for removing an LLCmptLocation compoent by calling RemoveComponent<ILocationComponent>(cmpt)
        /// even though the component is tracked under the LLCmptLocation type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmpt"></param>
        public void ForgetComponent<T>(IComponent cmpt) where T : class, IComponent {
            bool found = false;
            lock (_components) {
                Type interfaceType = typeof(T);
                if (_components.ContainsKey(interfaceType)) {
                    if (_components[interfaceType].Contains(cmpt)) {
                        _components[interfaceType].Remove(cmpt);
                        found = true;
                    }
                }
                if (!found) {
                    // Not found in the list for the given type, check the derived types to see if it is tracked there.
                    foreach (var kvp in _components) {
                        if (interfaceType.IsAssignableFrom(kvp.Key)) {
                            if (_components[interfaceType].Contains(cmpt)) {
                                _components[interfaceType].Remove(cmpt);
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (!found) {
                _log.Log(MBLogLevel.Warning, "Tried to remove component that was not tracked: " + cmpt.GetType().Name);
            }
        }

        /// <summary>
        /// Get the types of components that have been created for a given component interface.
        /// This will return all components that implement the given interface, including
        /// those that implement derived interfaces.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<IComponent> GetComponentTypes<T>() where T : class, IComponent {
            var ret = new List<IComponent>();
            lock (_components) {
                Type cmptType = typeof(T);
                if (_components.ContainsKey(cmptType)) {
                    ret.AddRange(_components[cmptType]);
                }
                foreach (var kvp in _components) {
                    if (cmptType.IsAssignableFrom(kvp.Key)) {
                        ret.AddRange(kvp.Value);
                    }
                }
            }
            return ret;
        }

        // Return a displayable representation of the component factory,
        // including the types of components that have been created and the counts of each type.
        public JsonNode GetDump() {
            var ret = new JsonObject();

            // Get dumps of all entities
            lock (_entities) {
                var entityDumps = new JsonArray();
                foreach (var ent in _entities.Values) {
                    entityDumps.Add(ent.GetDump());
                }
                ret["Entities"] = entityDumps;
            }
            lock (_components) {
                // Get dumps of all components
                var componentDumps = new JsonArray();
                foreach (var kvp in _components) {
                    foreach (var cmpt in kvp.Value) {
                        componentDumps.Add(cmpt.GetDump());
                    }
                }
                ret["Components"] = componentDumps;

                // Get the names of all component types
                var componentTypeNames = _components.Keys.Select(k => k.Name).ToList();
                var componentTypesOSD = new JsonArray();
                foreach (var typeName in componentTypeNames) {
                    componentTypesOSD.Add(typeName);
                }
                ret["ComponentTypes"] = componentTypesOSD;

                // Counts of each type of component that has been created
                var componentCountsOSD = new JsonObject();
                foreach (var kvp in _components) {
                    componentCountsOSD[kvp.Key.Name] = kvp.Value.Count;
                }
                ret["ComponentCounts"] = componentCountsOSD;
            }

            return ret;
        }

        public void Dispose() {
            lock (_components) {
                _entities.Clear();
                _components.Clear();
            }
        }
    }
}