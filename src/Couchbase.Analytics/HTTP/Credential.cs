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

using System.Net.Http.Headers;
using System.Text;

namespace Couchbase.AnalyticsClient.HTTP;

/// <summary>
/// A username and password credential that authenticates using the HTTP Basic scheme.
/// </summary>
/// <param name="Username">The username to authenticate with.</param>
/// <param name="Password">The password to authenticate with.</param>
public record Credential(string Username, string Password) : ICredential
{
    /// <inheritdoc />
    public AuthenticationHeaderValue AuthorizationHeader { get; } =
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}")));

    /// <summary>
    /// Creates a username and password credential.
    /// </summary>
    /// <param name="username">The username to authenticate with.</param>
    /// <param name="password">The password to authenticate with.</param>
    /// <returns>A <see cref="Credential"/> instance.</returns>
    public static Credential Create(string username, string password)
    {
        return new(username, password);
    }

    /// <summary>
    /// Excludes <see cref="AuthorizationHeader"/> from the record's ToString output
    /// to prevent leaking encoded credentials into logs.
    /// </summary>
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Username = {Username}");
        return true;
    }
}
