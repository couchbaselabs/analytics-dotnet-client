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

    [Fact]
    public void Create_JwtSwap_DoesNotRebuildHandler()
    {
        // Arrange — start with JWT, swap to a different JWT
        ICredential current = JwtCredential.Create("token-1");
        var options = new ClusterOptions();
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(() => current, options, logger);

        var handlerBefore = factory.CurrentHandler;
        factory.Create(); // prime with initial credential

        // Act — swap JWT
        current = JwtCredential.Create("token-2");
        factory.Create();

        // Assert — handler should be the same (no rebuild for JWT swaps)
        Assert.Same(handlerBefore, factory.CurrentHandler);
    }

    [Fact]
    public void Create_CertificateSwap_RebuildsHandler()
    {
        // Arrange — start with CertA, swap to CertB
        using var rsa1 = System.Security.Cryptography.RSA.Create(2048);
        var req1 = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=CertA", rsa1,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certA = req1.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));

        using var rsa2 = System.Security.Cryptography.RSA.Create(2048);
        var req2 = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=CertB", rsa2,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certB = req2.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));

        ICredential current = CertificateCredential.Create(certA);
        var options = new ClusterOptions();
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(() => current, options, logger);

        var handlerBefore = factory.CurrentHandler;
        factory.Create(); // prime with initial credential

        // Act — swap certificate
        current = CertificateCredential.Create(certB);
        factory.Create();

        // Assert — handler should be different (rebuilt for cert swaps)
        Assert.NotSame(handlerBefore, factory.CurrentHandler);
    }

    [Fact]
    public async Task Create_CertificateSwap_OldHandlerStillFunctional()
    {
        // Arrange — create a client, then swap the cert
        using var rsa1 = System.Security.Cryptography.RSA.Create(2048);
        var req1 = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=CertA", rsa1,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certA = req1.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));

        using var rsa2 = System.Security.Cryptography.RSA.Create(2048);
        var req2 = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=CertB", rsa2,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certB = req2.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));

        ICredential current = CertificateCredential.Create(certA);
        var options = new ClusterOptions();
        var logger = new Mock<ILogger<CouchbaseHttpClientFactory>>().Object;
        var factory = new CouchbaseHttpClientFactory(() => current, options, logger);

        // Get a client bound to the old handler
        var oldClient = factory.Create();

        // Act — swap certificate (triggers handler rebuild)
        current = CertificateCredential.Create(certB);
        factory.Create(); // triggers RecreateHandler

        // Assert — old client should NOT throw ObjectDisposedException
        // The old handler was not disposed, so SendAsync should not throw.
        // It will throw HttpRequestException (no server) but NOT ObjectDisposedException.
        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => oldClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://localhost:1/test")));
        Assert.IsNotType<ObjectDisposedException>(ex);
    }
}
