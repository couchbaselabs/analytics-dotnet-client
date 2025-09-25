# Getting Started

This guide shows how to install the package, connect to a Couchbase cluster, and run Analytics queries.

## Install

Add the package to your project:

```bash
dotnet add package Couchbase.Analytics
```

Requires .NET 8.0.

## Connect

Create a `Cluster` with a connection string and `Credential`. The connection string supports `http` or `https`, multiple hosts, and query/timeout/TLS parameters.

```csharp
using Couchbase.AnalyticsClient;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Options;

var credential = Credential.Create("username", "password");

var cluster = Cluster.Create(
    connectionString: "https://analytics.my-couchbase.example.com:18095?max_retries=5",
    credential: credential,
    configureOptions: options => options
        .WithTimeoutOptions(timeoutOpts => timeoutOpts
            .WithQueryTimeout(TimeSpan.FromSeconds(15)))
        .WithSecurityOptions(securityOpts => securityOpts
            .WithTrustOnlyCapella())
);
```

Notes:
- Use `http://host:8095` for non-TLS clusters, `https://host:18095` for TLS (or your own custom ports for a load balancer or proxy)
- If multiple IP addresses are resolved for a host, a connection will be attempted for a random IP address. If a connection attempt fails, another IP will be picked to attempt a connection, until all are exhausted.
- Connection string parameters include:
  - `timeout.connect_timeout`, `timeout.dispatch_timeout`, `timeout.query_timeout` (in milliseconds)
  - `security.trust_only_pem_file`, `security.disable_server_certificate_verification`, `security.cipher_suites`
  - `max_retries`

## Query

Run an Analytics statement and stream rows:

Note: Results are streamed by default. Use `QueryOptions.WithAsStreaming(false)` to get a blocking result.

```csharp
using Couchbase.AnalyticsClient.Options;

var result = await cluster.ExecuteQueryAsync(
    "SELECT 1 AS one;",
    new QueryOptions()
        .WithReadOnly(true)
        .WithScanConsistency(QueryScanConsistency.RequestPlus)
);

await foreach (var row in result.Rows)
{
    Console.WriteLine(row.ContentAs<MyPOCO>());
}
```

### Query with parameters

```csharp
var statement = "SELECT * FROM `travel-sample`.inventory.airline WHERE country = $country LIMIT $limit";

var paramResult = await cluster.ExecuteQueryAsync(
    statement,
    new QueryOptions()
        .WithNamedParameter("country", "United States")
        .WithNamedParameter("limit", 10)
);
```

### Database and scope context

Target a specific database and scope using `Database(...).Scope(...).ExecuteQueryAsync(...)`:

```csharp
var db = cluster.Database("travel-sample");
var scope = db.Scope("inventory");

var scoped = await scope.ExecuteQueryAsync(
    "SELECT META().id FROM airline LIMIT 5"
);

await foreach (var row in scoped.Rows)
{
    Console.WriteLine(row.Json.ToString());
}
```

## Cleanup

`Cluster` implements `IDisposable`. Dispose when done to release resources:

```csharp
cluster.Dispose();
```