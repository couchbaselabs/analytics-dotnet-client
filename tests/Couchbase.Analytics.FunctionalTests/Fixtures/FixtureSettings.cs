using System.Text.Json.Serialization;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures;

public class FixtureSettings
{
    [JsonPropertyName("ConnectionString")]
    public string? ConnectionString { get; set; } = "http://localhost:8095";

    /// <summary>
    /// Direct-to-node connection string that bypasses the load balancer.
    /// Required for mTLS tests because the nginx passive load balancer
    /// terminates TLS at L7 and does not forward client certificates.
    /// Falls back to <see cref="ConnectionString"/> if not set.
    /// </summary>
    [JsonPropertyName("DirectConnectionString")]
    public string? DirectConnectionString { get; set; }

    [JsonPropertyName("Username")]
    public string Username { get; set; } = "Administrator";

    [JsonPropertyName("Password")]
    public string? Password { get; set; } = "password";

    [JsonPropertyName("JwtToken")]
    public string? JwtToken { get; set; }

    [JsonPropertyName("ClientCertPath")]
    public string? ClientCertPath { get; set; }

    [JsonPropertyName("ClientKeyPath")]
    public string? ClientKeyPath { get; set; }

    [JsonPropertyName("ClientCertPath2")]
    public string? ClientCertPath2 { get; set; }

    [JsonPropertyName("ClientKeyPath2")]
    public string? ClientKeyPath2 { get; set; }
}
