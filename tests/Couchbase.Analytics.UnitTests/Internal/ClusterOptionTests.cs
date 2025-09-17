using System.Security.Cryptography.X509Certificates;
using Couchbase.AnalyticsClient.Certificates;
using Couchbase.AnalyticsClient.Options;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.UnitTests.Internal;

public class ClusterOptionTests
{
    private readonly ITestOutputHelper _outputHelper;

    public ClusterOptionTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void ClusterOptions_SecurityOptions_Should_Initialize_Correctly_With_Builder()
    {
        // Arrange
        var options = new ClusterOptions
        {
            ConnectionString = "http://localhost"
        };

        options = options.WithSecurityOptions(opt => opt.WithTrustOnlyCapella());

        Assert.Equal("http://localhost", options.ConnectionString);
        Assert.Equal(CertificateTrustMode.CapellaOnly, options.SecurityOptions.TrustMode);

        options = options.WithSecurityOptions(opt => opt.WithTrustOnlyCertificates(new X509Certificate2Collection()));
        Assert.Equal(CertificateTrustMode.CertificatesOnly, options.SecurityOptions.TrustMode);

        options = options.WithSecurityOptions(opt => opt.WithTrustOnlyPemFile("path/to/pemfile.pem"));
        Assert.Equal(CertificateTrustMode.PemFilePath, options.SecurityOptions.TrustMode);

        options = options.WithSecurityOptions(opt => opt.WithTrustOnlyPemString("-----BEGIN CERTIFICATE-----"));
        Assert.Equal(CertificateTrustMode.PemString, options.SecurityOptions.TrustMode);
    }

    [Fact]
    public void ClusterOptions_TimeoutOptions_Should_Initialize_Correctly_With_Builder()
    {
        var options = new ClusterOptions();
        options = options.WithTimeoutOptions(opt => opt
            .WithQueryTimeout(TimeSpan.FromSeconds(999))
            .WithConnectTimeout(TimeSpan.FromSeconds(999))
            .WithDispatchTimeout(TimeSpan.FromSeconds(999)));

        Assert.Equal(TimeSpan.FromSeconds(999), options.TimeoutOptions.QueryTimeout);
        Assert.Equal(TimeSpan.FromSeconds(999), options.TimeoutOptions.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(999), options.TimeoutOptions.DispatchTimeout);
    }

}