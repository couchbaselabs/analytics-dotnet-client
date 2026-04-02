# Getting Started

This guide shows how to install the package, connect to a Couchbase cluster, and run Analytics queries.

## Install

Add the package to your project:

```bash
dotnet add package Couchbase.AnalyticsClient
```

Requires .NET 8.0.

### Connect

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

#### JWT Authentication

To authenticate with a JSON Web Token (JWT) instead of username and password:

```csharp
var credential = JwtCredential.Create("xxxxx.yyyyy.zzzzz");

var cluster = Cluster.Create(
    connectionString: "https://analytics.my-couchbase.example.com:18095",
    credential: credential
);
```

#### Updating Credentials

After the cluster is created, you can supply a new credential (of the same type) for all subsequent requests:

```csharp
cluster.UpdateCredential(Credential.Create("newuser", "newpassword"));
// or
cluster.UpdateCredential(JwtCredential.Create("new.jwt.token"));
```

> [!NOTE]
> Use `http://host:8095` for non-TLS connections, `https://host:18095` for TLS (or your own custom ports for a load balancer or proxy)
>
> If multiple IP addresses are resolved for a host, a connection will be attempted for a random one. If that connection attempt fails, another IP will be picked to attempt a connection, until all are exhausted.
>
> Connection string parameters include:
> - `timeout.connect_timeout`, `timeout.dispatch_timeout`, `timeout.query_timeout`
> - `security.trust_only_pem_file`, `security.disable_server_certificate_verification`, `security.cipher_suites`
> - `max_retries`

### Query

Run an Analytics statement and stream rows:

> [!NOTE]
> Results are streamed by default. Use `QueryOptions.WithAsStreaming(false)` to get a blocking result.

```csharp
using Couchbase.AnalyticsClient.Options;

var result = await cluster.ExecuteQueryAsync(
    "SELECT 1 AS one;",
    new QueryOptions()
        .WithReadOnly(true)
        .WithScanConsistency(QueryScanConsistency.RequestPlus)
).ConfigureAwait(false);

await foreach (var row in result.ConfigureAwait(false))
{
    Console.WriteLine(row.ContentAs<JsonElement>());
}
```

### Query with parameters

```csharp
var statement = "SELECT * FROM `travel-sample`.inventory.airline WHERE country = $country LIMIT $limit";

var paramResult = await _analytics2Fixture.Cluster.ExecuteQueryAsync(
    statement,
    new QueryOptions()
        .WithNamedParameter("country", "United States")
        .WithNamedParameter("limit", 10)
).ConfigureAwait(false);

await foreach (var row in paramResult.ConfigureAwait(false))
{
    Console.WriteLine(row.ContentAs<JsonElement>());
}   

/** Output:
{"airline":{"id":"airline_19433","type":"airline","name":"XAIR USA","iata":"XA","icao":"XAU","callsign":"XAIR","country":"United States"}}
...
*/
```

### Database and scope context

Target a specific database and scope using `Database(...).Scope(...).ExecuteQueryAsync(...)`:

```csharp
var db = cluster.Database("travel-sample");
var scope = db.Scope("inventory");

var scoped = await scope.ExecuteQueryAsync(
    "SELECT id FROM airline LIMIT 5"
).ConfigureAwait(false);

await foreach (var row in scoped.ConfigureAwait(false))
{
    Console.WriteLine(row.ContentAs<JsonElement>());
}

/** Output:
{"id":"airline_19433"}
{"id":"airline_137"}
{"id":"airline_18239"}
{"id":"airline_10123"}
{"id":"airline_19290"}
*/
```

### Options

> [!WARNING]
> Option classes are immutable records. Each mutation returns a new instance of the options.

Initialize, or modify options using:

`With` methods return a new instance of the options, to allow chaining:

```csharp
var options = new QueryOptions()
    .WithReadOnly(true)
    .WithScanConsistency(QueryScanConsistency.RequestPlus);
```

Or use the initializer syntax:
```csharp
var options = new QueryOptions()
{
    ReadOnly = true,
    ScanConsistency = QueryScanConsistency.RequestPlus
}

// or

options = options with {
    ReadOnly = true,
    ScanConsistency = QueryScanConsistency.RequestPlus
}
```

### Cleanup

`Cluster` implements `IDisposable`. Dispose when done to release resources:

```csharp
cluster.Dispose();
```
