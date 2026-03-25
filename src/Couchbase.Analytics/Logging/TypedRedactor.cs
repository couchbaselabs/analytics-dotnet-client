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

using System.Runtime.CompilerServices;

namespace Couchbase.AnalyticsClient.Logging;

/// <summary>
/// Provides strongly-typed log redaction. Returns <see cref="Redacted{T}"/> structs
/// to avoid boxing when used with <c>[LoggerMessage]</c> source-generated logging.
/// </summary>
/// <remarks>
/// This type doesn't have an interface and is injected by class so that methods may be inlined.
/// For the public API, use <see cref="IRedactor"/>.
/// </remarks>
internal sealed class TypedRedactor
{
    private const string User = "ud";
    private const string Meta = "md";
    private const string System = "sd";

    public TypedRedactor(RedactionLevel redactionLevel)
    {
        RedactionLevel = redactionLevel;
    }

    public RedactionLevel RedactionLevel { get; }

    /// <summary>
    /// Redact user data like query statements, document keys, usernames.
    /// </summary>
    public Redacted<T> UserData<T>(T message) => RedactMessage(message, User);

    /// <summary>
    /// Redact metadata like bucket names, dataset names, index names.
    /// </summary>
    public Redacted<T> MetaData<T>(T message) => RedactMessage(message, Meta);

    /// <summary>
    /// Redact system data like hostnames, endpoints, URIs.
    /// </summary>
    public Redacted<T> SystemData<T>(T message) => RedactMessage(message, System);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Redacted<T> RedactMessage<T>(T message, string redactionType)
    {
        switch (RedactionLevel)
        {
            case RedactionLevel.None:
                return new Redacted<T>(message);

            case RedactionLevel.Full:
                break;

            case RedactionLevel.Partial:
                if (!ReferenceEquals(redactionType, User))
                {
                    return new Redacted<T>(message);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(RedactionLevel),
                    $"Unexpected redaction level: {RedactionLevel}");
        }

        return new Redacted<T>(message, redactionType);
    }
}
