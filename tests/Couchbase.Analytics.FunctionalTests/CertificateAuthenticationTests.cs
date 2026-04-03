using Couchbase.AnalyticsClient.Exceptions;
using Couchbase.AnalyticsClient.FunctionalTests.Fixtures;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.FunctionalTests;

[Collection(CertificateCollection.Name)]
public class CertificateAuthenticationTests
{
    private readonly CertificateFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CertificateAuthenticationTests(CertificateFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Test_Query_With_Certificate_Succeeds()
    {
        var result = await _fixture.Cluster.ExecuteQueryAsync(
            "SELECT 1;",
            new QueryOptions { Timeout = TimeSpan.FromSeconds(30) });

        Assert.NotNull(result);
        _output.WriteLine("mTLS certificate-authenticated query succeeded.");
    }

    [Fact]
    public async Task Test_Query_With_Invalid_Certificate_Fails()
    {
        // Create a self-signed certificate NOT signed by the dino CA — server should reject it
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var certReq = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=InvalidClient", rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var selfSignedCert = certReq.CreateSelfSigned(
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));

        var invalidCredential = CertificateCredential.Create(selfSignedCert);
        using var invalidCluster = Cluster.Create(
            _fixture.FixtureSettings.ConnectionString!,
            invalidCredential,
            new ClusterOptions());

        // The server should reject the TLS handshake with an untrusted client cert,
        // which surfaces as an HttpRequestException or AnalyticsException
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await invalidCluster.ExecuteQueryAsync(
                "SELECT 1;",
                new QueryOptions { Timeout = TimeSpan.FromSeconds(10) });
        });

        _output.WriteLine("Invalid (self-signed) certificate was correctly rejected.");
    }

    [Fact]
    public async Task Test_UpdateCredential_With_Fresh_Certificate()
    {
        // The fixture's cert (Administrator) is valid — use it to run a query first
        var result1 = await _fixture.Cluster.ExecuteQueryAsync(
            "SELECT 1;",
            new QueryOptions { Timeout = TimeSpan.FromSeconds(30) });
        Assert.NotNull(result1);

        // Swap to the second user's cert (mtls-swap-user)
        var settings = _fixture.FixtureSettings;
        Assert.False(string.IsNullOrWhiteSpace(settings.ClientCertPath2),
            "ClientCertPath2 must be set in settings.json for the swap test.");
        Assert.False(string.IsNullOrWhiteSpace(settings.ClientKeyPath2),
            "ClientKeyPath2 must be set in settings.json for the swap test.");

        var swapCredential = CertificateCredential.FromPem(
            settings.ClientCertPath2!, settings.ClientKeyPath2!);
        _fixture.Cluster.UpdateCredential(swapCredential);

        // Verify the cluster still works with the new certificate
        var result2 = await _fixture.Cluster.ExecuteQueryAsync(
            "SELECT 2;",
            new QueryOptions { Timeout = TimeSpan.FromSeconds(30) });
        Assert.NotNull(result2);

        // Restore the original credential so other tests aren't affected
        _fixture.Cluster.UpdateCredential(_fixture.CertificateCredential);
        _output.WriteLine("UpdateCredential with fresh certificate succeeded.");
    }
}
