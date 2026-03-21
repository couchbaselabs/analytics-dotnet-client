#region License
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2025 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion

using System.ComponentModel;
using System.Security.Authentication;
using Couchbase.AnalyticsClient.DI;
using Couchbase.AnalyticsClient.HTTP;
using Couchbase.AnalyticsClient.Internal;
using Couchbase.AnalyticsClient.Internal.DI;
using Couchbase.AnalyticsClient.Internal.Utils;
using Couchbase.AnalyticsClient.Logging;
using Couchbase.Core.Json;
using Couchbase.Core.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Couchbase.AnalyticsClient.Options;

public record ClusterOptions
{
    public SecurityOptions SecurityOptions { get; private set; } = new();

    public TimeoutOptions TimeoutOptions { get; private set; } = new();

    [InterfaceStability(StabilityLevel.Volatile)]
    public uint MaxRetries { get; private set; } = 7;

    public IDeserializer Deserializer { get; private set; } = new StjJsonDeserializer();

    internal ConnectionString? ConnectionStringValue { get; private set; }

    /// <summary>
    /// The level of log redaction to apply. Default is <see cref="RedactionLevel.None"/>.
    /// </summary>
    public RedactionLevel RedactionLevel { get; private set; } = RedactionLevel.None;

    private ILoggerFactory? Logging { get; set; }

    public ClusterOptions WithSecurityOptions(SecurityOptions securityOptions)
    {
        return this with { SecurityOptions = securityOptions };
    }

    public ClusterOptions WithSecurityOptions(Func<SecurityOptions, SecurityOptions> securityOptions)
    {
        SecurityOptions = securityOptions.Invoke(SecurityOptions);
        return this;
    }

    public ClusterOptions WithTimeoutOptions(TimeoutOptions timeoutOptions)
    {
        return this with { TimeoutOptions = timeoutOptions };
    }

    public ClusterOptions WithTimeoutOptions(Func<TimeoutOptions, TimeoutOptions> securityOptions)
    {
        TimeoutOptions = securityOptions.Invoke(TimeoutOptions);
        return this;
    }

    [InterfaceStability(StabilityLevel.Volatile)]
    public ClusterOptions WithMaxRetries(uint maxRetries)
    {
        return this with { MaxRetries = maxRetries };
    }

    /// <summary>
    /// Set the <see cref="ILoggerFactory"/> to use for logging.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>
    /// A copy of this <see cref="ClusterOptions"/> object for method chaining.
    /// </returns>
    public ClusterOptions WithLogging(ILoggerFactory? loggerFactory = null)
    {
        return this with { Logging = loggerFactory };
    }

    /// <summary>
    /// Sets the <see cref="IDeserializer"/> to use for deserializing JSON responses.
    /// This can be overridden on a per-operation basis by passing a deserializer to the <see cref="QueryOptions"/>.
    /// </summary>
    /// <param name="deserializer">An implementation of <see cref="IDeserializer"/></param>
    /// <returns>A copy of this <see cref="ClusterOptions"/> object for method chaining.</returns>
    public ClusterOptions WithDeserializer(IDeserializer deserializer)
    {
        return this with { Deserializer = deserializer };
    }

    /// <summary>
    /// Set the <see cref="Logging.RedactionLevel"/> to use for log redaction.
    /// </summary>
    /// <param name="redactionLevel">The redaction level.</param>
    /// <returns>A copy of this <see cref="ClusterOptions"/> object for method chaining.</returns>
    public ClusterOptions WithRedactionLevel(RedactionLevel redactionLevel)
    {
        return this with { RedactionLevel = redactionLevel };
    }

    private readonly IDictionary<Type, IServiceFactory> _services = DefaultServices.GetDefaultServices();

    internal ICouchbaseServiceProvider BuildServiceProvider(ICredential? credential = null)
    {
        this.AddClusterService(this);
        this.AddClusterService(Logging ??= new NullLoggerFactory());
        this.AddClusterService(new TypedRedactor(RedactionLevel));
        if (credential is not null) this.AddClusterService(credential);
        return new CouchbaseServiceProvider(_services);
    }

    /// <summary>
    /// Register a service with the cluster's <see cref="CouchbaseServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TService">The type of the service which will be requested.</typeparam>
    /// <typeparam name="TImplementation">The type of the service implementation which is returned.</typeparam>
    /// <param name="factory">Factory which will create the service.</param>
    /// <param name="lifetime">Lifetime of the service.</param>
    /// <returns>The <see cref="ClusterOptions"/>.</returns>
    public ClusterOptions AddService<TService, TImplementation>(
        Func<IServiceProvider, TImplementation> factory,
        ClusterServiceLifetime lifetime)
        where TImplementation : notnull, TService
    {
        _services[typeof(TService)] = lifetime switch
        {
            ClusterServiceLifetime.Transient => new TransientServiceFactory(serviceProvider => factory(serviceProvider)),
            ClusterServiceLifetime.Cluster => new SingletonServiceFactory(serviceProvider => factory(serviceProvider)),
            _ => throw new InvalidEnumArgumentException(nameof(lifetime), (int)lifetime,
                typeof(ClusterServiceLifetime))
        };

        return this;
    }

    /// <summary>
    /// The connection string for the cluster.
    /// </summary>
    internal string? ConnectionString
    {
        get => ConnectionStringValue?.ToString();
        set
        {
            ConnectionStringValue = value != null ? Internal.ConnectionString.Parse(value) : null;

            if (ConnectionStringValue == null) return;

            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.MaxRetries, out uint maxRetries))
            {
                MaxRetries = maxRetries;
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.ConnectTimeout, out TimeSpan connectTimeout))
            {
                TimeoutOptions = TimeoutOptions.WithConnectTimeout(connectTimeout);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.DispatchTimeout, out TimeSpan dispatchTimeout))
            {
                TimeoutOptions = TimeoutOptions.WithDispatchTimeout(dispatchTimeout);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.QueryTimeout, out TimeSpan queryTimeout))
            {
                TimeoutOptions = TimeoutOptions.WithQueryTimeout(queryTimeout);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.TrustOnlyPemFile, out string pathToPemFile))
            {
                SecurityOptions = SecurityOptions.WithTrustOnlyPemFile(pathToPemFile);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.DisableServerCertificateVerification, out bool disableServerCertificateVerification))
            {
                SecurityOptions = SecurityOptions.WithDisableCertificateVerification(disableServerCertificateVerification);
            }
            if (ConnectionStringValue.TryGetParameter(ConnectionStringParams.CipherSuites, out string? cipherSuites))
            {
                var protocolStrings = cipherSuites.Split(',');
                var protocols = SslProtocols.None;

                foreach (var protocolString in protocolStrings)
                {
                    if (Enum.TryParse<SslProtocols>(protocolString.Trim(), ignoreCase: true, out var protocol))
                    {
                        protocols |= protocol;
                    }
                }

                SecurityOptions = SecurityOptions.WithSslProtocols(protocols);
            }
        }
    }
}
