using System.Collections.Concurrent;
using System.Text.Json;

namespace Couchbase.Analytics2;

public record QueryOptions
{
    public TimeSpan? Timeout { get; init; } = TimeSpan.FromSeconds(100);

    public string ClientContextId { get; init; } = Guid.NewGuid().ToString();

    public Dictionary<string, object> NamedParameters { get; init; } = new();

    public List<object> PositionalParameters { get; init; } = new();

    public bool Priority { get; init; }

    public QueryScanConsistency ScanConsistency { get; init; }

    public TimeSpan? ScanWait { get; init; }

    //public JsonSerializer Serializer { get; init; }

    public bool ReadOnly { get; init; }

    public Dictionary<string, object> Raw {get;init;} = new();

    public CancellationToken CancellationToken { get; init; }

    internal string GetFormValuesAsJson(string statement)
    {
        return JsonSerializer.Serialize(GetFormValues(statement));
    }

    internal IDictionary<string, object> GetFormValues(string statement)
    {
        statement = CleanStatement(statement);
        var formValues = new Dictionary<string, object>
        {
            { "statement", statement },
            { "timeout", $"{Timeout?.TotalMilliseconds}ms" },
            { "client_context_id", ClientContextId },
            { "readonly", ReadOnly ? "true" : "false" },
            { "scan_consistency", ScanConsistency == QueryScanConsistency.NotBounded ? "not_bounded" : "request_plus" },
        };

        foreach (var parameter in NamedParameters)
        {
            formValues.Add(parameter.Key, parameter.Value);
        }

        if (PositionalParameters.Any())
        {
            formValues.Add("args", PositionalParameters.ToArray());
        }

        foreach (var rawParameter in Raw)
        {
            formValues.Add(rawParameter.Key, rawParameter.Value);
        }

        return formValues;
    }

    private string CleanStatement(string statement)
    {
        if (string.IsNullOrWhiteSpace(statement))
        {
            throw new ArgumentException("statement cannot be null or empty");
        }

        statement = statement.Trim();
        if (!statement.EndsWith(";"))
        {
            statement += ";";
        }

        return statement;
    }
}
