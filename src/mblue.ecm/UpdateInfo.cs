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

using System.Text.Json.Nodes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using org.herbal3d.mblue;
using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.ecm {
    /// <summary>
    /// When an entity is updated, this class is used to tell the entity what changed.
    /// This allows the entity to update only the parts that changed.
    /// </summary>
    public class UpdateInfo : IDumpable, IDisposable {

        // TODO: make the magic happen
        public UpdateInfo() {
        }

        public override string ToString() {
            return "UpdateInfo";
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public JsonNode? GetDump() {
            throw new NotImplementedException();
        }
    }
}
