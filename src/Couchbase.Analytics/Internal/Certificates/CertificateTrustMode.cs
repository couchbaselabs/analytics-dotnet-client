namespace Couchbase.Analytics2.Internal.Utils;

/// <summary>
/// Represents the different modes for certificate trust validation.
/// These are mutually exclusive - only one mode can be active at a time.
/// </summary>
public enum CertificateTrustMode
{
    /// <summary>
    /// Default trust mode: Trust both platform CAs and Capella CA certificates.
    /// </summary>
    Default,

    /// <summary>
    /// Trust only the Capella CA certificate(s) bundled with the SDK.
    /// </summary>
    CapellaOnly,

    /// <summary>
    /// Trust only the PEM-encoded certificate(s) from the specified file path.
    /// </summary>
    PemFilePath,

    /// <summary>
    /// Trust only the PEM-encoded certificate(s) from the provided string.
    /// </summary>
    PemString,

    /// <summary>
    /// Trust only the explicitly provided certificate collection.
    /// </summary>
    CertificatesOnly
}