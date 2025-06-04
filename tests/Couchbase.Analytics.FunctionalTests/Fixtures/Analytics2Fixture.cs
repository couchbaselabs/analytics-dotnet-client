using System.Net;
using Couchbase.Analytics2.Internal;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Couchbase.Analytics2.FunctionalTests.Fixtures;

public class Analytics2Fixture
{
    public FixtureSettings FixtureSettings { get; set; }

    public ClusterOptions ClusterOptions { get; set; }

    public Cluster Cluster { get; set; }

    public Analytics2Fixture()
    {
        FixtureSettings = GetFixtureSettings();
        ClusterOptions = new ClusterOptions()
        {
            ConnectionString = FixtureSettings.ConnectionString,
            SecurityOptions = new SecurityOptions()
        };
    }

    public FixtureSettings GetFixtureSettings()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("settings.json")
            .Build()
            .GetSection("TestSettings")
            .Get<FixtureSettings>()!; // Null-forgiving so it fails here if the deserialization fails
    }
}