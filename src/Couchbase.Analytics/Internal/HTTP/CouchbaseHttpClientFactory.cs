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

using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Couchbase.AnalyticsClient.Certificates;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.DnsUtil;
using Couchbase.AnalyticsClient.Internal.DnsUtil.Strategies;
using Couchbase.AnalyticsClient.Internal.Utils;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Logging;

namespace Couchbase.AnalyticsClient.Internal.HTTP;

internal class CouchbaseHttpClientFactory : ICouchbaseHttpClientFactory
{
    /// <summary>
    /// Grace period before disposing a retired handler, allowing in-flight requests to complete.
    /// </summary>
    private static readonly TimeSpan RetiredHandlerDisposeDelay = TimeSpan.FromMinutes(1);

    private readonly Func<ICredential> _credentialProvider;
    private readonly SecurityOptions _securityOptions;
    private readonly TimeoutOptions _timeoutOptions;
    private readonly ILogger<CouchbaseHttpClientFactory> _logger;
    private readonly object _handlerLock = new();
    private volatile AuthenticationHandler _sharedHandler;
    private volatile ICredential _lastKnownCredential;

    /// <summary>
    /// Exposes the current shared handler for testing handler lifecycle behavior.
    /// </summary>
    internal AuthenticationHandler CurrentHandler => _sharedHandler;

    public CouchbaseHttpClientFactory(Func<ICredential> credentialProvider, ClusterOptions options,
        ILogger<CouchbaseHttpClientFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _securityOptions = options.SecurityOptions ?? throw new ArgumentNullException(nameof(options));
        _timeoutOptions = options.TimeoutOptions ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lastKnownCredential = _credentialProvider();
        _sharedHandler = CreateClientHandler();
    }

    /// <summary>
    /// Creates and configures an HTTP handler with bidirectional certificate authentication.
    /// The handler sets up client certificates so the server can authenticate the client,
    /// and configures a RemoteCertificateValidationCallback so the client can authenticate the server.
    /// </summary>
    /// <returns>
    /// An <see cref="AuthenticationHandler"/> configured with:
    /// - Client certificates for mutual TLS authentication (when required by the server)
    /// - Server certificate validation callback based on the configured trust settings
    /// </returns>
    /// <remarks>
    /// When the credential is a <see cref="CertificateCredential"/>, the client certificate is
    /// attached to the handler's SSL options for mutual TLS authentication.
    /// </remarks>
    private AuthenticationHandler CreateClientHandler()
    {
        var handler = new SocketsHttpHandler();

        ConfigureDnsResolverCallback(handler);
        ConfigureClientCertificates(handler);

        handler.SslOptions.RemoteCertificateValidationCallback =
            CertificateValidation.CreateRemoteCertificateValidationCallback(_securityOptions, _logger);
        return new AuthenticationHandler(handler, _credentialProvider);
    }

    private void ConfigureClientCertificates(SocketsHttpHandler handler)
    {
        handler.SslOptions.EnabledSslProtocols = _securityOptions.SslProtocols;

        var certCollection = new X509Certificate2Collection();
        switch (_securityOptions.TrustMode)
        {
            case CertificateTrustMode.CapellaOnly:
                certCollection.Add(CertificateValidation.CapellaCaCert);
                break;
            case CertificateTrustMode.CertificatesOnly:
                certCollection.AddRange(_securityOptions.CertificatesValue!);
                break;
            case CertificateTrustMode.PemFilePath:
                certCollection.Add(X509CertificateLoader.LoadCertificateFromFile(_securityOptions.PathToPemFileValue!));
                break;
            case CertificateTrustMode.PemString:
                certCollection.Add(X509CertificateLoader.LoadCertificate(
                    System.Text.Encoding.ASCII.GetBytes(_securityOptions.CertificateValue!)));
                break;
            case CertificateTrustMode.Default:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_securityOptions.TrustMode));
        }

        // If using mTLS, add the client authentication certificate
        var credential = _credentialProvider();
        if (credential is CertificateCredential certCred)
        {
            certCollection.Add(certCred.Certificate);
        }

        handler.SslOptions.ClientCertificates = certCollection;

        // This emulates the behavior of HttpClientHandler in Manual mode, which selects the first certificate
        // from the list which is eligible for use as a client certificate based on having a private key and
        // the correct key usage flags.
        handler.SslOptions.LocalCertificateSelectionCallback =
            (_, _, _, _, _) => CertificateValidation.GetClientCertificate(certCollection)!;
    }

    /// <summary>
    /// Registers a ConnectCallback to the handler to configure the behaviour of each connection attempt.
    /// The current behaviour is:
    /// - Refresh the DNS record on every request
    /// - Connect to a random endpoint from the resolved DNS record
    /// - Use the <see cref="TimeoutOptions.ConnectTimeout"/> for each endpoint connection attempt.
    /// This means that if ConnectTimeout is set to 10 seconds and there are 3 endpoints, the total time to connect could be up to 30 seconds if all endpoints are slow to respond or unavailable.
    /// </summary>
    /// <param name="handler">The Http Handler</param>
    private void ConfigureDnsResolverCallback(SocketsHttpHandler handler)
    {
        var connector = new DnsEndpointConnector(
            new CountBasedDnsRefreshStrategy(1), // Refresh DNS entries on every request
            _timeoutOptions.ConnectTimeout,
            EndpointSelectionMode.RandomFromUnusedEndpoints);

        handler.ConnectCallback = async (context, cancellation) =>
        {
            var socket = await connector.ConnectAsync(context.DnsEndPoint, cancellation).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        };
    }

    public HttpClient Create()
    {
        var currentCredential = _credentialProvider();

        // Only rebuild the handler when the mTLS certificate changes.
        // JWT and Basic credential swaps don't need a handler rebuild because they work
        // via per-request header injection in AuthenticationHandler.SendAsync.
        // Note: Cluster.UpdateCredential prevents changing the credential type, so checking
        // currentCredential alone is sufficient — if it's a CertificateCredential now, it
        // always was.
        if (!ReferenceEquals(currentCredential, _lastKnownCredential) &&
            currentCredential is CertificateCredential)
        {
            RecreateHandler(currentCredential);
        }

        _lastKnownCredential = currentCredential;
        var httpClient = new HttpClient(_sharedHandler, false);
        ClientIdentifier.SetUserAgent(httpClient.DefaultRequestHeaders);
        return httpClient;
    }

    /// <summary>
    /// Creates a new HTTP handler with the updated credential, replacing the shared handler.
    /// The old handler is disposed after <see cref="RetiredHandlerDisposeDelay"/> to allow
    /// in-flight requests to complete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each <see cref="SocketsHttpHandler"/> maintains its own independent connection pool.
    /// By swapping <c>_sharedHandler</c> to a new instance, all subsequent <see cref="Create"/>
    /// calls return clients bound to a fresh pool with the new certificate. In-flight requests
    /// on the old handler continue to work until its deferred disposal.
    /// </para>
    /// <para>
    /// The old handler is disposed after a grace period via <see cref="DisposeAfterDelayAsync"/>.
    /// This follows the same pattern as <c>IHttpClientFactory</c> in ASP.NET Core, which defers
    /// handler disposal to allow request draining. The grace period is generous (1 minute) to
    /// accommodate the maximum query timeout.
    /// </para>
    /// </remarks>
    /// <param name="newCredential">The new credential that triggered the rebuild.</param>
    private void RecreateHandler(ICredential newCredential)
    {
        lock (_handlerLock)
        {
            // Double-check after acquiring lock — another thread may have already rebuilt
            if (ReferenceEquals(newCredential, _lastKnownCredential))
                return;

            var oldHandler = _sharedHandler;
            _sharedHandler = CreateClientHandler();
            _lastKnownCredential = newCredential;

            // Schedule deferred disposal of the old handler.
            // In-flight requests may still reference it, so we wait before disposing.
            _ = DisposeAfterDelayAsync(oldHandler);
        }
    }

    /// <summary>
    /// Disposes a retired handler after a grace period, allowing in-flight requests to drain.
    /// </summary>
    private static async Task DisposeAfterDelayAsync(AuthenticationHandler handler)
    {
        await Task.Delay(RetiredHandlerDisposeDelay).ConfigureAwait(false);
        handler.Dispose();
    }

    public HttpCompletionOption DefaultCompletionOption { get; set; } = HttpCompletionOption.ResponseHeadersRead;
}
