using System.Security.Authentication;
using Couchbase.Analytics2.Internal;
using Couchbase.Analytics2.Internal.Utils;

namespace Couchbase.Analytics2;

public record ClusterOptions
{
    public ClusterOptions()
    {
    }

    public SecurityOptions SecurityOptions { get; set; } = new();

    public TimeoutOptions TimeoutOptions { get; set; } = new();

    internal ConnectionString? ConnectionStringValue { get; set; }

    /// <summary>
    /// The connection string for the cluster.
    /// </summary>
    public string? ConnectionString
    {
        get => ConnectionStringValue?.ToString();
        set
        {
            ConnectionStringValue = value != null ? Analytics2.Internal.ConnectionString.Parse(value) : null;

            if (ConnectionStringValue == null) return;

            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.ConnectTimeout, out TimeSpan connectTimeout))
            {
                TimeoutOptions = TimeoutOptions.WithConnectTimeout(connectTimeout);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.DispatchTimeout, out TimeSpan dispatchTimeout))
            {
                TimeoutOptions = TimeoutOptions.WithDispatchTimeout(dispatchTimeout);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.QueryTimeout, out TimeSpan queryTimeout))
            {
                TimeoutOptions = TimeoutOptions.WithQueryTimeout(queryTimeout);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.TrustOnlyPemFile, out string pathToPemFile))
            {
                SecurityOptions = SecurityOptions.WithTrustOnlyPemFile(pathToPemFile);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.DisableServerCertificateVerification, out bool disableServerCertificateVerification))
            {
                SecurityOptions = SecurityOptions.WithDisableCertificateVerification(disableServerCertificateVerification);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.CipherSuites, out string? cipherSuites))
            {
                var protocolStrings = cipherSuites.Split(',');
                var protocols = SslProtocols.None;

                foreach (var protocolString in protocolStrings)
                {
                    if (Enum.TryParse<SslProtocols>(protocolString.Trim(), ignoreCase: true, out var protocol))
                    {
                        protocols |= protocol;
                    }
                }

                SecurityOptions = SecurityOptions.WithSslProtocols(protocols);
            }
        }
    }

    //internal JsonSerializer Serializer { get; init; } //static
}