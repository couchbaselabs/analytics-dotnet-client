#region License
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2025 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion

namespace Couchbase.AnalyticsClient.Internal.Utils;

internal static class ConnectionStringParams
{
    public const string ConnectTimeout = "timeout.connect_timeout";
    public const string DispatchTimeout = "timeout.dispatch_timeout";
    public const string QueryTimeout = "timeout.query_timeout";

    public const string TrustOnlyPemFile = "security.trust_only_pem_file";
    public const string DisableServerCertificateVerification = "security.disable_server_certificate_verification";
    public const string CipherSuites = "security.cipher_suites";

    public const string MaxRetries = "max_retries";
}
