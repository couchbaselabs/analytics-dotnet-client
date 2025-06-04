using System.Text.Json.Serialization;

namespace Couchbase.Analytics2.FunctionalTests.Fixtures;

public class FixtureSettings
{
    [JsonPropertyName("ConnectionString")]
    public string? ConnectionString { get; set; } = "http://localhost:8095";

    [JsonPropertyName("Username")]
    public string Username { get; set; } = "Administrator";

    [JsonPropertyName("Password")]
    public string? Password { get; set; } = "password";

}