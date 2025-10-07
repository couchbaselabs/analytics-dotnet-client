using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using Couchbase.AnalyticsClient.DI;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal.HTTP;
using Couchbase.AnalyticsClient.Json;
using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Query;
using Moq.Protected;

namespace Couchbase.AnalyticsClient.UnitTests.Internal;

public class QueryContextTests
{
    [Fact]
    public void Scope_ExecuteQueryAsync_SetsQueryContextCorrectly()
    {
        var database = CreateTestDatabase("testDatabase");
        var scope = database.Scope("testScope");

        var options = new QueryOptions { AsStreaming = false, ReadOnly = true };

        options = options with { QueryContext = new QueryContext(database.Name, scope.Name) };

        Assert.NotNull(options.QueryContext);
        Assert.Equal("testDatabase", options.QueryContext.Database);
        Assert.Equal("testScope", options.QueryContext.Scope);
        Assert.Equal("default:`testDatabase`.`testScope`", options.QueryContext.ToString());
    }


    [Fact]
    public void QueryOptions_GetFormValues_IncludesQueryContextWhenNotNull()
    {
        var queryContext = new QueryContext("testDb", "testScope");
        var options = new QueryOptions
        {
            QueryContext = queryContext
        };

        var formValues = options.GetFormValues("SELECT * FROM collection");

        Assert.True(formValues.ContainsKey("query_context"));
        Assert.Equal("default:`testDb`.`testScope`", formValues["query_context"]);
    }


    [Fact]
    public void QueryContext_Constructor_ValidParameters_SetsPropertiesCorrectly()
    {
        var queryContext = new QueryContext("myDatabase", "myScope");

        Assert.Equal("myDatabase", queryContext.Database);
        Assert.Equal("myScope", queryContext.Scope);
        Assert.Equal("default:`myDatabase`.`myScope`", queryContext.ToString());
    }

    [Fact]
    public void QueryContext_Constructor_NullDatabase_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new QueryContext(null!, "scope"));
        Assert.Equal("databaseName", exception.ParamName);
    }

    [Fact]
    public void QueryContext_Constructor_NullScope_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new QueryContext("database", null!));
        Assert.Equal("scopeName", exception.ParamName);
    }

    [Fact]
    public void QueryContext_Constructor_DatabaseWithBacktick_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new QueryContext("data`base", "scope"));
        Assert.Contains("Database name must not contain backtick (`), but got: data`base", exception.Message);
        Assert.Equal("databaseName", exception.ParamName);
    }

    [Fact]
    public void QueryContext_Constructor_ScopeWithBacktick_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => new QueryContext("database", "sco`pe"));
        Assert.Contains("Scope name must not contain backtick (`), but got: sco`pe", exception.Message);
        Assert.Equal("scopeName", exception.ParamName);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("db", "")]
    [InlineData("", "scope")]
    public void QueryContext_Constructor_ValidEdgeCases_ThrowCorrectly(string database, string scope)
    {
        Assert.Throws<ArgumentException>(() => new QueryContext(database, scope));
    }

    private static Database CreateTestDatabase(string name)
    {
        var cluster = CreateTestCluster();
        return cluster.Database(name);
    }

    private static Cluster CreateTestCluster()
    {
        var httpClientFactoryMock = new Mock<ICouchbaseHttpClientFactory>();
        var clusterLoggerMock = new Mock<ILogger<Cluster>>();
        var deserializerMock = new Mock<IDeserializer>();
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        httpClientFactoryMock.Setup(f => f.Create()).Returns(() => new HttpClient(httpMessageHandlerMock.Object));

        // Setup a basic response to avoid network calls
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var connectionString = "http://127.0.0.1";
        var credential = new Credential("Administrator", "password");

        var clusterOptions = new ClusterOptions { ConnectionString = connectionString }
            .AddService<ICouchbaseHttpClientFactory, ICouchbaseHttpClientFactory>(
                _ => httpClientFactoryMock.Object,
                ClusterServiceLifetime.Cluster)
            .AddService<ILogger<Cluster>, ILogger<Cluster>>(
                _ => clusterLoggerMock.Object,
                ClusterServiceLifetime.Cluster)
            .AddService<IDeserializer, IDeserializer>(
                _ => deserializerMock.Object,
                ClusterServiceLifetime.Cluster);

        return Cluster.Create(credential, clusterOptions);
    }
}