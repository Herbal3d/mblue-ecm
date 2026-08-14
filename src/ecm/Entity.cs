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

using System.ComponentModel;
using System.Text.Json.Nodes;

using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.ecm
{
    public class Entity : IEntity
    {
        protected MBLogger<Entity> m_log;

        protected ComponentFactory m_ComponentFactory;

        // Every entity has a local, session scoped ID
        protected ulong m_LGID = 0;
        public ulong LGID
        {
            get
            {
                if (m_LGID == 0) m_LGID = NextLGID();
                return m_LGID;
            }
        }
        private static ulong m_LGIDIndex = 0x10000000;
        public static ulong NextLGID() { return m_LGIDIndex++; }

        public virtual EntityName Name { get; set; }

        public virtual Entity? ContainingEntity { get; set; } = null;

        public BHash LastEntityHashCode { get; set; } = new BHashULong(0);

        protected Dictionary<Type, IComponent> m_components = new Dictionary<Type, IComponent>();


        public Entity(MBLogger<Entity> pLog,
                      ComponentFactory pComponentFactory,
                      EntityName pName,
                      Entity? pContainingEntity = null)
        {
            m_log = pLog;
            m_ComponentFactory = pComponentFactory;
            Name = pName;
            ContainingEntity = pContainingEntity;
        }

        #region Component Management
        /// <summary>
        /// Create a component of the given type and add it to the entity.
        /// The component is created using the ComponentFactory.
        /// </summary>
        /// <typeparam name="T">Type of the component to create</typeparam>
        /// <param name="parameters">parameters for the component constructor</param>
        public IEntity CreateAndAddComponent<T>(params object[] parameters) where T : class, IComponent
        {
            var cmpt = m_ComponentFactory.CreateComponent<T>(parameters);
            cmpt.ContainingEntity = this;
            AddComponent<T>(cmpt);
            return this;
        }

        /// <summary>
        /// Register a Component with the entity.
        /// </summary>
        /// <typeparam name="T">Type of the component to add</typeparam>
        /// <param name="pComponent">The component instance to add</param>
        public IEntity AddComponent<T>(T pComponent) where T : class, IComponent
        {
            lock (m_components)
            {
                if (m_components.ContainsKey(typeof(T)))
                {
                    m_log.Log(MBLogLevel.DBADERROR, "Entity.AddComponent: Component of type {0} already exists in entity {1}",
                            typeof(T).ToString(), Name.Name);
                }
                else
                {
                    m_components.Add(typeof(T), pComponent);
                    pComponent.ContainingEntity = this;
                }
            }
            return this;
        }

        /// <summary>
        /// Try to get a component of the given type. This will look for exact
        /// matches first, then will look for derived types.
        /// This allows adding LLCmptLocation and looking it up as ICmptLocation.
        /// </summary>
        /// <param name="pType"></param>
        /// <param name="pComponent"></param>
        /// <returns></returns>
        public bool TryGetComponent<T>(out IComponent pComponent)
        {
            lock (m_components)
            {
                if (m_components.TryGetValue(typeof(T), out IComponent? found))
                {
                    if (found is not null)
                    {
                        pComponent = found;
                        return true;
                    }
                }

                // If we didn't find an exact match, look for derived types
                Type cType = typeof(T);
                foreach (var kvp in m_components)
                {
                    Type componentType = kvp.Value.GetType();
                    if (cType.IsAssignableFrom(componentType))
                    {
                        pComponent = kvp.Value;
                        return true;
                    }
                }
            }

            pComponent = null!;
            return false;
        }

        /// <summary>
        /// Get a component of the given type. This will look for exact
        /// matches first, then will look for derived types.
        /// This allows adding LLCmptLocation and looking it up as ICmptLocation.
        /// If no component of the given type is found, an exception is thrown.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public T Cmpt<T>() where T : class, IComponent
        {
            if (TryGetComponent<T>(out IComponent cmpt))
            {
                return (T)cmpt;
            }
            // Question: Should this throw on error or return null? I think throw is
            // better since it is a programming error to ask for a component that doesn't exist.
            m_log.Log(MBLogLevel.DBADERROR, "EntityBase.Cmpt: No component of type {0}", typeof(T).ToString());
            throw new KeyNotFoundException($@"EntityBase.Cmpt: EntID={m_LGID} No component of type {typeof(T).ToString()}");
        }

        /// <summary>
        ///  Check if the entity has a component of the given type. This will look for exact
        /// matches first, then will look for derived types.
        /// This allows adding LLCmptLocation and looking it up as ICmptLocation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasComponent<T>() where T : class, IComponent
        {
            return TryGetComponent<T>(out _);
        }
        // Test and return component if it exists
        public bool HasComponent<T>(out T? component) where T : class, IComponent
        {
            if (TryGetComponent<T>(out IComponent cmpt))
            {
                component = (T)cmpt;
                return true;
            }
            component = null;
            return false;
        }
        #endregion Component Management

        public virtual void Dispose()
        {
            // tell all the interfaces we're done with them
            foreach (var kvp in m_components)
            {
                try
                {
                    IDisposable idis = kvp.Value as IDisposable;
                    idis?.Dispose();
                    // is this right? How to tell object it's done here but don't need to zap oneself
                }
                catch
                {
                    // if it won't dispose it's not our problem
                }
            }
            m_components.Clear();
        }

        // Tell the entity that something about it changed
        // TODO: CHANGE TO USE EVENTS
        virtual public void Update(UpdateInfo pWhat)
        {
            m_log.Log(MBLogLevel.DUPDATEDETAIL, $"IEntity.Update. what={pWhat.ToString()}");
            // Update all the components. This makes things happen since all logic is hiding in the components.
            /*
            IComponent? cmpt = null;
            try
            {
                foreach (var kvp in m_components)
                {
                    cmpt = kvp.Value;
                    cmpt.Update(pWhat);
                }
            }
            catch (Exception ex)
            {
                m_log.Log(MBLogLevel.DUPDATE, "Error updating component {0} of entity {1}: {2}",
                        cmpt?.GetType().ToString() ?? "--unknown--", Name.ToString() ?? "", ex.ToString());
            }
            */
        }

        // Default implementation of IDumpable.
        public virtual JsonNode GetDump()
        {
            JsonObject ret = new JsonObject();
            ret["Name"] = Name.ToString();
            ret["LGID"] = LGID.ToString();
            ret["ContainingEntity"] = ContainingEntity != null ? ContainingEntity.Name.Name : "--none--";
            JsonArray components = new JsonArray();
            foreach (var kvp in m_components)
            {
                components.Add(kvp.Key.ToString());
            }
            ret["Components"] = components;
            return ret;
        }
    }
}
