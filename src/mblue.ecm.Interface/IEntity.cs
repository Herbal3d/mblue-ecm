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

using org.herbal3d.mblue.Common;

namespace org.herbal3d.mblue.ecm;

public interface IEntity : IDumpable, IDisposable {
    public EntityName Name { get; }

    // The local, session unique identifier for this entity
    public ulong LGID { get; }

    // Add a component to the entity
    public IEntity AddComponent<T>(T pComponent) where T : class, IComponent;
    public IEntity CreateAndAddComponent<T>(params object[] parameters) where T : class, IComponent;

    // Remove a component from the entity
    // public void RemoveComponent(IComponent component);

    // Get a component of a specific type from the entity
    public bool TryGetComponent<T>(out IComponent pComponent) where T : class, IComponent;
    public T Cmpt<T>() where T : class, IComponent;
    public bool HasComponent<T>() where T : class, IComponent;

}

