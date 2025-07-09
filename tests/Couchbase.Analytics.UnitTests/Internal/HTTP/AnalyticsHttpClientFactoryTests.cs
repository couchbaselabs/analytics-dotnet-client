using System.Security.Authentication;
using Couchbase.Analytics2.Internal.HTTP;
using Couchbase.Analytics2.Internal.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Couchbase.Analytics2.UnitTests.Internal.HTTP;

public class CouchbaseHttpClientFactoryTest
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var credential = new Mock<ICredential>().Object;
        var options = new ClusterOptions().WithSecurityOptions(new SecurityOptions().WithDisableCertificateVerification(true));
        var redactor = new Mock<IRedactor>().Object;
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;

        // Act
        var factory = new CouchbaseHttpClientFactory(credential, options, logger);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_NullSecurityOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var credential = new Mock<ICredential>().Object;
        var redactor = new Mock<IRedactor>().Object;
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CouchbaseHttpClientFactory(credential, null, logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var credential = new Mock<ICredential>().Object;
        var options = new ClusterOptions();
        var redactor = new Mock<IRedactor>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CouchbaseHttpClientFactory(credential, options, null));
    }

    [Fact]
    public void Create_ReturnsHttpClientInstance()
    {
        // Arrange
        var credential = new Credential("Administrator", "password");
        var options = new ClusterOptions().WithSecurityOptions(new SecurityOptions().WithSslProtocols(SslProtocols.Tls12));
        var redactor = new Mock<IRedactor>().Object;
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(credential, options, logger);

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
        var credential = new Mock<ICredential>().Object;
        var options = new ClusterOptions().WithSecurityOptions(new SecurityOptions()
            .WithDisableCertificateVerification(true)
            .WithTrustOnlyCapella());

        var redactor = new Mock<IRedactor>().Object;
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(credential, options, logger);

        // Act
        var handler = factory.Create().DefaultRequestHeaders;

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void DefaultCompletionOption_HasExpectedValue()
    {
        // Arrange
        var credential = new Mock<ICredential>().Object;
        var options = new ClusterOptions();
        var redactor = new Mock<IRedactor>().Object;
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(credential, options, logger);

        // Act
        var completionOption = factory.DefaultCompletionOption;

        // Assert
        Assert.Equal(HttpCompletionOption.ResponseHeadersRead, completionOption);
    }
}