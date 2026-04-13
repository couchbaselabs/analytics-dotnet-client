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
/// A JSON Web Token (JWT) credential that authenticates using the HTTP Bearer scheme.
/// </summary>
/// <param name="Token">The JWT token string.</param>
public sealed record JwtCredential(string Token) : ICredential
{
    /// <inheritdoc />
    public AuthenticationHeaderValue AuthorizationHeader { get; } =
        new("Bearer", Token);

    /// <summary>
    /// Creates a JWT credential.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <returns>A <see cref="JwtCredential"/> instance.</returns>
    public static JwtCredential Create(string token)
    {
        return new(token);
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(Token)} = <{Token.Length} chars>");
        return true;
    }
}
