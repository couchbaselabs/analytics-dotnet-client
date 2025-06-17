namespace Couchbase.Text.Json;

/// <summary>
/// The Default serializer that is based on System.Text.Json
/// </summary>
public interface ISerializer
{
   /// <summary>
   /// Deserializes a stream of JSON into an object.
   /// </summary>
   /// <param name="stream">The stream of JSON bytes.</param>
   /// <param name="cancellationToken">An optional CancellationToken.</param>
   /// <typeparam name="T">The type to deserialize into.</typeparam>
   /// <returns>A ValueTask that can be awaited.</returns>
   ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
   
   
   /// <summary>
   /// Serializes .NET objects into a stream.
   /// </summary>
   /// <param name="stream">The stream of JSON bytes.</param>
   /// <param name="obj">The </param>
   /// <param name="cancellationToken">An optional CancellationToken.</param>
   /// <typeparam name="T">The type to deserialize into.</typeparam>
   /// <returns>A ValueTask that can be awaited.</returns>
   ValueTask SerializeAsync<T>(Stream stream, T obj, CancellationToken cancellationToken = default);
   
   /// <summary>
   /// Creates a Json stream reader based on STJ.
   /// </summary>
   /// <param name="stream">The stream to read from.</param>
   /// <param name="cancellationToken">An optional CancellationToken.</param>
   /// <returns>The <see cref="IJsonStreamReader"/> to read the stream.</returns>
   IJsonStreamReader CreateJsonStreamReader(Stream stream, CancellationToken cancellationToken = default);
}