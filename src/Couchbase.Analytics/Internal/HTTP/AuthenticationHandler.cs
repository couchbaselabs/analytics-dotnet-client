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

using Couchbase.AnalyticsClient.HTTP;

namespace Couchbase.AnalyticsClient.Internal.HTTP;

/// <summary>
/// A delegating handler that sets the Authorization header on outgoing HTTP requests
/// based on the current <see cref="ICredential"/>.
/// </summary>
/// <remarks>
/// The credential is resolved via a <see cref="Func{ICredential}"/> on every request,
/// enabling credential hot-swap at runtime via <see cref="Cluster.UpdateCredential"/>.
/// </remarks>
internal class AuthenticationHandler : DelegatingHandler
{
    private readonly Func<ICredential> _credentialProvider;

    /// <summary>
    /// Creates an <see cref="AuthenticationHandler"/> that resolves credentials from the given provider.
    /// </summary>
    /// <param name="innerHandler">The inner HTTP handler.</param>
    /// <param name="credentialProvider">A function that returns the current credential.</param>
    public AuthenticationHandler(HttpMessageHandler innerHandler, Func<ICredential> credentialProvider)
        : base(innerHandler)
    {
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credential = _credentialProvider();
        request.Headers.Authorization = credential.AuthorizationHeader;
        return base.SendAsync(request, cancellationToken);
    }
}
