using System.Security.Cryptography.X509Certificates;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Utils;

internal static class CertificateUtils
{
    internal static X509Certificate2 CreateCertificateFromStrings(string certPem, string keyPem)
    {
        var certPath = Path.GetTempFileName();
        var keyPath = Path.GetTempFileName();
        var pfxPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(certPath, certPem);
            File.WriteAllText(keyPath, keyPem);
            using var pemCert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            var pfxBytes = pemCert.Export(X509ContentType.Pfx);
            File.WriteAllBytes(pfxPath, pfxBytes);
            return X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password: null);
        }
        finally
        {
            File.Delete(certPath);
            File.Delete(keyPath);
            File.Delete(pfxPath);
        }
    }

    internal static ICredential ToCore(this ClusterNewInstanceRequest.Types.Credential protoCredential)
    {
        return protoCredential.TypeCase switch
        {
            ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.UsernameAndPassword =>
                Credential.Create(
                    protoCredential.UsernameAndPassword.Username,
                    protoCredential.UsernameAndPassword.Password),
            ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.JwtAuth =>
                JwtCredential.Create(protoCredential.JwtAuth.Jwt),
            ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.CertificateAuth =>
                CertificateCredential.Create(
                    CreateCertificateFromStrings(
                        protoCredential.CertificateAuth.Cert,
                        protoCredential.CertificateAuth.Key)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}