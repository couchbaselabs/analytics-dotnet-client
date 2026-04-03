using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Configuration;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures;

/// <summary>
/// Fixture that connects to the analytics cluster using mTLS (client certificate) authentication.
/// Requires <c>ClientCertPath</c> and <c>ClientKeyPath</c> to be set in <c>settings.json</c>.
/// </summary>
public class CertificateFixture : IDisposable
{
    public FixtureSettings FixtureSettings { get; }
    public ClusterOptions ClusterOptions { get; }
    public Cluster Cluster { get; }
    public CertificateCredential CertificateCredential { get; }

    public CertificateFixture()
    {
        FixtureSettings = GetFixtureSettings();

        if (string.IsNullOrWhiteSpace(FixtureSettings.ClientCertPath))
        {
            throw new InvalidOperationException(
                "ClientCertPath is not set in settings.json. " +
                "Generate a client cert with: cbdinocluster certificates get-client-cert <username>");
        }

        if (string.IsNullOrWhiteSpace(FixtureSettings.ClientKeyPath))
        {
            throw new InvalidOperationException(
                "ClientKeyPath is not set in settings.json. " +
                "Generate a client cert with: cbdinocluster certificates get-client-cert <username>");
        }

        CertificateCredential = CertificateCredential.FromPem(
            FixtureSettings.ClientCertPath,
            FixtureSettings.ClientKeyPath);

        ClusterOptions = new ClusterOptions();
        Cluster = Cluster.Create(
            FixtureSettings.ConnectionString!,
            CertificateCredential,
            ClusterOptions);
    }

    private static FixtureSettings GetFixtureSettings()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("settings.json")
            .Build()
            .GetSection("TestSettings")
            .Get<FixtureSettings>()!;
    }

    public void Dispose()
    {
        Cluster?.Dispose();
    }
}
