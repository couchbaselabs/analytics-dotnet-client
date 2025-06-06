using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.Analytics2.FunctionalTests;

public class ClusterTests
{
    private readonly ITestOutputHelper _output;

    public ClusterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Test_QueryAsync()
    {
        // Try the correct Analytics port instead of default HTTPS port
        var connectionString = "https://cb.2xg3vwszqgqcrsix.cloud.couchbase.com:18095"; // Analytics HTTPS port
        var username = "Administrator";
        var password = "Password123!";

        var cluster = Cluster.Create(connectionString, new Credential(username, password));

        try
        {
            using var response = await cluster.ExecuteQueryAsync<dynamic>("SELECT 'hello' AS greeting;");
            Assert.NotNull(response);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Query failed with exception: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                _output.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    [Fact]
    public async Task Test_QueryAsync_WithDisabledCertValidation()
    {
        var connectionString = "https://cb.2xg3vwszqgqcrsix.cloud.couchbase.com:18095";
        var username = "Administrator";
        var password = "Password123!";

        var clusterOptions = new ClusterOptions();
        clusterOptions.SecurityOptions.DisableCertificateVerification(true);

        try
        {
            var cluster = Cluster.Create(connectionString, new Credential(username, password), clusterOptions);

            var queryOptions = new QueryOptions { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await cluster.ExecuteQueryAsync<dynamic>("SELECT 'hello' AS greeting;", queryOptions);
            Assert.NotNull(response);
        }
        catch (Exception ex)
        {
            _output.WriteLine($"FAILED even with disabled cert validation: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
            {
                _output.WriteLine($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            throw;
        }
    }
}