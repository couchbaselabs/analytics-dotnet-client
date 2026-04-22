using System.Text.Json;
using Couchbase.AnalyticsClient.Async;
using Couchbase.AnalyticsClient.Internal;

namespace Couchbase.AnalyticsClient.UnitTests.Helpers;

/// <summary>
/// Convenience factory for creating handle objects from raw JSON strings in tests.
/// Keeps production constructors accepting only <see cref="JsonElement"/>.
/// </summary>
internal static class TestHandleFactory
{
    public static QueryHandle CreateQueryHandle(string handle, string requestId, string responseJson, IAnalyticsService service)
        => new(handle, requestId, JsonDocument.Parse(responseJson).RootElement, service);

    public static QueryResultHandle CreateQueryResultHandle(string handlePath, string requestId, string responseJson, IAnalyticsService service)
        => new(handlePath, requestId, JsonDocument.Parse(responseJson).RootElement, service);

    public static QueryStatus CreateQueryStatus(string requestId, string responseJson, IAnalyticsService service)
        => new(requestId, JsonDocument.Parse(responseJson).RootElement, service);
}
