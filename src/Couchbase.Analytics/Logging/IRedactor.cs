#region License
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2026 Couchbase, Inc.
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

using System.Diagnostics.CodeAnalysis;

namespace Couchbase.AnalyticsClient.Logging;

/// <summary>
/// An interface used for redacting specific log information.
/// </summary>
public interface IRedactor
{
    /// <summary>
    /// Redact user data like query statements, document keys, usernames.
    /// </summary>
    [return: NotNullIfNotNull(nameof(message))]
    object? UserData(object? message);

    /// <summary>
    /// Redact metadata like bucket names, dataset names, index names.
    /// </summary>
    [return: NotNullIfNotNull(nameof(message))]
    object? MetaData(object? message);

    /// <summary>
    /// Redact system data like hostnames, endpoints, URIs.
    /// </summary>
    [return: NotNullIfNotNull(nameof(message))]
    object? SystemData(object? message);
}
