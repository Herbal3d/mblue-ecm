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

using MBSD = org.herbal3d.mblue.Common.StructuredData;

namespace org.herbal3d.mblue.ecm {

    public interface ICmptLocation : IComponent {

        MBSD.Quaternion Heading { get; set; }
        MBSD.Vector3 LocalPosition { get; set; }     // position relative to parent (if any)
        MBSD.Vector3 RegionPosition { get; set; }         // position relative to RegionContext
        MBSD.Vector3d GlobalPosition { get; }

    }

    // When a location component is updated, this event is created to capture the new state.
    public struct LocationUpdateEvent : IEntityEvent {
        public LocationUpdateEvent(ICmptLocation pComponent) {
            Entity = pComponent.ContainingEntity;
            Component = pComponent;
            LocalPosition = pComponent.LocalPosition;
            RegionPosition = pComponent.RegionPosition;
            GlobalPosition = pComponent.GlobalPosition;
            Heading = pComponent.Heading;
        }
        public IEntity Entity { get; set; }
        public IComponent? Component { get; set; }
        public MBSD.Vector3 LocalPosition { get; set; }
        public MBSD.Vector3 RegionPosition { get; set; }
        public MBSD.Vector3d GlobalPosition { get; set; }
        public MBSD.Quaternion Heading { get; set; }
    }
}
