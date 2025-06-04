using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Analytics2.Internal.Utils;

namespace Couchbase.Analytics2;

public record SecurityOptions
{
    /// <summary>
    /// The certificate trust mode that determines which certificates to trust.
    /// </summary>
    internal CertificateTrustMode TrustMode { get; init; } = CertificateTrustMode.Default;

    /// <summary>
    /// Path to the PEM file (used when TrustMode is PemFile).
    /// </summary>
    internal string? PemFilePath { get; init; }

    /// <summary>
    /// PEM-encoded certificate string (used when TrustMode is PemString).
    /// </summary>
    internal string? PemString { get; init; }

    /// <summary>
    /// Certificate collection (used when TrustMode is Certificates).
    /// </summary>
    internal X509Certificate2Collection? Certificates { get; init; }

    /// <summary>
    /// If true, the SDK trusts ANY certificate regardless of validity.
    /// </summary>
    /// <remarks>The default is false. Use with caution in production environments.</remarks>
    internal bool DisableServerCertificateValidation { get; init; }

    /// <summary>
    /// SSL protocols the SDK is allowed to use when negotiating TLS settings.
    /// </summary>
    internal SslProtocols SslProtocols { get; init; } = SslProtocols.Tls13 | SslProtocols.Tls12;

    internal string? PathToPemFileValue => PemFilePath;
    internal string? CertificateValue => PemString;
    internal X509Certificate2Collection? CertificatesValue => Certificates;

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK
    /// to trust only the Capella CA certificate(s) bundled with
    /// the SDK.
    /// </summary>
    public SecurityOptions WithTrustOnlyCapella()
    {
        return this with
            {
                TrustMode = CertificateTrustMode.CapellaOnly,
                PemFilePath = null,
                PemString = null,
                Certificates = null
            };
    }

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK to
    /// trust only the PEM-encoded certificate(s) in the file at
    /// the given FS path.
    /// </summary>
    public SecurityOptions WithTrustOnlyPemFile(string pathToPemFile)
    {
        return this with
            {
                TrustMode = CertificateTrustMode.PemFilePath,
                PemFilePath = pathToPemFile,
                PemString = null,
                Certificates = null
            };
    }

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK to
    /// trust only the PEM-encoded certificate(s) in the given
    /// string.
    /// </summary>
    public SecurityOptions WithTrustOnlyPemString(string certificate)
    {
        return this with
        {
            TrustMode = CertificateTrustMode.PemString,
            PemString = certificate,
            PemFilePath = null,
            Certificates = null
        };
    }

    /// <summary>
    /// Clears any existing trust settings, and tells the SDK to
    /// trust only the specified certificates.
    /// </summary>
    public SecurityOptions WithTrustOnlyCertificates(X509Certificate2Collection certificates)
    {
        return this with
        {
            TrustMode = CertificateTrustMode.CertificatesOnly,
            Certificates = certificates,
            PemFilePath = null,
            PemString = null
        };
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
    public SecurityOptions WithDisableCertificateVerification(bool disable = false)
    {
        return this with { DisableServerCertificateValidation = disable };
    }

    /// <summary>
    /// Sets the SSL protocols the SDK is allowed to use when negotiating TLS settings.
    /// </summary>
    public SecurityOptions WithSslProtocols(SslProtocols protocols)
    {
        return this with { SslProtocols = protocols };
    }
}