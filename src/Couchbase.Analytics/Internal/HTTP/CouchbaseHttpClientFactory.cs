using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Analytics2.Internal.Logging;
using Couchbase.Analytics2.Internal.Utils;
using Microsoft.Extensions.Logging;

namespace Couchbase.Analytics2.Internal.HTTP;

internal class CouchbaseHttpClientFactory : ICouchbaseHttpClientFactory
{
    private readonly ICredential _credential;
    private readonly SecurityOptions _options;
    private readonly IRedactor _redactor;
    private readonly ILogger<CouchbaseHttpClientFactory> _logger;
    private readonly HttpMessageHandler _sharedHandler;
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    public CouchbaseHttpClientFactory(ICredential credential, SecurityOptions options, IRedactor  redactor,
        ILogger<CouchbaseHttpClientFactory> logger)
    {
        _credential = credential;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _redactor = redactor;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sharedHandler = CreateClientHandler();
    }

    private HttpMessageHandler? CreateClientHandler()
    {
        var handler = new SocketsHttpHandler();
        handler.SslOptions.EnabledSslProtocols = _options.SslProtocolsValue;

        if (_options.DisableServerCertificateValidation)
        {
            //log an error here as this is really dangerous
            handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
            return new AuthenticationHandler(handler, _credential);
        }

        X509Certificate2Collection certCollection = new X509Certificate2Collection();
        if (_options.TrustOnlyCapellaValue)
        {
            //use the capella cert packaged with the SdK
            certCollection.Add(CapellaCaCert);
        }
        else if (_options.TrustOnlyCertificatesValue)
        {
            certCollection.AddRange(_options.CertificatesValue);
        }
        else if (_options.TrustOnlyPemFileValue)
        {
            certCollection.Add(new X509Certificate2(_options.PathToPemFileValue));
        }
        else if (_options.TrustOnlyPemStringValue)
        {
            certCollection.Add(new X509Certificate2(
                rawData: System.Text.Encoding.ASCII.GetBytes(_options.CertificateValue)));
        }
        handler.SslOptions.EnabledSslProtocols = _options.SslProtocolsValue;
        handler.SslOptions.ClientCertificates = certCollection;

        // This emulates the behavior of HttpClientHandler in Manual mode, which selects the first certificate
        // from the list which is eligible for use as a client certificate based on having a private key and
        // the correct key usage flags.
        handler.SslOptions.LocalCertificateSelectionCallback =
            (_, _, _, _, _) => GetClientCertificate(certCollection)!;

        handler.SslOptions.RemoteCertificateValidationCallback = _options.DangerousRemoteCertificateValidationCallback;

        return new AuthenticationHandler(handler, _credential);
    }

    public HttpClient Create()
    {
        var httpClient = new HttpClient(_sharedHandler, false)
        {
            DefaultRequestHeaders =
            {
               // ExpectContinue = _context.ClusterOptions.EnableExpect100Continue
            }
        };

        ClientIdentifier.SetUserAgent(httpClient.DefaultRequestHeaders);

        return httpClient;
    }

    public HttpCompletionOption DefaultCompletionOption { get; set; } =
        HttpCompletionOption.ResponseHeadersRead;


    /// <summary>
    /// The certificate (in PEM format) to use by default for connecting to *.cloud.couchbase.com.
    /// </summary>
    /// <remarks>
    /// This in-memory certificate does not work on .NET Framework (legacy) clients.
    /// </remarks>
    internal const string CapellaCaCertPem =
                @"-----BEGIN CERTIFICATE-----
                MIIDFTCCAf2gAwIBAgIRANLVkgOvtaXiQJi0V6qeNtswDQYJKoZIhvcNAQELBQAw
                JDESMBAGA1UECgwJQ291Y2hiYXNlMQ4wDAYDVQQLDAVDbG91ZDAeFw0xOTEyMDYy
                MjEyNTlaFw0yOTEyMDYyMzEyNTlaMCQxEjAQBgNVBAoMCUNvdWNoYmFzZTEOMAwG
                A1UECwwFQ2xvdWQwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCfvOIi
                enG4Dp+hJu9asdxEMRmH70hDyMXv5ZjBhbo39a42QwR59y/rC/sahLLQuNwqif85
                Fod1DkqgO6Ng3vecSAwyYVkj5NKdycQu5tzsZkghlpSDAyI0xlIPSQjoORA/pCOU
                WOpymA9dOjC1bo6rDyw0yWP2nFAI/KA4Z806XeqLREuB7292UnSsgFs4/5lqeil6
                rL3ooAw/i0uxr/TQSaxi1l8t4iMt4/gU+W52+8Yol0JbXBTFX6itg62ppb/Eugmn
                mQRMgL67ccZs7cJ9/A0wlXencX2ohZQOR3mtknfol3FH4+glQFn27Q4xBCzVkY9j
                KQ20T1LgmGSngBInAgMBAAGjQjBAMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYE
                FJQOBPvrkU2In1Sjoxt97Xy8+cKNMA4GA1UdDwEB/wQEAwIBhjANBgkqhkiG9w0B
                AQsFAAOCAQEARgM6XwcXPLSpFdSf0w8PtpNGehmdWijPM3wHb7WZiS47iNen3oq8
                m2mm6V3Z57wbboPpfI+VEzbhiDcFfVnK1CXMC0tkF3fnOG1BDDvwt4jU95vBiNjY
                xdzlTP/Z+qr0cnVbGBSZ+fbXstSiRaaAVcqQyv3BRvBadKBkCyPwo+7svQnScQ5P
                Js7HEHKVms5tZTgKIw1fbmgR2XHleah1AcANB+MAPBCcTgqurqr5G7W2aPSBLLGA
                fRIiVzm7VFLc7kWbp7ENH39HVG6TZzKnfl9zJYeiklo5vQQhGSMhzBsO70z4RRzi
                DPFAN/4qZAgD5q3AFNIq2WWADFQGSwVJhg==
                -----END CERTIFICATE-----";

    /// <summary>
    /// The certificate to use by default for connecting to *.cloud.couchbase.com.
    /// </summary>
    /// <remarks>
    /// This in-memory certificate does not work on .NET Framework (legacy) clients.
    /// </remarks>

    internal static readonly X509Certificate2 CapellaCaCert = new(
        rawData: System.Text.Encoding.ASCII.GetBytes(CapellaCaCertPem),
        password: (string?)null);

    /// <summary>
    /// Default CA Certificates included with the SDK.
    /// </summary>
    public static readonly IReadOnlyList<X509Certificate2> DefaultCertificates = new List<X509Certificate2>()
    {
        CapellaCaCert,
    };

    private static X509Certificate2? GetClientCertificate(X509Certificate2Collection candidateCerts) =>
        candidateCerts.Cast<X509Certificate2>()
            .FirstOrDefault(cert => cert.HasPrivateKey && IsValidClientCertificate(cert));

    private static bool IsValidClientCertificate(X509Certificate2 cert) =>
        !cert.Extensions.Cast<X509Extension>().Any(extension =>
            (extension is X509EnhancedKeyUsageExtension eku && !IsValidForClientAuthentication(eku)) ||
            (extension is X509KeyUsageExtension keyUsageExtenstion && !IsValidForDigitalSignatureUsage(keyUsageExtenstion)));

    private static bool IsValidForClientAuthentication(X509EnhancedKeyUsageExtension enhancedKeyUsageExtension) =>
        enhancedKeyUsageExtension.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == ClientAuthenticationOid);

    private static bool IsValidForDigitalSignatureUsage(X509KeyUsageExtension keyUsageExtenstion) =>
        keyUsageExtenstion.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature);
}
