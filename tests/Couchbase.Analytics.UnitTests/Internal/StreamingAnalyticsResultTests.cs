using System.Text.Json;
using Couchbase.AnalyticsClient.Results;
using Couchbase.Text.Json;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.UnitTests.Internal;

public class StreamingAnalyticsResultTests
{
    private readonly ITestOutputHelper _output;

    public StreamingAnalyticsResultTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task StreamingAnalyticsResult_DeserializesCorrectly()
    {
        var json = File.ReadAllBytes("JsonDocuments/analyticsResponse.json");
        var stream = new MemoryStream(json);

       var analyticsResult = new StreamingAnalyticsResult(stream,new StjJsonDeserializer(), new Mock<IDisposable?>().Object);
       await analyticsResult.InitializeAsync(CancellationToken.None);

       var airlines = await analyticsResult.ToListAsync();
       Assert.NotNull(airlines);
       Assert.NotEmpty(airlines);
    }

    public class Root
    {
        public Airline airline { get; set; }
    }

    public class Airline
    {
        public int id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string iata { get; set; }
        public string icao { get; set; }
        public string callsign { get; set; }
        public string country { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange
        var mockStream = new MemoryStream();
        var mockDisposable = new Mock<IDeserializer>();

        // Act
        var result = new StreamingAnalyticsResult(mockStream, mockDisposable.Object);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InitializeAsync_CompletesSuccessfully()
    {
        // Arrange
        var mockStream = new MemoryStream();
        var mockSerializer = new Mock<IDeserializer>();
        mockSerializer.Setup(x=>x.CreateJsonStreamReader(It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(new Mock<IJsonStreamReader>().Object);
        var result = new StreamingAnalyticsResult(mockStream, mockSerializer.Object);

        // Act
        await result.InitializeAsync();

        // Assert
        Assert.True(true); // No exception should be thrown
    }
}