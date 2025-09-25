using System.Security.Authentication;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Public.Certificates;
using Couchbase.AnalyticsClient.Public.Options;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Internal
{
    public class ConnectionStringTest
    {
        [Fact]
        public void Parse_ValidHttpConnectionString_ShouldSetSchemeToHttp()
        {
            var input = "http://user@host1,host2:8091?param1=value1&param2=value2";
            var connectionString = ConnectionString.Parse(input);

            Assert.Equal(Scheme.Http, connectionString.Scheme);
            Assert.Equal("user", connectionString.Username);
            Assert.Equal(2, connectionString.Hosts.Count);
            Assert.Equal("host1", connectionString.Hosts[0].Host);
            Assert.Equal("host2", connectionString.Hosts[1].Host);
            Assert.Equal(8091, connectionString.Hosts[1].Port);
            Assert.Equal("value1", connectionString.Parameters["param1"]);
            Assert.Equal("value2", connectionString.Parameters["param2"]);
        }

        [Fact]
        public void Parse_ValidHttpsConnectionString_ShouldSetSchemeToHttps()
        {
            var input = "https://host1";
            var connectionString = ConnectionString.Parse(input);

            Assert.Equal(Scheme.Https, connectionString.Scheme);
            Assert.Single(connectionString.Hosts);
            Assert.Equal("host1", connectionString.Hosts[0].Host);
        }

        [Fact]
        public void Parse_InvalidScheme_ShouldThrowArgumentException()
        {
            var input = "ftp://host1";

            var exception = Assert.Throws<ArgumentException>(() => ConnectionString.Parse(input));
            Assert.Contains("Unknown scheme", exception.Message);
        }

        [Fact]
        public void Parse_NullInput_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ConnectionString.Parse(null!));
        }

        [Fact]
        public void Parse_EmptyHosts_ShouldThrowArgumentException()
        {
            var input = "http://";

            var exception = Assert.Throws<ArgumentException>(() => ConnectionString.Parse(input));
            Assert.Contains("Hosts list is empty", exception.Message);
        }

        [Fact]
        public void ToString_ShouldGenerateCorrectConnectionString()
        {
            var input = "http://user@host1:8091?param1=value1";
            var connectionString = ConnectionString.Parse(input);

            var result = connectionString.ToString();

            Assert.Equal(input, result);
        }

        [Fact]
        public void TryGetParameter_ExistingKey_ShouldReturnTrueAndValue()
        {
            var input = "http://host1?param1=value1";
            var connectionString = ConnectionString.Parse(input);

            var success = connectionString.TryGetParameter("param1", out string value);

            Assert.True(success);
            Assert.Equal("value1", value);
        }

        [Fact]
        public void TryGetParameter_NonExistingKey_ShouldReturnFalse()
        {
            var input = "http://host1";
            var connectionString = ConnectionString.Parse(input);

            var success = connectionString.TryGetParameter("param1", out string value);

            Assert.False(success);
            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void GetBootstrapEndpoints_ShouldReturnCorrectPorts()
        {
            var input = "http://host1,host2:8091";
            var connectionString = ConnectionString.Parse(input);

            var endpoints = connectionString.GetBootstrapEndpoints().ToList();

            Assert.Equal(2, endpoints.Count);
            Assert.Equal(80, endpoints[0].Port);
            Assert.Equal(8091, endpoints[1].Port);
        }

        [Fact]
        public void Test_ConnectionString_TimeoutParameters()
        {
            var cstring = "http://localhost:8095?" +
                         "timeout.connect_timeout=5000&" +
                         "timeout.dispatch_timeout=15000&" +
                         "timeout.query_timeout=300000";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(TimeSpan.FromMilliseconds(5000), options.TimeoutOptions.ConnectTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(15000), options.TimeoutOptions.DispatchTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(300000), options.TimeoutOptions.QueryTimeout);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_PemFile()
        {
            var cstring = "https://localhost:8095?" +
                         "security.trust_only_pem_file=/path/to/certificate.pem";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal("/path/to/certificate.pem", options.SecurityOptions.PathToPemFileValue);
            Assert.Equal(CertificateTrustMode.PemFilePath, options.SecurityOptions.TrustMode);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_DisableCertificateVerification()
        {
            var cstring = "https://localhost:8095?" +
                         "security.disable_server_certificate_verification=true";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.True(options.SecurityOptions.DisableServerCertificateValidation);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_DisableCertificateVerification_False()
        {
            var cstring = "https://localhost:8095?" +
                         "security.disable_server_certificate_verification=false";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.False(options.SecurityOptions.DisableServerCertificateValidation);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_CipherSuites_Single()
        {
            var cstring = "https://localhost:8095?" +
                         "security.cipher_suites=Tls12";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(SslProtocols.Tls12, options.SecurityOptions.SslProtocols);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_CipherSuites_Multiple()
        {
            var cstring = "https://localhost:8095?" +
                         "security.cipher_suites=Tls12,Tls13";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls13, options.SecurityOptions.SslProtocols);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_CipherSuites_WithSpaces()
        {
            var cstring = "https://localhost:8095?" +
                         "security.cipher_suites=Tls12,Tls13";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls13, options.SecurityOptions.SslProtocols);
        }

        [Fact]
        public void Test_ConnectionString_SecurityParameters_CipherSuites_InvalidProtocol()
        {
            var cstring = "https://localhost:8095?" +
                         "security.cipher_suites=Tls12,InvalidProtocol,Tls13";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            // Invalid protocols should be ignored, only valid ones should be set
            Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls13, options.SecurityOptions.SslProtocols);
        }

        [Fact]
        public void Test_ConnectionString_AllParameters()
        {
            var cstring = "https://localhost:8095?" +
                         "timeout.connect_timeout=5000&" +
                         "timeout.dispatch_timeout=15000&" +
                         "timeout.query_timeout=300000&" +
                         "security.trust_only_pem_file=/path/to/certificate.pem&" +
                         "security.disable_server_certificate_verification=true&" +
                         "security.cipher_suites=Tls12,Tls13";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            // Verify timeout options
            Assert.Equal(TimeSpan.FromMilliseconds(5000), options.TimeoutOptions.ConnectTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(15000), options.TimeoutOptions.DispatchTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(300000), options.TimeoutOptions.QueryTimeout);

            // Verify security options
            Assert.Equal("/path/to/certificate.pem", options.SecurityOptions.PathToPemFileValue);
            Assert.Equal(CertificateTrustMode.PemFilePath, options.SecurityOptions.TrustMode);
            Assert.True(options.SecurityOptions.DisableServerCertificateValidation);
            Assert.Equal(SslProtocols.Tls12 | SslProtocols.Tls13, options.SecurityOptions.SslProtocols);
        }

        [Theory]
        [InlineData("1000", 1000)]
        [InlineData("0", 0)]
        [InlineData("60000", 60000)]
        public void Test_ConnectionString_TimeoutParameter_ConnectTimeout_Values(string timeoutValue, int expectedMilliseconds)
        {
            var cstring = $"http://localhost:8095?timeout.connect_timeout={timeoutValue}";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), options.TimeoutOptions.ConnectTimeout);
        }

        [Theory]
        [InlineData("2000", 2000)]
        [InlineData("0", 0)]
        [InlineData("120000", 120000)]
        public void Test_ConnectionString_TimeoutParameter_DispatchTimeout_Values(string timeoutValue, int expectedMilliseconds)
        {
            var cstring = $"http://localhost:8095?timeout.dispatch_timeout={timeoutValue}";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), options.TimeoutOptions.DispatchTimeout);
        }

        [Theory]
        [InlineData("30000", 30000)]
        [InlineData("0", 0)]
        [InlineData("600000", 600000)]
        public void Test_ConnectionString_TimeoutParameter_QueryTimeout_Values(string timeoutValue, int expectedMilliseconds)
        {
            var cstring = $"http://localhost:8095?timeout.query_timeout={timeoutValue}";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), options.TimeoutOptions.QueryTimeout);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("True", true)]
        [InlineData("False", false)]
        [InlineData("TRUE", true)]
        [InlineData("FALSE", false)]
        public void Test_ConnectionString_DisableServerCertificateVerification_Values(string boolValue, bool expectedValue)
        {
            var cstring = $"https://localhost:8095?security.disable_server_certificate_verification={boolValue}";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(expectedValue, options.SecurityOptions.DisableServerCertificateValidation);
        }

        [Theory]
        [InlineData("/etc/ssl/certs/certificate.pem")]
        [InlineData("C:\\certificates\\cert.pem")]
        [InlineData("./relative/path/to/cert.pem")]
        [InlineData("~/home/user/cert.pem")]
        public void Test_ConnectionString_TrustOnlyPemFile_Values(string pemFilePath)
        {
            var cstring = $"https://localhost:8095?security.trust_only_pem_file={pemFilePath}";

            var options = new ClusterOptions
            {
                ConnectionString = cstring
            };

            Assert.Equal(pemFilePath, options.SecurityOptions.PathToPemFileValue);
            Assert.Equal(CertificateTrustMode.PemFilePath, options.SecurityOptions.TrustMode);
        }
    }
}