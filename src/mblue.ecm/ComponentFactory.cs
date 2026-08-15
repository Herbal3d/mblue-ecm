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

using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.ecm {

    /// <summary>
    /// The class that manages the creation of components.
    /// This is used to track what types of components are being created
    /// and to allow for future features like component pooling.
    /// </summary>
    public class ComponentFactory : IDumpable, IDisposable {

        protected readonly IMBLogger _log;
        protected readonly IServiceProvider _provider;

        protected Dictionary<Type, List<IComponent>> _componentTypes = new Dictionary<Type, List<IComponent>>();

        public ComponentFactory(IMBLogger pLog,
                                IServiceProvider pProvider) {
            _log = pLog;
            _provider = pProvider;
        }

        public T CreateComponent<T>(params object[] parameters) where T : class, IComponent {
            var cmpt = ActivatorUtilities.CreateInstance<T>(_provider, parameters);

            // Keep track of the types of components being created. This is used for future features like component pooling.
            this.RememberComponent<T>(cmpt);

            return cmpt;
        }

        public void RememberComponent<T>(IComponent cmpt) where T : class, IComponent {
            lock (_componentTypes) {
                Type interfaceType = typeof(T);
                if (!_componentTypes.ContainsKey(interfaceType)) {
                    _componentTypes[interfaceType] = new List<IComponent>();
                }
                _componentTypes[interfaceType].Add(cmpt);
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
            lock (_componentTypes) {
                Type interfaceType = typeof(T);
                if (_componentTypes.ContainsKey(interfaceType)) {
                    if (_componentTypes[interfaceType].Contains(cmpt)) {
                        _componentTypes[interfaceType].Remove(cmpt);
                        found = true;
                    }
                }
                if (!found) {
                    // Not found in the list for the given type, check the derived types to see if it is tracked there.
                    foreach (var kvp in _componentTypes) {
                        if (interfaceType.IsAssignableFrom(kvp.Key)) {
                            if (_componentTypes[interfaceType].Contains(cmpt)) {
                                _componentTypes[interfaceType].Remove(cmpt);
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
            lock (_componentTypes) {
                Type cmptType = typeof(T);
                if (_componentTypes.ContainsKey(cmptType)) {
                    ret.AddRange(_componentTypes[cmptType]);
                }
                foreach (var kvp in _componentTypes) {
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
            lock (_componentTypes) {
                var componentTypeNames = _componentTypes.Keys.Select(k => k.Name).ToList();
                var componentTypesOSD = new JsonArray();
                foreach (var typeName in componentTypeNames) {
                    componentTypesOSD.Add(typeName);
                }
                ret["ComponentTypes"] = componentTypesOSD;

                // Counts of each type of component that has been created
                var componentCountsOSD = new JsonObject();
                foreach (var kvp in _componentTypes) {
                    componentCountsOSD[kvp.Key.Name] = kvp.Value.Count;
                }
                ret["ComponentCounts"] = componentCountsOSD;
            }

            return ret;
        }

        public void Dispose() {
            lock (_componentTypes) {
                _componentTypes.Clear();
            }
        }
    }
}

