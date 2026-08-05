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
    public class EntityFactory : IDumpable, IDisposable {

        protected readonly IMBLogger _log;
        protected readonly IServiceProvider _provider;
        protected readonly EntityCollection _collection;

        protected Dictionary<Type, List<IComponent>> _componentTypes = new Dictionary<Type, List<IComponent>>();

        public EntityFactory(IMBLogger pLog,
                             IServiceProvider pProvider,
                             EntityCollection pCollection) {
            _log = pLog;
            _provider = pProvider;
            _collection = pCollection;
        }

        public Entity CreateEntity(params object[] parameters) {
            var ent = ActivatorUtilities.CreateInstance<Entity>(_provider, parameters);

            // Keep track of the types of components being created. This is used for future features like component pooling.
            _collection.AddEntity(ent);

            return ent;
        }

        public void ReleaseEntity(Entity ent) {
            _collection.RemoveEntity(ent);
            ent.Dispose();
        }

        public JsonNode GetDump() {
            var ret = new JsonObject();
            return ret;
        }

        public void Dispose() {
            lock (_componentTypes) {
                _componentTypes.Clear();
            }
        }
    }
}

