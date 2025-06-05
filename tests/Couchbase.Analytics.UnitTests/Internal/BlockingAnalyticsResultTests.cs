// filepath: /Users/jeffry.morris/Documents/source/analytics-dotnet-client/src/Couchbase.Analytics/Internal/BlockingAnalyticsResultTest.cs
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Analytics2.Internal.Serialization;
using Couchbase.Analytics2.UnitTests.POCOs;
using Moq;
using Moq.Protected;
using Xunit;

namespace Couchbase.Analytics2.Internal.Tests;

public class BlockingAnalyticsResultTest
{
    [Fact]
    public async Task BlockingAnalyticsResult_DeserializesCorrectly()
    {
        var json = File.ReadAllBytes("JsonDocuments/analyticsResponse.json");
        var stream = new MemoryStream(json);
        var onj = await JsonSerializer.DeserializeAsync<AnalyticsResultData<Airline>>(stream, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
    [Fact]
    public async Task InitializeAsync_ShouldReadResponseStream()
    {
        // Arrange
        var json = File.ReadAllBytes("JsonDocuments/analyticsResponse.json");
        var stream = new MemoryStream(json);
        var result = new BlockingAnalyticsResult<object>(stream, new DefaultSerializer());

        // Act
        await result.InitializeAsync();

        // Assert
        // No exception should be thrown, and the method should complete successfully.
    }

    [Fact]
    public void GetAsyncEnumerator_ShouldThrowNotImplementedException()
    {
        // Arrange
        var stream = new MemoryStream();
        var result = new BlockingAnalyticsResult<object>(stream, new DefaultSerializer());

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => result.GetAsyncEnumerator());
    }

    [Fact]
    public async Task InitializeAsync_WithEmptyStream_ShouldNotThrow()
    {
        // Arrange
        var stream = new MemoryStream();
        var result = new BlockingAnalyticsResult<object>(stream, new DefaultSerializer());

        // Act
        await result.InitializeAsync();

        // Assert
        // No exception should be thrown for an empty stream.
    }

    [Fact]
    public async Task InitializeAsync_ShouldCallReadBody()
    {
        // Arrange
        var json = "{\"results\": [{\"key\": \"value\"}]}";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var httpClientMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var result = new Mock<BlockingAnalyticsResult<object>>(stream, httpClientMock.Object) { CallBase = true };

        // Act
        await result.Object.InitializeAsync();

        // Assert
        result.Protected().Verify("ReadBody", Times.Once(), ItExpr.IsAny<byte[]>());
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = new BlockingAnalyticsResult<object>(stream, new DefaultSerializer());

        // Assert
        Assert.NotNull(result);
    }
}