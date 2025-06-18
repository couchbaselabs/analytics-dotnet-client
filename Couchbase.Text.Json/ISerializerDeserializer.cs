namespace Couchbase.Text.Json;

public interface ISerializerDeserializer
{
    ISerializer Serializer { get; }
    
    IDeserializer Deserializer { get; }
}