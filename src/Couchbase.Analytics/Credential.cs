namespace Couchbase.Analytics2;

public record Credential(string Username, string Password) : ICredential
{
    public static Credential Create(string username, string password)
    {
        return new (username, password);
    }
}
