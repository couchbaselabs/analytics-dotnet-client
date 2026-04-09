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

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Logging;

namespace Couchbase.AnalyticsClient.Certificates;

/// <summary>
/// Provides certificate validation and management functionality for Couchbase Analytics connections.
/// This class centralizes all certificate-related operations including validation, selection, and Capella cloud certificate handling.
/// </summary>
public static partial class CertificateValidation
{
    /// <summary>
    /// Object Identifier (OID) for client authentication in X.509 certificates.
    /// This OID (1.3.6.1.5.5.7.3.2) indicates that a certificate can be used for client authentication purposes.
    /// </summary>
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    /// <summary>
    /// The certificate (in PEM format) to use by default for connecting to *.cloud.couchbase.com.
    /// </summary>
    /// <remarks>
    /// This in-memory certificate does not work on .NET Framework (legacy) clients.
    /// </remarks>
    internal const string CapellaCaCertPem =
        """
        -----BEGIN CERTIFICATE-----
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
        -----END CERTIFICATE-----
        """;

    /// <summary>
    /// The certificate to use by default for connecting to *.cloud.couchbase.com as a X509Certificate2 instance.
    /// </summary>
    /// <remarks>
    /// The certificate is automatically parsed from the PEM format and made available as an X509Certificate2 instance.
    /// </remarks>
    internal static readonly X509Certificate2 CapellaCaCert =
        X509CertificateLoader.LoadCertificate(System.Text.Encoding.ASCII.GetBytes(CapellaCaCertPem));

    /// <summary>
    /// Creates a <see cref="RemoteCertificateValidationCallback"/> that validates server certificates according to the specified security options.
    /// /// </summary>
    /// <param name="securityOptions">The <see cref="SecurityOptions"/> that define the validation scheme.</param>
    /// <param name="logger">The <see cref="ILogger"/> for recording certificate validation events and security warnings.</param>
    /// <returns>
    /// A callback intended to be used with <see cref="HttpMessageHandler"/> for server certificate validation.
    /// </returns>
    /// <remarks>
    /// The supported trust modes are (configured via <see cref="SecurityOptions"/>):
    /// - Default: Trusts platform well-known CAs AND the embedded Capella CA certificate
    /// - TrustOnlyCapella: Validates only against the embedded Capella CA certificate
    /// - TrustOnlyPemFile/TrustOnlyPemString/TrustOnlyCertificates: Validates only against user-specified certificates
    /// - DisableServerCertificateValidation: Disables certificate verification (not recommended for production)
    /// </remarks>
    public static RemoteCertificateValidationCallback CreateRemoteCertificateValidationCallback(
        SecurityOptions securityOptions,
        ILogger logger)
    {
        return (sender, certificate, chain, sslPolicyErrors) =>
        {
            // Return true regardless of certificate validity
            if (securityOptions.DisableServerCertificateValidation)
            {
                LogCertificateValidationDisabled(logger);
                return true;
            }

            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                LogSslPolicyErrors(sslPolicyErrors, logger);
            }

            // Determine validation mode based on security options
            var useCustomTrustOnly = securityOptions.TrustMode != CertificateTrustMode.Default;

            if (!useCustomTrustOnly)
            {
                // Default mode: Trust both platform CAs and Capella CA
                return ValidateWithPlatformAndCapellaTrust(certificate, chain, sslPolicyErrors, logger);
            }
            else
            {
                // Custom trust mode: Only trust specified certificates
                var trustedCertificates = BuildCustomTrustCollection(securityOptions, logger);
                if (trustedCertificates == null)
                {
                    return false; // Error loading custom certificates
                }

                return ValidateAgainstCustomTrust(certificate, chain, trustedCertificates, logger);
            }
        };
    }

    /// <summary>
    /// Logs SSL policy errors with explanations.
    /// </summary>
    /// <param name="sslPolicyErrors">The SSL policy errors to log.</param>
    /// <param name="logger">Logger for recording the errors.</param>
    private static void LogSslPolicyErrors(SslPolicyErrors sslPolicyErrors, ILogger logger)
    {
        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            LogSslPolicyErrorRemoteCertificateNotAvailable(logger);
        }

        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            LogSslPolicyErrorRemoteCertificateNameMismatch(logger);
        }

        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
        {
            LogSslPolicyErrorRemoteCertificateChainErrors(logger);
        }
    }

    /// <summary>
    /// Validates certificates using both platform trust (well-known CAs) and Capella CA trust.
    /// This is the default validation mode.
    /// </summary>
    /// <param name="certificate">The server certificate to validate.</param>
    /// <param name="chain">The certificate chain provided by the server.</param>
    /// <param name="sslPolicyErrors">SSL policy errors from the platform validation.</param>
    /// <param name="logger">Logger for recording validation results.</param>
    /// <returns>True if the certificate is valid according to platform trust OR Capella trust. false otherwise.</returns>
    private static bool ValidateWithPlatformAndCapellaTrust(
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors,
        ILogger logger)
    {
        // First check: If platform validation passes, accept the certificate
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            LogCertificateValidationPassedPlatformCAs(logger);
            return true;
        }

        // Second check: Validate against Capella CA
        var capellaTrust = new X509Certificate2Collection { CapellaCaCert };
        var capellaValid = ValidateAgainstCustomTrust(certificate, chain, capellaTrust, logger);

        if (capellaValid)
        {
            LogCertificateValidationPassedCapellaCA(logger);
            return true;
        }

        LogCertificateValidationFailedBothPlatformAndCapella(logger);
        return false;
    }

    /// <summary>
    /// Builds a collection of custom trusted certificates based on security options.
    /// </summary>
    /// <param name="securityOptions">The security options containing custom certificate configuration.</param>
    /// <param name="logger">Logger for recording errors during certificate loading.</param>
    /// <returns>Collection of trusted certificates, or null if there was an error loading certificates.</returns>
    private static X509Certificate2Collection? BuildCustomTrustCollection(SecurityOptions securityOptions, ILogger logger)
    {
        var trustedCertificates = new X509Certificate2Collection();

        try
        {
            if (securityOptions.TrustMode == CertificateTrustMode.CapellaOnly)
            {
                trustedCertificates.Add(CapellaCaCert);
            }
            else if (securityOptions.TrustMode == CertificateTrustMode.PemFilePath)
            {
                trustedCertificates.Add(X509CertificateLoader.LoadCertificateFromFile(securityOptions.PathToPemFileValue!));
            }
            else if (securityOptions.TrustMode == CertificateTrustMode.PemString)
            {
                var certs = new X509Certificate2Collection();
                certs.ImportFromPem(securityOptions.CertificateValue!);
                trustedCertificates.AddRange(certs);
            }
            else if (securityOptions.TrustMode == CertificateTrustMode.CertificatesOnly)
            {
                trustedCertificates.AddRange(securityOptions.CertificatesValue!);
            }

            LogBuiltCustomTrustCollection(logger, trustedCertificates.Count);
            return trustedCertificates;
        }
        catch (Exception ex)
        {
            LogFailedToBuildCustomTrustCollection(logger, ex);
            return null;
        }
    }

    /// <summary>
    /// Validates a server certificate chain against a collection of trusted certificates using .NET's built-in chain validation.
    /// </summary>
    /// <param name="certificate">The server certificate to validate.</param>
    /// <param name="chain">The certificate chain provided by the server (may be null).</param>
    /// <param name="trustedCertificates">Collection of certificates that are trusted for validation.</param>
    /// <param name="logger">Logger for recording validation results and errors.</param>
    /// <returns>
    /// True if the certificate is valid and trusted; false otherwise.
    /// A certificate is considered valid if it can build a valid chain to one of the trusted certificates.
    /// </returns>
    /// <remarks>
    /// This method builds a custom trust chain and allows unknown CAs.
    /// </remarks>
    private static bool ValidateAgainstCustomTrust(
        X509Certificate? certificate,
        X509Chain? chain,
        X509Certificate2Collection trustedCertificates,
        ILogger logger)
    {
        if (certificate == null)
        {
            LogServerCertificateIsNull(logger);
            return false;
        }

        X509Certificate2 serverCert;
        try
        {
            serverCert = new X509Certificate2(certificate);
        }
        catch (CryptographicException ex)
        {
            LogFailedToParseServerCertificate(logger, ex);
            return false;
        }

        using var validationChain = new X509Chain();

        // Configure chain policy for custom validation
        validationChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        validationChain.ChainPolicy.CustomTrustStore.AddRange(trustedCertificates);

        // Allow unknown certificate authority since we're using a custom trust
        validationChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

        // We don't check online for revocation since we may be validating against private CAs that don't support OCSP/CRL
        validationChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

        // Copy intermediate certificates from the TLS chain into ExtraStore so the
        // custom chain builder can locate them even if they are not in CustomTrustStore.
        if (chain != null)
        {
            foreach (var element in chain.ChainElements)
            {
                validationChain.ChainPolicy.ExtraStore.Add(element.Certificate);
            }
        }

        // Build and validate the certificate chain
        var chainValid = validationChain.Build(serverCert);

        if (!chainValid)
        {
            LogChainValidationErrors(validationChain, serverCert, logger);
            return false;
        }

        LogCertificateChainValidationSuccessful(logger, serverCert.Subject, serverCert.Issuer);
        return true;
    }

    /// <summary>
    /// Selects the most appropriate client certificate from a collection of candidate certificates.
    /// This method identifies certificates that have private keys and are valid for client authentication purposes.
    /// </summary>
    /// <param name="candidateCerts">A collection of X509Certificate2 objects to evaluate for client authentication.</param>
    /// <returns>
    /// The first certificate that meets the criteria for client authentication, or null if no suitable certificate is found.
    /// A suitable certificate must have a private key and pass client certificate validation checks.
    /// </returns>
    /// <remarks>
    /// This method is used during TLS handshake when the server requests client certificate authentication.
    /// It ensures that only certificates with the proper key usage extensions and private keys are selected.
    /// </remarks>
    public static X509Certificate2? GetClientCertificate(X509Certificate2Collection candidateCerts) =>
        candidateCerts.FirstOrDefault(cert => cert.HasPrivateKey && IsValidClientCertificate(cert));

    /// <summary>
    /// Validates whether an X509 certificate is suitable for client authentication.
    /// This method checks the certificate's key usage and enhanced key usage extensions.
    /// </summary>
    /// <param name="cert">The X509Certificate2 to validate for client authentication usage.</param>
    /// <returns>
    /// True if the certificate is valid for client authentication; false otherwise.
    /// A certificate is considered valid if it either has no usage restrictions or has the correct usage flags set.
    /// </returns>
    /// <remarks>
    /// This validation ensures compliance with X.509 certificate standards and prevents misuse of certificates
    /// that are not intended for client authentication (e.g., server certificates, code signing certificates).
    /// </remarks>
    internal static bool IsValidClientCertificate(X509Certificate2 cert) =>
        !cert.Extensions.Any(extension =>
            (extension is X509EnhancedKeyUsageExtension eku && !IsValidForClientAuthentication(eku)) ||
            (extension is X509KeyUsageExtension keyUsageExtension && !IsValidForDigitalSignatureUsage(keyUsageExtension)));

    /// <summary>
    /// Determines if an Enhanced Key Usage extension permits client authentication.
    /// This method checks if the certificate's enhanced key usage extension includes the client authentication OID.
    /// </summary>
    /// <param name="enhancedKeyUsageExtension">The Enhanced Key Usage extension to examine.</param>
    /// <returns>
    /// True if the extension includes the client authentication OID (1.3.6.1.5.5.7.3.2); false otherwise.
    /// If the extension exists but doesn't include client authentication, the certificate should not be used for client auth.
    /// </returns>
    /// <remarks>
    /// The Enhanced Key Usage extension restricts the purposes for which a certificate can be used.
    /// For Couchbase Analytics client connections, certificates must explicitly allow client authentication.
    /// </remarks>
    private static bool IsValidForClientAuthentication(X509EnhancedKeyUsageExtension enhancedKeyUsageExtension) =>
        enhancedKeyUsageExtension.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == ClientAuthenticationOid);

    /// <summary>
    /// Determines if a Key Usage extension permits digital signatures.
    /// This method checks if the certificate's key usage extension includes the digital signature flag.
    /// </summary>
    /// <param name="keyUsageExtension">The Key Usage extension to examine.</param>
    /// <returns>
    /// True if the extension includes the DigitalSignature flag; false otherwise.
    /// Digital signature capability is required for client authentication in TLS connections.
    /// </returns>
    /// <remarks>
    /// The Key Usage extension defines the cryptographic operations for which the certificate's private key can be used.
    /// For client authentication, the certificate must be able to create digital signatures to prove ownership of the private key.
    /// </remarks>
    private static bool IsValidForDigitalSignatureUsage(X509KeyUsageExtension keyUsageExtension) =>
        keyUsageExtension.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature);

    #region Logging

    /// <summary>
    /// Logs info about certificate chain validation errors.
    /// </summary>
    /// <param name="chain">The certificate chain that failed validation.</param>
    /// <param name="serverCert">The server certificate that was being validated.</param>
    /// <param name="logger">Logger for recording the errors.</param>
    private static void LogChainValidationErrors(X509Chain chain, X509Certificate2 serverCert, ILogger logger)
    {
        LogCertificateChainValidationFailed(logger, serverCert.Subject, serverCert.Issuer);

        foreach (var element in chain.ChainElements)
        {
            if (element.ChainElementStatus.Length <= 0) continue;
            foreach (var status in element.ChainElementStatus)
            {
                LogChainElementError(logger, element.Certificate.Subject, status.Status, status.StatusInformation);
            }
        }
    }

    [LoggerMessage(1, LogLevel.Warning, "Server certificate validation is disabled. This is not recommended for production environments")]
    private static partial void LogCertificateValidationDisabled(ILogger logger);

    [LoggerMessage(2, LogLevel.Warning, "SSL Policy Error: Remote certificate is not available - the server did not provide a certificate")]
    private static partial void LogSslPolicyErrorRemoteCertificateNotAvailable(ILogger logger);

    [LoggerMessage(3, LogLevel.Warning, "SSL Policy Error: Remote certificate name mismatch - the certificate subject name does not match the hostname. This may happen if the server is using a self-signed certificate or the SDK is trying to connect to the server IP directly without a given hostname")]
    private static partial void LogSslPolicyErrorRemoteCertificateNameMismatch(ILogger logger);

    [LoggerMessage(4, LogLevel.Warning, "SSL Policy Error: Remote certificate chain errors - there are issues with the certificate chain validation")]
    private static partial void LogSslPolicyErrorRemoteCertificateChainErrors(ILogger logger);

    [LoggerMessage(5, LogLevel.Debug, "Certificate validation passed using platform well-known CAs")]
    private static partial void LogCertificateValidationPassedPlatformCAs(ILogger logger);

    [LoggerMessage(6, LogLevel.Debug, "Certificate validation passed using Capella CA certificate")]
    private static partial void LogCertificateValidationPassedCapellaCA(ILogger logger);

    [LoggerMessage(7, LogLevel.Warning, "Certificate validation failed for both platform CAs and Capella CA")]
    private static partial void LogCertificateValidationFailedBothPlatformAndCapella(ILogger logger);

    [LoggerMessage(8, LogLevel.Debug, "Built custom trust collection with {count} certificates")]
    private static partial void LogBuiltCustomTrustCollection(ILogger logger, int count);

    [LoggerMessage(9, LogLevel.Error, "Failed to build custom trust certificate collection")]
    private static partial void LogFailedToBuildCustomTrustCollection(ILogger logger, Exception ex);

    [LoggerMessage(10, LogLevel.Warning, "Server certificate is null")]
    private static partial void LogServerCertificateIsNull(ILogger logger);

    [LoggerMessage(11, LogLevel.Error, "Failed to parse server certificate")]
    private static partial void LogFailedToParseServerCertificate(ILogger logger, Exception ex);

    [LoggerMessage(12, LogLevel.Debug, "Certificate chain validation successful. Subject: {subject}, Issuer: {issuer}")]
    private static partial void LogCertificateChainValidationSuccessful(ILogger logger, string subject, string issuer);

    [LoggerMessage(13, LogLevel.Warning, "Certificate chain validation failed. Subject: {subject}, Issuer: {issuer}")]
    private static partial void LogCertificateChainValidationFailed(ILogger logger, string subject, string issuer);

    [LoggerMessage(14, LogLevel.Warning, "Chain element error for {subject}: {status} - {statusInformation}")]
    private static partial void LogChainElementError(ILogger logger, string subject, X509ChainStatusFlags status, string statusInformation);

    #endregion
}