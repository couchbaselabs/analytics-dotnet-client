using System.Net.Mime;
using System.Text.Json;

namespace Couchbase.Analytics2.Internal.Serialization;

public interface IJsonSerializer
{
   ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
   
   ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancellationToken = default);
}