namespace Couchbase.Analytics2;

public interface ICredential
{
    /// <summary>
    /// The username to authenticate against.
    /// </summary>
    string Username { get; init; }

    /// <summary>
    /// The password of the principle
    /// </summary>
    string Password { get; init; }

    bool Equals(Credential? other);

    bool Equals(object? other);

    int GetHashCode();

    void Deconstruct(out string Username, out string Password);

    string ToString();
}
