namespace Couchbase.Analytics2.Internal.Utils;

internal static class ConnectionStringParams
{
    public const string ConnectTimeout = "timeout.connect_timeout";
    public const string DispatchTimeout = "timeout.dispatch_timeout";
    public const string QueryTimeout = "timeout.query_timeout";

    public const string TrustOnlyPemFile = "security.trust_only_pem_file";
    public const string DisableServerCertificateVerification = "security.disable_server_certificate_verification";
    public const string CipherSuites = "security.cipher_suites";
}