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
    /// Components have some authorization to do stuff.
    /// This class is used to hold that authorization information.
    /// All the auth and compare stuff is in here. The entity and components just use it.
    /// </summary>
    public class AuthInfo : IDumpable, IDisposable {

        // TODO: make the magic happen
        public AuthInfo() {
        }

        public bool CanRead(AuthInfo pOther) {
            // TODO: implement the actual access check
            return true;
        }

        public bool CanChange(AuthInfo pOther) {
            // TODO: implement the actual access check
            return true;
        }

        public override string ToString() {
            return "AuthInfo";
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public JsonNode? GetDump() {
            throw new NotImplementedException();
        }
    }
}

