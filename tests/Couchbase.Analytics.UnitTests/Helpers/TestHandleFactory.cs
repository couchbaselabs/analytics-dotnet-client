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
    {
        using var doc = JsonDocument.Parse(responseJson);
        return new QueryHandle(handle, requestId, doc.RootElement.Clone(), service);
    }

    public static QueryResultHandle CreateQueryResultHandle(string handlePath, string requestId, string responseJson, IAnalyticsService service)
    {
        using var doc = JsonDocument.Parse(responseJson);
        return new QueryResultHandle(handlePath, requestId, doc.RootElement.Clone(), service);
    }

    public static QueryStatus CreateQueryStatus(string requestId, string responseJson, IAnalyticsService service)
    {
        using var doc = JsonDocument.Parse(responseJson);
        return new QueryStatus(requestId, doc.RootElement.Clone(), service);
    }
}
