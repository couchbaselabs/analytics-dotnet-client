using System.Text.Json;
using Couchbase.AnalyticsClient.Internal.Results;
using Couchbase.Core.Json;
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

        var analyticsResult = new StreamingAnalyticsResult(stream, new StjJsonDeserializer(), new Mock<IDisposable>().Object);
        await analyticsResult.InitializeAsync(CancellationToken.None);

        var airlines = await analyticsResult.ToListAsync(CancellationToken.None);
        Assert.NotNull(airlines);
        Assert.NotEmpty(airlines);
    }

    public class Root
    {
        public Airline? airline { get; set; }
    }

    public class Airline
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public string? iata { get; set; }
        public string? icao { get; set; }
        public string? callsign { get; set; }
        public string? country { get; set; }

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
        mockSerializer.Setup(x => x.CreateJsonStreamReader(It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Returns(new Mock<IJsonStreamReader>().Object);
        var result = new StreamingAnalyticsResult(mockStream, mockSerializer.Object);

        // Act
        await result.InitializeAsync();

        // Assert
        Assert.True(true); // No exception should be thrown
    }

    [Fact]
    public async Task EnumerateRows_SecondEnumeration_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = File.ReadAllBytes("JsonDocuments/analyticsResponse.json");
        var stream = new MemoryStream(json);
        var result = new StreamingAnalyticsResult(stream, new StjJsonDeserializer());
        await result.InitializeAsync(CancellationToken.None);

        // Act — first enumeration should succeed
        var rows = await result.ToListAsync(CancellationToken.None);
        Assert.NotEmpty(rows);

        // Assert — second enumeration should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in result) { }
        });
    }

    [Fact]
    public async Task DisposeAsync_DisposesOwnedResources()
    {
        // Arrange
        var stream = new MemoryStream();
        var ownedResource = new Mock<IDisposable>();
        var result = new StreamingAnalyticsResult(stream, new StjJsonDeserializer(), ownedResource.Object);

        // Act
        await result.DisposeAsync();

        // Assert
        ownedResource.Verify(r => r.Dispose(), Times.Once);
        Assert.False(stream.CanRead); // Stream should be disposed
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DisposesOnlyOnce()
    {
        // Arrange
        var stream = new MemoryStream();
        var ownedResource = new Mock<IDisposable>();
        var result = new StreamingAnalyticsResult(stream, new StjJsonDeserializer(), ownedResource.Object);

        // Act
        await result.DisposeAsync();
        await result.DisposeAsync();

        // Assert
        ownedResource.Verify(r => r.Dispose(), Times.Once);
    }
}
