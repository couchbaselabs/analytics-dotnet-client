    using System.Text.Json.Serialization;

    namespace Couchbase.AnalyticsClient.FunctionalTests.Utils;

    public class GreetingResponse
    {
        [JsonPropertyName("greeting")]
        public string? Greeting { get; set; }
    }