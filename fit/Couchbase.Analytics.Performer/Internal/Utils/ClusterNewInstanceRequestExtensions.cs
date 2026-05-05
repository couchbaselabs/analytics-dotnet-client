using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class ClusterNewInstanceRequestExtensions
{
    public static ICredential ToSdkCredential(
        this ClusterNewInstanceRequest request)
    {
        return request.Credential.ToSdkCredential();
    }

    public static ICredential ToSdkCredential(
        this ClusterNewInstanceRequest.Types.Credential credential)
    {
        switch (credential.TypeCase)
        {
            case ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.UsernameAndPassword:
                return new Credential(credential.UsernameAndPassword.Username,
                    credential.UsernameAndPassword.Password);
            case ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.JwtAuth:
                return new JwtCredential(credential.JwtAuth.Jwt);
            case ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.CertificateAuth:
                var x509 = X509Certificate2.CreateFromPem(
                    credential.CertificateAuth.Cert,
                    credential.CertificateAuth.Key);
                return new CertificateCredential(x509);
            case ClusterNewInstanceRequest.Types.Credential.TypeOneofCase.None:
            default:
                throw new ArgumentException("No credential type specified");
        }
    }

    public static ClusterOptions ToSdkQueryOptions(this ClusterNewInstanceRequest request)
    {
        var protoOptions = request.Options;
        var clusterOptions = new ClusterOptions
        {
            ConnectionString = request.ConnectionString
        };

        if (protoOptions.Deserializer is not null)
        {
            if (protoOptions.Deserializer.Custom is not null)
            {

            }
            else if (protoOptions.Deserializer.Json is not null)
            {

            }
            else if (protoOptions.Deserializer.Passthrough is not null)
            {
                // Use default serializer
            }
        }

        clusterOptions = clusterOptions.WithSecurityOptions(request.Options.Security.ToCore());
        clusterOptions = clusterOptions.WithTimeoutOptions(request.Options.Timeout.ToCore());

        return clusterOptions;
    }

    private static TimeoutOptions ToCore(this ClusterNewInstanceRequest.Types.Options.Types.TimeoutOptions? protoTimeout)
    {
        var timeoutOptions = new TimeoutOptions();
        if (protoTimeout is null) return timeoutOptions;

        if (protoTimeout.ConnectTimeout is not null)
        {
            timeoutOptions = timeoutOptions.WithConnectTimeout(protoTimeout.ConnectTimeout.ToTimeSpan());
        }
        if (protoTimeout.DispatchTimeout is not null)
        {
            timeoutOptions = timeoutOptions.WithDispatchTimeout(protoTimeout.DispatchTimeout.ToTimeSpan());
        }
        if (protoTimeout.QueryTimeout is not null)
        {
            timeoutOptions = timeoutOptions.WithQueryTimeout(protoTimeout.QueryTimeout.ToTimeSpan());
        }
        // handle_timeout is not supported by the .NET SDK; intentionally ignored.
        return timeoutOptions;
    }

    private static SecurityOptions ToCore(
        this ClusterNewInstanceRequest.Types.Options.Types.SecurityOptions? protoSecurity)
    {
        // FIT clusters (e.g. dinocluster) issue server certs without reachable OCSP/CRL endpoints,
        // so revocation checking is always disabled in the performer.
        var securityOptions = new SecurityOptions().WithEnableCertificateRevocationCheck(false);
        if (protoSecurity is null) return securityOptions;

        if (protoSecurity.TrustOnlyPlatform)
        {
            // what is this?
        }
        else if (protoSecurity.HasTrustOnlyCapella)
        {
            securityOptions = securityOptions.WithTrustOnlyCapella();
        }
        else if (protoSecurity.HasTrustOnlyPemString)
        {
            // The driver may send a bundle of multiple PEM-encoded certificates concatenated
            // together (e.g. server cert + intermediate).
            var collection = new X509Certificate2Collection();
            collection.ImportFromPem(protoSecurity.TrustOnlyPemString);
            securityOptions = collection.Count > 1
                ? securityOptions.WithTrustOnlyCertificates(collection)
                : securityOptions.WithTrustOnlyPemString(protoSecurity.TrustOnlyPemString);
        }
        if (protoSecurity.HasDisableServerCertificateVerification)
        {
            securityOptions = securityOptions.WithDisableCertificateVerification(protoSecurity.DisableServerCertificateVerification);
        }
        if (protoSecurity.CipherSuites is not null)
        {
            foreach (var cipher in protoSecurity.CipherSuites)
            {
                Serilog.Log.Information("Trying to parse cipher {Cipher} as an SslProtocol", cipher);
                if (Enum.TryParse<SslProtocols>(cipher, false, out var sslProtocol))
                {
                    securityOptions = securityOptions.WithSslProtocols(sslProtocol);
                }
                else
                {
                    Serilog.Log.Information("Failed to parse cipher {Cipher} as an SslProtocol. Valid members are: Tls12, Tls13", cipher);
                }
            }
        }
        return securityOptions;
    }


}