using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Couchbase.Analytics2.Internal.Certificates;

namespace Couchbase.Columnar.Internal;

/// <summary>
    /// Default implementation of <see cref="CouchbaseHttpClientFactory"/>.
    /// </summary>
    internal class CouchbaseHttpClientFactory : ICouchbaseHttpClientFactory
    {
        private readonly ClusterOptions _options;
        private readonly ILogger<CouchbaseHttpClientFactory> _logger;

        private readonly HttpMessageHandler _sharedHandler;

        public CouchbaseHttpClientFactory(ClusterOptions options, ILogger<CouchbaseHttpClientFactory> logger)
        {
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (options == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(logger));
            }

            // ReSharper restore ConditionIsAlwaysTrueOrFalse

            _options = options;
            _logger = logger;

            DefaultCompletionOption = _options.ClusterOptions.Tuning.StreamHttpResponseBodies
                ? HttpCompletionOption.ResponseHeadersRead
                : HttpCompletionOption.ResponseContentRead;

            _sharedHandler = CreateClientHandler();
        }

        /// <inheritdoc />
        public HttpCompletionOption DefaultCompletionOption { get; }

        /// <inheritdoc />
        public HttpClient Create()

        {
            var httpClient = new HttpClient(_sharedHandler, false)
            {
                DefaultRequestHeaders =
                {
                    ExpectContinue = _options.ClusterOptions.EnableExpect100Continue
                }
            };

#if NET5_0_OR_GREATER
            //experimental support for HTTP V.2
            if (_options.ClusterOptions.Experiments.EnableHttpVersion2)
            {
                httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                httpClient.DefaultRequestVersion = HttpVersion.Version20;
            }
#endif

            ClientIdentifier.SetUserAgent(httpClient.DefaultRequestHeaders);

            return httpClient;
        }

        private HttpMessageHandler CreateClientHandler()
        {
            var clusterOptions = _options.ClusterOptions;

            if (clusterOptions.IsCapella && !clusterOptions.EffectiveEnableTls)
            {
                _logger.LogWarning("TLS is required when connecting to Couchbase Capella. Please enable TLS by prefixing the connection string with \"couchbases://\" (note the final 's').");
            }

#if !NETCOREAPP3_1_OR_GREATER
            var handler = new HttpClientHandler();

            //for x509 cert authentication
            if (_context.ClusterOptions.X509CertificateFactory != null)
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = _context.ClusterOptions.EnabledSslProtocols;
                handler.ClientCertificates.AddRange(_context.ClusterOptions.X509CertificateFactory.GetCertificates());
            }

            try
            {
                handler.CheckCertificateRevocationList = _context.ClusterOptions.EnableCertificateRevocation;
                handler.ServerCertificateCustomValidationCallback =
                    CreateCertificateValidator(_context.ClusterOptions);
            }
            catch (PlatformNotSupportedException)
            {
                _logger.LogDebug(
                    "Cannot set ServerCertificateCustomValidationCallback, not supported on this platform");
            }
            catch (NotImplementedException)
            {
                _logger.LogDebug(
                    "Cannot set ServerCertificateCustomValidationCallback, not implemented on this platform");
            }
#else
            var handler = new SocketsHttpHandler();

            X509Certificate2Collection? certs = null;
            //for x509 cert authentication
            if (_options.ClusterOptions.X509CertificateFactory != null)
            {
                handler.SslOptions.EnabledSslProtocols = _options.ClusterOptions.EnabledSslProtocols;

                certs = _options.ClusterOptions.X509CertificateFactory.GetCertificates();
                handler.SslOptions.ClientCertificates = certs;

                // This emulates the behavior of HttpClientHandler in Manual mode, which selects the first certificate
                // from the list which is eligible for use as a client certificate based on having a private key and
                // the correct key usage flags.
                handler.SslOptions.LocalCertificateSelectionCallback =
                    (_, _, _, _, _) => CertificateValidation.GetClientCertificate(certs)!;
            }

            // We don't need to check for unsupported platforms here, because this code path only applies to recent
            // versions of .NET which all support certificate validation callbacks
            handler.SslOptions.CertificateRevocationCheckMode = _options.ClusterOptions.EnableCertificateRevocation
                ? X509RevocationMode.Online
                : X509RevocationMode.NoCheck;

            RemoteCertificateValidationCallback? certValidationCallback = _options.ClusterOptions.HttpCertificateCallbackValidation;
            if (certValidationCallback == null)
            {
                CallbackCreator callbackCreator = new CallbackCreator( _options.ClusterOptions.HttpIgnoreRemoteCertificateMismatch, _logger, _redactor, certs);
                certValidationCallback = certValidationCallback = (__sender, __certificate, __chain, __sslPolicyErrors) =>
                    callbackCreator.Callback(__sender, __certificate, __chain, __sslPolicyErrors);
            }

            handler.SslOptions.RemoteCertificateValidationCallback = certValidationCallback;

            if (_options.ClusterOptions.PlatformSupportsCipherSuite
                && _options.ClusterOptions.EnabledTlsCipherSuites.Count > 0)
            {
                handler.SslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(_options.ClusterOptions.EnabledTlsCipherSuites);
            }

            if (_options.ClusterOptions.IdleHttpConnectionTimeout > TimeSpan.Zero)
            {
                //https://issues.couchbase.com/browse/MB-37032
                handler.PooledConnectionIdleTimeout = _options.ClusterOptions.IdleHttpConnectionTimeout;
            }

            if (_options.ClusterOptions.HttpConnectionLifetime > TimeSpan.Zero)
            {
                handler.PooledConnectionLifetime = _options.ClusterOptions.HttpConnectionLifetime;
            }

#endif

#if NET5_0_OR_GREATER
            if (_options.ClusterOptions.EnableTcpKeepAlives)
            {
                handler.KeepAlivePingDelay = _options.ClusterOptions.TcpKeepAliveInterval;
                handler.KeepAlivePingTimeout = _options.ClusterOptions.TcpKeepAliveTime;
                handler.KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;
            }
#endif
            try
            {
                if (_options.ClusterOptions.MaxHttpConnections > 0)
                {
                    //0 means the WinHttpHandler default size of Int.MaxSize is used
                    handler.MaxConnectionsPerServer = _options.ClusterOptions.MaxHttpConnections;
                }
            }
            catch (PlatformNotSupportedException e)
            {
                _logger.LogDebug(e, "Cannot set MaxConnectionsPerServer, not supported on this platform");
            }

            return new AuthenticatingHttpMessageHandler(handler, _options);
        }

#if !NETCOREAPP3_1_OR_GREATER
        private Func<HttpRequestMessage, X509Certificate, X509Chain, SslPolicyErrors, bool>
            CreateCertificateValidator(ClusterOptions clusterOptions)
        {
            bool OnCertificateValidation(HttpRequestMessage request, X509Certificate certificate,
                X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                var callback = clusterOptions.HttpCertificateCallbackValidation;
                if (callback == null)
                {
                    CallbackCreator callbackCreator = new CallbackCreator(clusterOptions.HttpIgnoreRemoteCertificateMismatch, _logger, _redactor, null);
                    callback = (__sender, __certificate, __chain, __sslPolicyErrors) =>
                        callbackCreator.Callback(__sender, __certificate, __chain, __sslPolicyErrors);
                }
                return callback(request, certificate, chain, sslPolicyErrors);
            }

            return OnCertificateValidation;
        }
#endif
    }
}
