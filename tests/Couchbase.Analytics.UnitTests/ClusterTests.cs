using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Couchbase.Analytics2.Internal.Utils;
using Xunit;
using Moq;
#nullable enable

namespace Couchbase.Analytics2.UnitTests
{
    public class ClusterTest
    {
        [Fact]
        public void Create_ValidParameters_Lambda_ReturnsClusterInstance()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");

            // Act
            var cluster = Cluster.Create(httpEndpoint, credential, options =>
                options.WithSecurityOptions(options.SecurityOptions.WithSslProtocols(SslProtocols.Tls13)));

            // Assert
            Assert.NotNull(cluster);
        }

        [Fact]
        public void Create_ValidParameters_ReturnsClusterInstance()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var clusterOptions = new Mock<ClusterOptions>().Object;

            // Act
            var cluster = Cluster.Create(httpEndpoint, credential, clusterOptions);

            // Assert
            Assert.NotNull(cluster);
        }

        [Fact]
        public void Create_NullHttpEndpoint_ThrowsArgumentNullException()
        {
            // Arrange
            Credential credential = Credential.Create("Administrator", "password");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Cluster.Create(null, credential));
        }

        [Fact]
        public void Create_NullCredential_ThrowsArgumentNullException()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Cluster.Create(httpEndpoint, null));
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            // Arrange
            var connectionString = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var cluster = Cluster.Create(connectionString, credential);

            // Act
            cluster.Dispose();

            // Assert
            // No exceptions should be thrown, and resources should be released.
        }

        /// <summary>
        /// Calls all methods in the ClusterOptions class to ensure they are all callable and do not throw exceptions.
        /// If the API changes, this test will fail to compile.
        /// This also displays how to use the options which are immutable records, as opposed to classes.
        /// </summary>
        [Fact]
        public void Create_ClusterOptions_With_All_Parameters()
        {
            var securityOptions = new SecurityOptions()
                .WithDisableCertificateVerification(true)
                .WithSslProtocols(SslProtocols.Tls12)
                .WithTrustOnlyCertificates(new X509Certificate2Collection())
                .WithTrustOnlyPemFile("path/to/certificate.pem")
                .WithTrustOnlyCapella()
                .WithTrustOnlyPemString("pem_string");
            var timeoutOptions = new TimeoutOptions()
                .WithDispatchTimeout(TimeSpan.Zero)
                .WithConnectTimeout(TimeSpan.Zero)
                .WithQueryTimeout(TimeSpan.Zero);

            var clusterOptions = new ClusterOptions()
            {
                ConnectionString = "https://unit_test.cloud.couchbase.com:9999"
            };
            clusterOptions.WithSecurityOptions(securityOptions).WithTimeoutOptions(timeoutOptions);

            timeoutOptions = clusterOptions.TimeoutOptions with
            {
                QueryTimeout = TimeSpan.FromSeconds(30),
                ConnectTimeout = TimeSpan.FromSeconds(10),
                DispatchTimeout = TimeSpan.FromSeconds(5)
            };
            clusterOptions.WithTimeoutOptions(timeoutOptions);

            clusterOptions.WithSecurityOptions(clusterOptions.SecurityOptions.WithDisableCertificateVerification(true));

            Assert.NotNull(clusterOptions.SecurityOptions);
            Assert.NotNull(clusterOptions.TimeoutOptions);
            Assert.NotNull(clusterOptions.ConnectionStringValue);
        }
    }
}