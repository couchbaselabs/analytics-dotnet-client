using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Couchbase.Analytics2;

public record SecurityOptions
{
    private bool _disableServerCertificateValidation;
    private bool _trustOnlyPemFile;
    private bool _trustOnlyCapella;
    private bool _trustOnlyPemString;
    private bool _trustOnlyCertificates;
    private string _pathToPemFile;
    private string _certificate;
    private X509Certificate2Collection _certificates;
    private bool _ignoreRemoteCertificateMismatch = false;
    private RemoteCertificateValidationCallback _remoteCertificateValidationCallback;
    private ClusterOptions _clusterOptions;

    private SslProtocols _sslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12;

    internal SecurityOptions()
    {
        _clusterOptions = new ClusterOptions();
    }
    
    internal SecurityOptions(ClusterOptions clusterOptions)
    {
        _clusterOptions = clusterOptions;
    }
    internal bool TrustOnlyCapellaValue => _trustOnlyCapella;

    internal bool TrustOnlyPemFileValue => _trustOnlyPemFile;

    internal bool TrustOnlyPemStringValue => _trustOnlyPemString;

    internal bool TrustOnlyCertificatesValue => _trustOnlyCertificates;

    internal string PathToPemFileValue => _pathToPemFile;

    internal string CertificateValue => _certificate;

    internal X509Certificate2Collection CertificatesValue => _certificates;

    internal SslProtocols SslProtocolsValue => _sslProtocols;

    internal bool DisableServerCertificateValidation => _disableServerCertificateValidation;

    internal RemoteCertificateValidationCallback RemoteCertificateValidationCallbackValue => _remoteCertificateValidationCallback;

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK
    /// to trust only the Capella CA certificate(s) bundled with
    /// the SDK.
    /// </summary>
    public ClusterOptions TrustOnlyCapella()
    {
        ClearAll();
        _trustOnlyCapella = true;
        return _clusterOptions;
    }

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK to
    /// trust only the PEM-encoded certificate(s) in the file at
    /// the given FS path.
    /// </summary>
    public ClusterOptions TrustOnlyPemFile(string pathToPemFile)
    {
        ClearAll();
        _trustOnlyPemFile = true;
        _pathToPemFile = pathToPemFile;
        return _clusterOptions;
    }

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK to
    /// trust only the PEM-encoded certificate(s) in the given
    /// string.
    /// </summary>
    public ClusterOptions TrustOnlyPemString(string certificate)
    {
        ClearAll();
        _trustOnlyPemString = true;
        _certificate = certificate;
        return _clusterOptions;
    }

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK to
    /// trust only the specified certificates.
    /// </summary>
    public ClusterOptions TrustOnlyCertificates(X509Certificate2Collection certificates)
    {
        ClearAll();
        _trustOnlyCertificates = true;
        _certificates = certificates;
        return _clusterOptions;
    }

    /// <summary>
    /// If true, the SDK trusts ANY certificate regardless of
    /// validity.
    /// Impl Note: This is a separate property because a user
    /// should be able to set this to false and then back to
    /// true without losing the previous certificate trust
    /// settings.
    /// <remarks>The default is false. If disabled an error will be logged.</remarks>
    /// </summary>
    public ClusterOptions DisableCertificateVerification(bool disable = false)
    {
        _disableServerCertificateValidation = disable;
        return _clusterOptions;
    }

    /// <summary>
    /// Names of TLS cipher suites the SDK is allowed to use
    /// when negotiating TLS settings, or an empty list to
    /// use any cipher suite supported by the runtime
    /// environment.
    /// <remarks>An empty list.</remarks>
    /// </summary>
    public ClusterOptions SslProtocols(SslProtocols sslProtocols)
    {
        _sslProtocols = sslProtocols;
        return _clusterOptions;
    }

    internal ClusterOptions IgnoreRemoteCertificateMismatch(
        bool ignoreRemoteCertificateMismatch)
    {
        _ignoreRemoteCertificateMismatch = ignoreRemoteCertificateMismatch;
        return _clusterOptions;
    }

    internal ClusterOptions RemoteCertificateValidationCallback(RemoteCertificateValidationCallback remoteCertificateValidationCallback)
    {
        _remoteCertificateValidationCallback = remoteCertificateValidationCallback;
        return _clusterOptions;
    }

    /// <summary>
    /// For development only
    /// </summary>
    internal RemoteCertificateValidationCallback DangerousRemoteCertificateValidationCallback { get; } =
        (o, certificate, chain, errors) =>
            SkipServerCertificateValidation(o, certificate, chain, errors);

    private static bool SkipServerCertificateValidation(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        //temporarily skip cert validation
        return true;
    }

    private void ClearAll()
    {
        _trustOnlyCapella = false;
        _trustOnlyPemFile = false;
        _trustOnlyPemString = false;
        _trustOnlyCertificates = false;
        _pathToPemFile = null;
        _certificate = null;
        _certificates = null;
    }
}
