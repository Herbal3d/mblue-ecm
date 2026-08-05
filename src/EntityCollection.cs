// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using org.herbal3d.mblue;
using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.ecm {
    public class EntityCollection {
        protected MBLogger<EntityCollection> m_log;

        public delegate void EntityNewCallback(Entity ent);
        public delegate void EntityUpdateCallback(Entity ent, UpdateInfo what);
        public delegate void EntityRemovedCallback(Entity ent);

        // used in TryGetCreateentity calls to create the entity if needed
        public delegate Entity CreateEntityCallback();

        public event EntityNewCallback? OnEntityNew;
        public event EntityUpdateCallback? OnEntityUpdate;
        public event EntityRemovedCallback? OnEntityRemoved;

        private bool m_shouldQueueEvent = true;

        // As components are added to an entity, they are put in this dictionary for quick access.
        // This is used to do by-component updates and lookups.
        protected DualIndexDictionary<string, ulong, Entity> m_entityDictionary;

        public EntityCollection(MBLogger<EntityCollection> pLog
                                ) {
            m_log = pLog;
            m_entityDictionary = new DualIndexDictionary<string, ulong, Entity>();
        }

        public int Count {
            get { return m_entityDictionary.Count; }
        }

        public void AddEntity(Entity entity) {
            // m_log.Log(MBLogLevel.DWORLDDETAIL, "AddEntity: {0}, n={1}", m_name, entity.Name.Name);
            if (TrackEntity(entity)) {
                // tell the viewer about this prim and let the renderer convert it
                //    into the format needed for display
                if (m_shouldQueueEvent) {
                    // disconnect this work from the caller -- use another thread
                    Task.Run(() => { OnEntityNew?.Invoke(entity); });
                } else {
                    OnEntityNew?.Invoke(entity);
                }
            }
        }

        public void UpdateEntity(Entity entity, UpdateInfo detail) {
            if (OnEntityUpdate is not null) {
                m_log.Log(MBLogLevel.DUPDATEDETAIL, "UpdateEntity: " + entity.Name);
                if (m_shouldQueueEvent) {
                    Task.Run(() => { OnEntityUpdate?.Invoke(entity, detail); });
                } else {
                    OnEntityUpdate?.Invoke(entity, detail);
                }
            }
        }

        public void RemoveEntity(Entity entity) {
            m_log.Log(MBLogLevel.DWORLDDETAIL, "RemoveEntity: " + entity.Name);

            EntityRemovedCallback? erc = OnEntityRemoved;
            erc?.Invoke(entity);

            lock (this) {
                m_entityDictionary.Remove(entity.Name.Name);
            }
        }

        private void SelectEntity(Entity ent) {
        }

        /// <summary>
        /// Add the entity to the collection and track it.
        /// </summary>
        /// <param name="ent"></param>
        /// <returns>'true' if the entity was added, 'false' if it was already in the collection.</returns>
        private bool TrackEntity(Entity ent) {
            try {
                lock (this) {
                    if (m_entityDictionary.ContainsKey(ent.Name.Name)) {
                        m_log.Log(MBLogLevel.DWORLD, "Asked to add same entity again: " + ent.Name);
                    } else {
                        m_entityDictionary.Add(ent.Name.Name, ent.LGID, ent);
                        return true;
                    }
                }
            } catch (Exception e) {
                m_log.Log(MBLogLevel.DWORLD, $"Exception adding entity {ent.Name}: {e}");
            }
            return false;
        }

        private void UnTrackEntity(Entity ent) {
            lock (this) {
                m_entityDictionary.Remove(ent.Name.Name, ent.LGID);
            }
        }

        private void ClearTrackedEntities() {
            lock (this) {
                m_entityDictionary.Clear();
            }
        }
        public bool TryGetEntity(ulong lgid, out Entity ent) {
            return m_entityDictionary.TryGetValue(lgid, out ent);
        }

        public bool TryGetEntity(string entName, out Entity ent) {
            return m_entityDictionary.TryGetValue(entName, out ent);
        }

        public bool TryGetEntity(EntityName entName, out Entity ent) {
            return m_entityDictionary.TryGetValue(entName.Name, out ent);
        }

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
        public bool TryGetCreateEntity(EntityName entName, out Entity? ent, CreateEntityCallback createIt) {
            // m_log.Log(LogLevel.DWORLDDETAIL, "TryGetCreateEntity: n={0}", entName);
            try {
                lock (this) {
                    if (!TryGetEntity(entName, out ent)) {
                        Entity newEntity = createIt();
                        AddEntity(newEntity);
                        ent = newEntity;
                    }
                }
                return true;
            } catch (Exception e) {
                m_log.Log(MBLogLevel.DBADERROR, "TryGetCreateEntityLocalID: Failed to create entity: {0}", e.ToString());
            }
            ent = null;
            return false;
        }

        public Entity? FindEntity(Predicate<Entity> pred) {
            return m_entityDictionary.FindValue(pred);
        }

        // Perform an action on each entity in the collection.
        // The collection is locked for the duration of the action,
        //     so the action should be quick and not call back into the collection.
        public void ForEach(Action<Entity> act) {
            lock (this) {
                m_entityDictionary.ForEach(act);
            }
        }

        public void Dispose() {
            ForEach((Entity ent) => {
                ent.Dispose();
            });
            m_entityDictionary.Clear(); // release any entities we might have

            OnEntityNew = null;
            OnEntityUpdate = null;
            OnEntityRemoved = null;

        }
    }
}
