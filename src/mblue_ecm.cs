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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace org.herbal3d.mblue.ecm {

    public static class MBlueECMServiceSetup {

        public static IServiceCollection AddServices(this IServiceCollection pServices, IConfiguration pConfig) {
            return pServices
                .Configure<ECMConfig>(pConfig.GetSection(ECMConfig.subSectionName))
                .AddTransient<UpdateInfo>()
                .AddTransient<AuthInfo>()
                .AddTransient<Entity>()
                .AddSingleton<EntityFactory>()
                .AddSingleton<ComponentFactory>()
            ;
        }
    }

}
