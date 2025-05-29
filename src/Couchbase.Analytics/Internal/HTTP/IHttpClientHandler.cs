namespace Couchbase.Analytics2.Internal.HTTP;

public interface IHttpClientHandler
{
    Task<HttpResponseMessage> GetAsync(string url);
    Task<HttpResponseMessage> PostAsync(string url, HttpContent content);
}
