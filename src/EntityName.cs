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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using org.herbal3d.mblue;
using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.ecm {
    /// <summary>
    /// EntityName class to hold the name of an entity.
    /// 
    /// The name is to be an URI style name with a host and entity ID.
    /// Names will have components and parts and conversion methods
    /// to convert between forms. This is where they all hide.
    /// The goal is to have this class hold the details of the name
    /// thus allowing recreation of the name.
    /// 
    /// The basic name is available with the toString() method or
    /// the .Name property.
    /// 
    /// At the moment this is a simple wrapper around a string.
    /// More complex parsing and handling will be added as needed.
    /// 
    /// There is a lot in this routine from the old EntityName class that
    /// was built around the LLLP texture naming conventions. This will be
    /// reworked as needed to handle other naming conventions.
    /// THe code here exists mainly to prevent errors in older code
    /// that is in the process of being ported.
    /// 
    /// </summary>
    public class EntityName : IDumpable {

        public string Name { get; set; } = "";

        public EntityName(string pName) {
            Name = pName;
        }

        public JsonNode? GetDump() {
            return new JsonObject() {
                ["Name"] = Name
            };
        }
    }
}