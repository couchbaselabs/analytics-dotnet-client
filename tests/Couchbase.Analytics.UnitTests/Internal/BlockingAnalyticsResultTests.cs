using Couchbase.Analytics2.Internal;
using Couchbase.Text.Json;
using Xunit;

namespace Couchbase.Analytics2.UnitTests.Internal;

public class BlockingAnalyticsResultTest
{
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
        Assert.Throws<InvalidOperationException>(() => result.GetAsyncEnumerator());
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
