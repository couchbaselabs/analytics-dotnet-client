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

using System.Security.Cryptography.X509Certificates;

namespace Couchbase.AnalyticsClient.Certificates;

/// <summary>
/// Loads X509 certificates across target frameworks. On net9.0+ this uses
/// X509CertificateLoader, which replaces the X509Certificate2 constructors
/// obsoleted by SYSLIB0057. On net8.0 it falls back to those constructors.
/// </summary>
internal static class CertificateLoader
{
    public static X509Certificate2 LoadCertificate(byte[] data) =>
#if NET9_0_OR_GREATER
        X509CertificateLoader.LoadCertificate(data);
#else
        new X509Certificate2(data);
#endif

    public static X509Certificate2 LoadCertificateFromFile(string path) =>
#if NET9_0_OR_GREATER
        X509CertificateLoader.LoadCertificateFromFile(path);
#else
        new X509Certificate2(path);
#endif

    public static X509Certificate2 LoadPkcs12FromFile(string path, string? password) =>
#if NET9_0_OR_GREATER
        X509CertificateLoader.LoadPkcs12FromFile(path, password);
#else
        new X509Certificate2(path, password);
#endif
}
