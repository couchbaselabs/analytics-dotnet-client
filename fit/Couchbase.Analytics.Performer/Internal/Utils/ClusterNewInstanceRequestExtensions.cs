using System.Security.Authentication;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Couchbase.Grpc.Protocol.Columnar;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class ClusterNewInstanceRequestExtensions
{
    public static Credential ToSdkCredential(
        this ClusterNewInstanceRequest request)
    {
        return new Credential(request.Credential.UsernameAndPassword.Username,
            request.Credential.UsernameAndPassword.Password);
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

    private static TimeoutOptions ToCore(this Couchbase.Grpc.Protocol.Columnar.ClusterNewInstanceRequest.Types.Options.Types.TimeoutOptions? protoTimeout)
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
        return timeoutOptions;
    }

    private static SecurityOptions ToCore(
        this Couchbase.Grpc.Protocol.Columnar.ClusterNewInstanceRequest.Types.Options.Types.SecurityOptions? protoSecurity)
    {
        var securityOptions = new SecurityOptions();
        if (protoSecurity is null) return securityOptions;

        if (protoSecurity.TrustOnlyPlatform)
        {
            // what is this?
        }
        else if(protoSecurity.HasTrustOnlyCapella)
        {
            securityOptions = securityOptions.WithTrustOnlyCapella();
        }
        else if (protoSecurity.HasTrustOnlyPemString)
        {
            securityOptions = securityOptions.WithTrustOnlyPemString(protoSecurity.TrustOnlyPemString);
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