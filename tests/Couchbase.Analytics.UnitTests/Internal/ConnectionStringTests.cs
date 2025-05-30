// filepath: /Users/jeffry.morris/Documents/source/analytics-dotnet-client/src/Couchbase.Analytics/Internal/ConnectionStringTest.cs

using Couchbase.Analytics2.Internal;
using Xunit;

namespace Couchbase.Analytics2.UnitTests.Internal
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
            Assert.Equal(80, endpoints[0].Port); // Default HTTP port
            Assert.Equal(8091, endpoints[1].Port);
        }

        [Fact]
        public void IsValidDnsSrv_SingleHostWithoutPort_ShouldReturnTrue()
        {
            var input = "http://host1";
            var connectionString = ConnectionString.Parse(input);

            Assert.True(connectionString.IsValidDnsSrv());
        }

        [Fact]
        public void IsValidDnsSrv_MultipleHosts_ShouldReturnFalse()
        {
            var input = "http://host1,host2";
            var connectionString = ConnectionString.Parse(input);

            Assert.False(connectionString.IsValidDnsSrv());
        }
    }
}