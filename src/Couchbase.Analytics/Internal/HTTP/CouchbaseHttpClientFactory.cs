using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Analytics2.Exceptions;
using Couchbase.Analytics2.Internal.Logging;
using Couchbase.Analytics2.Internal.Utils;
using Couchbase.Analytics2.Internal.Certificates;
using Couchbase.Analytics2.Internal.DnsUtil;
using Couchbase.Analytics2.Internal.DnsUtil.Strategies;
using Microsoft.Extensions.Logging;

namespace Couchbase.Analytics2.Internal.HTTP;

internal class CouchbaseHttpClientFactory : ICouchbaseHttpClientFactory
{
    private readonly ICredential _credential;
    private readonly SecurityOptions _securityOptions;
    private readonly TimeoutOptions _timeoutOptions;
    private readonly IRedactor _redactor;
    private readonly ILogger<CouchbaseHttpClientFactory> _logger;
    private readonly AuthenticationHandler _sharedHandler;

    public CouchbaseHttpClientFactory(ICredential credential, ClusterOptions options, IRedactor  redactor,
        ILogger<CouchbaseHttpClientFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _credential = credential;
        _securityOptions = options.SecurityOptions ?? throw new ArgumentNullException(nameof(options));
        _timeoutOptions = options.TimeoutOptions ?? throw new ArgumentNullException(nameof(options));
        _redactor = redactor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    /// The handler is not configured for Certificate Based Authentication. A username/password credential is still required.
    /// </remarks>
    private AuthenticationHandler CreateClientHandler()
    {
        var handler = new SocketsHttpHandler();

        ConfigureDnsResolverCallback(handler);
        ConfigureClientCertificates(handler);

        handler.SslOptions.RemoteCertificateValidationCallback =
            CertificateValidation.CreateRemoteCertificateValidationCallback(_securityOptions, _logger);
        return new AuthenticationHandler(handler, _credential);
    }

    private void ConfigureClientCertificates(SocketsHttpHandler handler)
    {
        handler.SslOptions.EnabledSslProtocols = _securityOptions.SslProtocols;

        X509Certificate2Collection certCollection = new X509Certificate2Collection();
        switch (_securityOptions.TrustMode)
        {
            case CertificateTrustMode.CapellaOnly:
                certCollection.Add(CertificateValidation.CapellaCaCert);
                break;
            case CertificateTrustMode.CertificatesOnly:
                certCollection.AddRange(_securityOptions.CertificatesValue);
                break;
            case CertificateTrustMode.PemFilePath:
                certCollection.Add(new X509Certificate2(_securityOptions.PathToPemFileValue));
                break;
            case CertificateTrustMode.PemString:
                certCollection.Add(new X509Certificate2(
                    rawData: System.Text.Encoding.ASCII.GetBytes(_securityOptions.CertificateValue)));
                break;
            case CertificateTrustMode.Default:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_securityOptions.TrustMode));
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
            EndpointSelectionMode.RandomFromUnusedEndpoints
        );

        handler.ConnectCallback = async (context, cancellation) =>
        {
            try
            {
                var socket = await connector.ConnectAsync(context.DnsEndPoint, cancellation).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (AggregateException ex)
            {
                throw new AnalyticsException($"Failed to connect to all endpoints for host: {context.DnsEndPoint.Host}:{context.DnsEndPoint.Port} ", ex);
            }
        };
    }

    public HttpClient Create()
    {
        var httpClient = new HttpClient(_sharedHandler, false);
        ClientIdentifier.SetUserAgent(httpClient.DefaultRequestHeaders);
        return httpClient;
    }

    public HttpCompletionOption DefaultCompletionOption { get; set; } = HttpCompletionOption.ResponseHeadersRead;

}