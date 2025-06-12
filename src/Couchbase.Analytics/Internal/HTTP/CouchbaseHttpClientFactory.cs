using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Analytics2.Internal.Logging;
using Couchbase.Analytics2.Internal.Utils;
using Couchbase.Analytics2.Internal.Certificates;
using Microsoft.Extensions.Logging;

namespace Couchbase.Analytics2.Internal.HTTP;

internal class CouchbaseHttpClientFactory : ICouchbaseHttpClientFactory
{
    private readonly ICredential _credential;
    private readonly SecurityOptions _options;
    private readonly IRedactor _redactor;
    private readonly ILogger<CouchbaseHttpClientFactory> _logger;
    private readonly AuthenticationHandler _sharedHandler;

    public CouchbaseHttpClientFactory(ICredential credential, SecurityOptions options, IRedactor  redactor,
        ILogger<CouchbaseHttpClientFactory> logger)
    {
        _credential = credential;
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
        handler.SslOptions.EnabledSslProtocols = _options.SslProtocols;

        X509Certificate2Collection certCollection = new X509Certificate2Collection();
        if (_options.TrustMode == CertificateTrustMode.CapellaOnly)
        {
            certCollection.Add(CertificateValidation.CapellaCaCert);
        }
        else if (_options.TrustMode == CertificateTrustMode.CertificatesOnly)
        {
            certCollection.AddRange(_options.CertificatesValue);
        }
        else if (_options.TrustMode == CertificateTrustMode.PemFilePath)
        {
            certCollection.Add(new X509Certificate2(_options.PathToPemFileValue));
        }
        else if (_options.TrustMode == CertificateTrustMode.PemString)
        {
            certCollection.Add(new X509Certificate2(
                rawData: System.Text.Encoding.ASCII.GetBytes(_options.CertificateValue)));
        }

        handler.SslOptions.ClientCertificates = certCollection;

        // This emulates the behavior of HttpClientHandler in Manual mode, which selects the first certificate
        // from the list which is eligible for use as a client certificate based on having a private key and
        // the correct key usage flags.
        handler.SslOptions.LocalCertificateSelectionCallback =
            (_, _, _, _, _) => CertificateValidation.GetClientCertificate(certCollection)!;

        // Use proper certificate validation instead of dangerous bypass
        handler.SslOptions.RemoteCertificateValidationCallback =
            CertificateValidation.CreateRemoteCertificateValidationCallback(_options, _logger);
        return new AuthenticationHandler(handler, _credential);
    }

    public HttpClient Create()
    {
        var httpClient = new HttpClient(_sharedHandler, false);
        ClientIdentifier.SetUserAgent(httpClient.DefaultRequestHeaders);
        return httpClient;
    }

    public HttpCompletionOption DefaultCompletionOption { get; set; } = HttpCompletionOption.ResponseHeadersRead;

}