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

namespace Couchbase.AnalyticsClient.Certificates;

/// <summary>
/// Represents the different modes for certificate trust validation.
/// These are mutually exclusive - only one mode can be active at a time.
/// </summary>
public enum CertificateTrustMode
{
    /// <summary>
    /// Default trust mode: Trust both platform CAs and Capella CA certificates.
    /// </summary>
    Default,

    /// <summary>
    /// Trust only the Capella CA certificate(s) bundled with the SDK.
    /// </summary>
    CapellaOnly,

    /// <summary>
    /// Trust only the PEM-encoded certificate(s) from the specified file path.
    /// </summary>
    PemFilePath,

    /// <summary>
    /// Trust only the PEM-encoded certificate(s) from the provided string.
    /// </summary>
    PemString,

    /// <summary>
    /// Trust only the explicitly provided certificate collection.
    /// </summary>
    CertificatesOnly
}
