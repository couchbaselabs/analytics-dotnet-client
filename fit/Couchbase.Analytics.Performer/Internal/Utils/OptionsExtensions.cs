using Couchbase.AnalyticsClient.Options;
using Couchbase.AnalyticsClient.Query;
using Couchbase.Grpc.Protocol.Columnar;
using Couchbase.Core;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace Couchbase.Analytics.Performer.Internal.Utils;

internal static class OptionsExtensions
{
    public static QueryOptions ToQueryOptions(this Couchbase.Grpc.Protocol.Columnar.ExecuteQueryRequest.Types.Options? protoOptions)
    {
        var queryOptions = new QueryOptions();
        if (protoOptions is null) return queryOptions;

        if (protoOptions.HasReadonly) queryOptions = queryOptions.WithReadOnly(protoOptions.Readonly);
        if (protoOptions.HasScanConsistency) queryOptions = queryOptions.WithScanConsistency(protoOptions.ScanConsistency.ToCore());
        if (protoOptions.Deserializer is not null) queryOptions = queryOptions.WithDeserializer(protoOptions.Deserializer.ToCore());
        if (protoOptions.Timeout is not null) queryOptions = queryOptions.WithTimeout(protoOptions.Timeout.ToTimeSpan());
        if (protoOptions.HasMaxRetries) queryOptions = queryOptions.WithMaxRetries((uint)protoOptions.MaxRetries);


        if (protoOptions.ParametersPositional is not null)
        {
            queryOptions = queryOptions.WithPositionalParameters(protoOptions.ParametersPositional.Values.ToCore());
        }

        if (protoOptions.ParametersNamed is not null)
        {
            queryOptions = queryOptions.WithNamedParameters(protoOptions.ParametersNamed.Fields.ToCore());
        }

        if (protoOptions.Raw is not null)
        {
            queryOptions = queryOptions.WithRawParameters(protoOptions.Raw.Fields.ToCore());
        }

        return queryOptions;
    }

    private static IDeserializer ToCore(this  Couchbase.Grpc.Protocol.Columnar.Deserializer protoDeserializer)
    {
        return protoDeserializer.TypeCase switch
            {
                Couchbase.Grpc.Protocol.Columnar.Deserializer.TypeOneofCase.Json => new StjJsonDeserializer(),
                Couchbase.Grpc.Protocol.Columnar.Deserializer.TypeOneofCase.Passthrough => new StjJsonDeserializer(),
                Couchbase.Grpc.Protocol.Columnar.Deserializer.TypeOneofCase.Custom => throw new NotSupportedException("Custom deserializer is not supported in .NET"),
                _ => throw new ArgumentOutOfRangeException(nameof(protoDeserializer.TypeCase), "Could not parse Deserializer")
            };

    }


    private static QueryScanConsistency ToCore(
        this ExecuteQueryRequest.Types.Options.Types.ScanConsistency protoScanConsistency)
    {
        switch (protoScanConsistency)
        {
            case ExecuteQueryRequest.Types.Options.Types.ScanConsistency.NotBounded:
                return QueryScanConsistency.NotBounded;
            case ExecuteQueryRequest.Types.Options.Types.ScanConsistency.RequestPlus:
                return QueryScanConsistency.RequestPlus;
            default:
                throw new ArgumentOutOfRangeException("Could not parse ScanConsistency");
        }
    }

    private static object? ToClrObject(Value value)
    {
        switch (value.KindCase)
        {
            case Value.KindOneofCase.NullValue:
                return null;
            case Value.KindOneofCase.NumberValue:
                // Prefer integer for integral numbers within Int64 range
                var number = value.NumberValue;
                if (number % 1 == 0 && number <= long.MaxValue && number >= long.MinValue)
                {
                    return (long)number;
                }
                return number; // double
            case Value.KindOneofCase.StringValue:
                return value.StringValue;
            case Value.KindOneofCase.BoolValue:
                return value.BoolValue;
            case Value.KindOneofCase.StructValue:
                return ToClrDictionary(value.StructValue);
            case Value.KindOneofCase.ListValue:
                return ToClrList(value.ListValue);
            case Value.KindOneofCase.None:
            default:
                throw new ArgumentOutOfRangeException($"Unsupported value type: {value.KindCase}");
        }
    }

    private static List<object> ToClrList(ListValue listValue)
    {
        return listValue.Values.Select(ToClrObject).Select(obj => obj!).ToList();
    }

    private static Dictionary<string, object> ToClrDictionary(Struct structValue)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kv in structValue.Fields)
        {
            var obj = ToClrObject(kv.Value);
            dict[kv.Key] = obj!;
        }
        return dict;
    }

    private static List<object> ToCore(this RepeatedField<Value> values)
    {
        return values.Select(ToClrObject).Select(obj => obj!).ToList();
    }

    internal static List<object> ToList(ListValue listValue)
    {
        return ToClrList(listValue);
    }

    private static Dictionary<string, object> ToCore(this MapField<string, Value> fields)
    {
        var dictionary = new Dictionary<string, object>();
        foreach (var paramValue in fields)
        {
            var obj = ToClrObject(paramValue.Value);
            dictionary[paramValue.Key] = obj!;
        }
        return dictionary;
    }
}