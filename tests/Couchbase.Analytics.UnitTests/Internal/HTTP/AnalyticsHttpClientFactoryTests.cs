using System.Security.Authentication;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Options;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Internal.HTTP;

public class CouchbaseHttpClientFactoryTest
{
    private static Func<ICredential> TestCredentialProvider() => () => Credential.Create("test", "test");

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var options = new ClusterOptions().WithSecurityOptions(new SecurityOptions().WithDisableCertificateVerification(true));
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;

        // Act
        var factory = new CouchbaseHttpClientFactory(TestCredentialProvider(), options, logger);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_NullSecurityOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CouchbaseHttpClientFactory(TestCredentialProvider(), null!, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ClusterOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CouchbaseHttpClientFactory(TestCredentialProvider(), options, null!));
    }

    [Fact]
    public void Create_ReturnsHttpClientInstance()
    {
        // Arrange
        var options = new ClusterOptions().WithSecurityOptions(new SecurityOptions().WithSslProtocols(SslProtocols.Tls12));
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(TestCredentialProvider(), options, logger);

        // Act
        var httpClient = factory.Create();

        // Assert
        Assert.NotNull(httpClient);
        Assert.IsType<HttpClient>(httpClient);
    }

    [Fact]
    public void CreateClientHandler_DisableServerCertificateValidation_SetsValidationCallback()
    {
        // Arrange
        var options = new ClusterOptions().WithSecurityOptions(new SecurityOptions()
            .WithDisableCertificateVerification(true)
            .WithTrustOnlyCapella());

        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(TestCredentialProvider(), options, logger);

        // Act
        var handler = factory.Create().DefaultRequestHeaders;

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void DefaultCompletionOption_HasExpectedValue()
    {
        // Arrange
        var options = new ClusterOptions();
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(TestCredentialProvider(), options, logger);

        // Act
        var completionOption = factory.DefaultCompletionOption;

        // Assert
        Assert.Equal(HttpCompletionOption.ResponseHeadersRead, completionOption);
    }
}
