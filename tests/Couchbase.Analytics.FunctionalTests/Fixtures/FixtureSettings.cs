using System.Text.Json.Serialization;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures;

public class FixtureSettings
{
    [JsonPropertyName("ConnectionString")]
    public string? ConnectionString { get; set; } = "http://localhost:8095";

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
