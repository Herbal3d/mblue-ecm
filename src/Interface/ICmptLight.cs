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

using MBSD = org.herbal3d.mblue.StructuredData;

namespace org.herbal3d.mblue.ecm;

/// <summary>
/// Lights that fill the world. Used for sun and moon. Individual object 
/// lighting is done by the entities themselves.
/// </summary>
public interface ICmptLight : IComponent
{
    bool Visible { get; set; }

    MBSD.Color4 Color { get; set; }

    MBSD.Vector3 Position { get; set; }

    MBSD.Vector3 Target { get; set; }
}
