using System.Text.Json;
using Couchbase.AnalyticsClient.Public.Results;
using Couchbase.Grpc.Protocol.Columnar;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Couchbase.Analytics.Performer.Internal.Utils;

public static class ContentAsExtensions
{
    public static ContentWas ContentAsToAnalyticsRow(this AnalyticsRow analyticsRow, Couchbase.Grpc.Protocol.Columnar.ContentAs contentAs)
    {
        switch (contentAs.AsCase)
        {
            case ContentAs.AsOneofCase.AsByteArray:
                return new ContentWas { ContentWasBytes = ByteString.CopyFrom(analyticsRow.ContentAs<byte[]>()) };
            case ContentAs.AsOneofCase.AsList:
                var listJson = analyticsRow.ContentAs<JsonElement>();
                var listValue = ConvertJsonArrayToList(listJson);
                return new ContentWas { ContentWasList = listValue };
            case ContentAs.AsOneofCase.AsMap:
                var json = analyticsRow.ContentAs<JsonElement>();
                var structValue = ConvertJsonObjectToStruct(json);
                return new ContentWas { ContentWasMap = structValue };
            case ContentAs.AsOneofCase.AsString:
                return new ContentWas { ContentWasString = analyticsRow.ContentAs<string>() };
            case ContentAs.AsOneofCase.None:
            default:
                throw new ArgumentException("No contentAs specified");
        }
    }

    private static Struct ConvertJsonObjectToStruct(JsonElement jsonObject)
    {
        var result = new Struct();
        foreach (var property in jsonObject.EnumerateObject())
        {
            result.Fields[property.Name] = ConvertJsonToProtoValue(property.Value);
        }
        return result;
    }

    private static ListValue ConvertJsonArrayToList(JsonElement jsonArray)
    {
        var list = new ListValue();
        foreach (var item in jsonArray.EnumerateArray())
        {
            list.Values.Add(ConvertJsonToProtoValue(item));
        }
        return list;
    }

    private static Value ConvertJsonToProtoValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return new Value { StringValue = element.GetString() };
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                {
                    return new Value { NumberValue = longValue };
                }
                if (element.TryGetDouble(out var doubleValue))
                {
                    return new Value { NumberValue = doubleValue };
                }
                return new Value { NumberValue = double.Parse(element.GetRawText(), System.Globalization.CultureInfo.InvariantCulture) };
            case JsonValueKind.True:
            case JsonValueKind.False:
                return new Value { BoolValue = element.GetBoolean() };
            case JsonValueKind.Object:
                return new Value { StructValue = ConvertJsonObjectToStruct(element) };
            case JsonValueKind.Array:
                return new Value { ListValue = ConvertJsonArrayToList(element) };
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
            default:
                return new Value { NullValue = NullValue.NullValue };
        }
    }
}