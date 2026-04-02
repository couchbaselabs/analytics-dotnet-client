using System.Net;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Internal.HTTP;

public class AuthenticationHandlerTests
{
    /// <summary>
    /// A test handler that captures the request for inspection instead of sending it.
    /// </summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task SendAsync_BasicCredential_SetsBasicAuthHeader()
    {
        // Arrange
        var credential = Credential.Create("admin", "password");
        var inner = new CapturingHandler();
        var handler = new AuthenticationHandler(inner, () => credential);
        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("http://localhost/test");

        // Assert
        Assert.NotNull(inner.CapturedRequest);
        Assert.NotNull(inner.CapturedRequest!.Headers.Authorization);
        Assert.Equal("Basic", inner.CapturedRequest.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task SendAsync_JwtCredential_SetsBearerAuthHeader()
    {
        // Arrange
        var credential = JwtCredential.Create("my.jwt.token");
        var inner = new CapturingHandler();
        var handler = new AuthenticationHandler(inner, () => credential);
        var client = new HttpClient(handler);

        // Act
        await client.GetAsync("http://localhost/test");

        // Assert
        Assert.NotNull(inner.CapturedRequest);
        Assert.NotNull(inner.CapturedRequest!.Headers.Authorization);
        Assert.Equal("Bearer", inner.CapturedRequest.Headers.Authorization!.Scheme);
        Assert.Equal("my.jwt.token", inner.CapturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_CredentialHotSwap_UsesUpdatedCredential()
    {
        // Arrange — start with Basic, swap to JWT mid-test
        ICredential current = Credential.Create("admin", "password");
        var inner = new CapturingHandler();
        var handler = new AuthenticationHandler(inner, () => current);
        var client = new HttpClient(handler);

        // Act — first request uses Basic
        await client.GetAsync("http://localhost/test");
        Assert.Equal("Basic", inner.CapturedRequest!.Headers.Authorization!.Scheme);

        // Swap credential
        current = JwtCredential.Create("swapped.jwt.token");

        // Act — second request uses Bearer
        await client.GetAsync("http://localhost/test");
        Assert.Equal("Bearer", inner.CapturedRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("swapped.jwt.token", inner.CapturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public void Constructor_NullProvider_ThrowsArgumentNullException()
    {
        var inner = new CapturingHandler();
        Assert.Throws<ArgumentNullException>(() => new AuthenticationHandler(inner, null!));
    }
}
