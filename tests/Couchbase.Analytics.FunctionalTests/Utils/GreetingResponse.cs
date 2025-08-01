    using System.Text.Json.Serialization;

    public class GreetingResponse
    {
        [JsonPropertyName("greeting")]
        public string Greeting { get; set; }
    }