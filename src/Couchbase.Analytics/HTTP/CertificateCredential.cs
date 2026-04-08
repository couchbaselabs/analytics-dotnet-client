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
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Couchbase.AnalyticsClient.HTTP;

/// <summary>
/// A client certificate (mTLS) credential that authenticates during the TLS handshake
/// instead of using HTTP headers. The SDK should support reading the certificate and
/// private key from a PKCS#12 file.
/// </summary>
public sealed record CertificateCredential : ICredential
{
    /// <summary>
    /// The X.509 client certificate used for mutual TLS authentication.
    /// Must contain a private key.
    /// </summary>
    public X509Certificate2 Certificate { get; }

    /// <summary>
    /// Returns <c>null</c> because mTLS authentication is handled during the TLS handshake,
    /// not via HTTP headers.
    /// </summary>
    public AuthenticationHeaderValue? AuthorizationHeader => null;

    /// <summary>
    /// Creates a certificate credential from an <see cref="X509Certificate2"/> instance.
    /// </summary>
    /// <param name="certificate">
    /// An X.509 certificate with a private key. The certificate must be suitable for client
    /// authentication (TLS handshake).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="certificate"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the certificate does not have a private key.</exception>
    public CertificateCredential(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (!certificate.HasPrivateKey)
        {
            throw new ArgumentException(
                "Certificate must have a private key for client authentication.", nameof(certificate));
        }

        Certificate = certificate;
    }

    /// <summary>
    /// Creates a certificate credential from an <see cref="X509Certificate2"/> instance.
    /// </summary>
    /// <param name="certificate">An X.509 certificate with a private key.</param>
    /// <returns>A <see cref="CertificateCredential"/> instance.</returns>
    public static CertificateCredential Create(X509Certificate2 certificate) => new(certificate);

    /// <summary>
    /// Creates a certificate credential by loading a PKCS#12 (.p12 / .pfx) file.
    /// </summary>
    /// <param name="path">The filesystem path to the PKCS#12 file.</param>
    /// <param name="password">The password protecting the PKCS#12 file, or <c>null</c> if unprotected.</param>
    /// <returns>A <see cref="CertificateCredential"/> instance.</returns>
    public static CertificateCredential FromPkcs12(string path, string? password = null) =>
        new(X509CertificateLoader.LoadPkcs12FromFile(path, password));

    /// <summary>
    /// Creates a certificate credential by loading PEM-encoded certificate and private key files.
    /// </summary>
    /// <param name="certPath">The filesystem path to the PEM-encoded certificate file.</param>
    /// <param name="keyPath">The filesystem path to the PEM-encoded private key file.</param>
    /// <returns>A <see cref="CertificateCredential"/> instance.</returns>
    public static CertificateCredential FromPem(string certPath, string keyPath) =>
        new(X509Certificate2.CreateFromPemFile(certPath, keyPath));

    /// <summary>
    /// Excludes sensitive certificate details from the record's ToString output.
    /// Only Subject and Thumbprint are included.
    /// </summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(Certificate.Subject)} = {Certificate.Subject}, {nameof(Certificate.Thumbprint)} = {Certificate.Thumbprint}");
        return true;
    }
}
