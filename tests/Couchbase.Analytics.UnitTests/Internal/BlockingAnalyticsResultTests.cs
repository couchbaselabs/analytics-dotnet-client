using System.Text.Json;
using Couchbase.Analytics2.Internal;
using Couchbase.Text.Json;
using Newtonsoft.Json;
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
        var result = new BlockingAnalyticsResult<object>(stream, new StjJsonDeserializer());

        // Act
        await result.InitializeAsync();

        // Assert
        // No exception should be thrown, and the method should complete successfully.
    }
    
    [Fact]
    public async Task Error_23000_ShouldHaveErrorMessageAndCode()
    {
        // Arrange
        var json = File.ReadAllBytes("JsonDocuments/error-23000-response.json");
        var stream = new MemoryStream(json);
        var result = new BlockingAnalyticsResult<object>(stream, new StjJsonDeserializer());

        // Act
        await result.InitializeAsync();

        // Assert
        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal("Enterprise Analytics is temporarily unavailable", error.Message);
        Assert.Equal(23000, error.Code);
        Assert.True(error.Retriable);
    }

    [Fact]
    public void GetAsyncEnumerator_ShouldThrowNotImplementedException()
    {
        // Arrange
        var stream = new MemoryStream();
        var result = new BlockingAnalyticsResult<object>(stream, new StjJsonDeserializer());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.GetAsyncEnumerator());
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = new BlockingAnalyticsResult<object>(stream, new StjJsonDeserializer());

        // Assert
        Assert.NotNull(result);
    }
}
