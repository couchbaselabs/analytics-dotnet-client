using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Configuration;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures;

/// <summary>
/// Fixture that connects to the analytics cluster using JWT (Bearer) authentication.
/// Requires <c>JwtToken</c> to be set in <c>settings.json</c>.
/// </summary>
public class JwtFixture : IDisposable
{
    public FixtureSettings FixtureSettings { get; }
    public ClusterOptions ClusterOptions { get; }
    public Cluster Cluster { get; }
    public JwtCredential JwtCredential { get; }

    public JwtFixture()
    {
        FixtureSettings = GetFixtureSettings();

        if (string.IsNullOrWhiteSpace(FixtureSettings.JwtToken))
        {
            throw new InvalidOperationException(
                "JwtToken is not set in settings.json. " +
                "Generate a token with: cbdinocluster jwt generate <username> --can-read --can-write");
        }

        JwtCredential = JwtCredential.Create(FixtureSettings.JwtToken);
        ClusterOptions = new ClusterOptions();
        Cluster = Cluster.Create(
            FixtureSettings.ConnectionString!,
            JwtCredential,
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
