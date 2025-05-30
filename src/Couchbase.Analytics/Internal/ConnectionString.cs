using System.Text;
using System.Text.RegularExpressions;
using Couchbase.Analytics2.Internal.Utils;

#nullable enable

namespace Couchbase.Analytics2.Internal;

internal class ConnectionString
{
    private const int HttpsPort = 443;
    private const int HttpPort = 80;

    private static readonly Regex ConnectionStringRegex = new Regex(
        "^((?<scheme>[^://]+)://)?((?<username>[^\n@]+)@)?(?<hosts>[^\n?]+)?(\\?(?<params>(.+)))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    private static readonly Regex Ipv6Regex = new Regex(
        "^\\[(?<address>.+)](:?(?<port>[0-9]+))?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public Scheme Scheme { get; private set; } = Scheme.Http;
    public string? Username { get; private set; }
    public IList<HostEndpoint> Hosts { get; private set; } = new List<HostEndpoint>();
    public IDictionary<string, string> Parameters { get; private set; } = new Dictionary<string, string>();

    public bool IsDnsSrv { get; private set; }

    public Uri? DnsSrvUri { get; private set; }

    /// <summary>
    /// Gets or sets a value that determines whether host names are provided in random order during bootstrapping. (default: false)
    /// </summary>
    /// <remarks>The RFC specifies that hosts should be used in random order, but the existing behavior is serial.</remarks>
    public bool RandomizeSeedHosts { get; set; } = false;

    private ConnectionString()
    {
    }

    public ConnectionString(ConnectionString source, IEnumerable<HostEndpoint> newHosts)
    {
        Scheme = source.Scheme;
        Username = source.Username;
        Hosts = newHosts.ToList();
        Parameters = source.Parameters;
    }

    internal ConnectionString(ConnectionString source, IEnumerable<HostEndpoint> newHosts, bool isDnsSrv, Uri dnsSrvUri)
        : this(source, newHosts)
    {
        IsDnsSrv = isDnsSrv;
        DnsSrvUri = dnsSrvUri;
    }

    internal static ConnectionString Parse(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var match = ConnectionStringRegex.Match(input);
        if (!match.Success)
        {
            throw new ArgumentException("Invalid connection string");
        }

        var connectionString = new ConnectionString();

        if (match.Groups["scheme"].Success)
        {
            switch (match.Groups["scheme"].Value)
            {
                case "http":
                    connectionString.Scheme = Scheme.Http;
                    break;
                case "https":
                    connectionString.Scheme = Scheme.Https;
                    break;
                default:
                    throw new ArgumentException($"Unknown scheme {match.Groups["scheme"].Value}");
            }
        }

        if (match.Groups["username"].Success)
        {
            connectionString.Username = match.Groups["username"].Value;
        }

        if (match.Groups["hosts"].Success)
        {
            connectionString.Hosts = match.Groups["hosts"].Value.Split(',')
                .Select(host => HostEndpoint.Parse(host.Trim()))
                .ToList();
        } else if (match.Groups["hosts"].Length == 0)
        {
            throw new ArgumentException("Hosts list is empty. At least one host is expected in the connection string.");
        }

        if (match.Groups["params"].Success)
        {
            connectionString.Parameters = match.Groups["params"].Value.Split('&')
                .Select(x =>
                {
                    var kvp = x.Split('=');
                    return new KeyValuePair<string, string>(kvp[0], kvp[1]);
                })
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        return connectionString;
    }

    public IEnumerable<HostEndpointWithPort> GetBootstrapEndpoints(bool? overrideTls = null)
    {
        var hosts = new List<HostEndpoint>(Hosts);
        if (RandomizeSeedHosts)
        {
            hosts = hosts.Shuffle();
        }

        foreach (var endpoint in hosts)
        {
            if (endpoint.Port != null)
            {
                yield return new HostEndpointWithPort(endpoint.Host, endpoint.Port.GetValueOrDefault());
            }
            else
            {
                yield return new HostEndpointWithPort(endpoint.Host,
                    overrideTls.GetValueOrDefault(Scheme == Scheme.Https) ? HttpsPort: HttpPort);
            }
        }
    }

    internal Uri GetDnsBootStrapUri()
    {
        return new UriBuilder
        {
            Scheme = Scheme.ToString(),
            Host = Hosts.First().Host
        }.Uri;
    }

    /// <summary>
    /// Identifies if this connection string is valid for use with DNS SRV lookup.
    /// </summary>
    /// <returns>True if valid for DNS SRV lookup.</returns>
    /// <seealso cref="GetDnsBootStrapUri"/>.
    public bool IsValidDnsSrv()
    {
        if(IsDnsSrv)
        {
            return true;
        }
        if (Scheme != Scheme.Http && Scheme != Scheme.Https)
        {
            return false;
        }

        if (Hosts.Count > 1)
        {
            return false;
        }

        return Hosts.Single().Port == null;
    }

    public bool TryGetParameter(string key, out object parameter)
    {
        if (Parameters.TryGetValue(key, out var value))
        {
            parameter = value;
            return true;
        }

        parameter = string.Empty;
        return false;
    }

    public bool TryGetParameter(string key, out string parameter)
    {
        if (Parameters.TryGetValue(key, out var value))
        {
            parameter = value;
            return true;
        }

        parameter = string.Empty;
        return false;
    }

    public bool TryGetParameter(string key, out int parameter)
    {
        if (TryGetParameter(key, out string value))
        {
            parameter =  Convert.ToInt32(value);
            return true;
        }

        parameter = default;
        return false;
    }

    public bool TryGetParameter(string key, out float parameter)
    {
        if (TryGetParameter(key, out string value))
        {
            parameter =  Convert.ToSingle(value);
            return true;
        }

        parameter = default;
        return false;
    }

    public bool TryGetParameter(string key, out TimeSpan parameter)
    {
        if (TryGetParameter(key, out string value))
        {
            parameter = TimeSpan.FromMilliseconds(Convert.ToUInt32(value));
            return true;
        }

        parameter = default;
        return false;
    }

    public bool TryGetParameter(string key, out bool parameter)
    {
        if (TryGetParameter(key, out string value))
        {
            if (value == "on")
            {
                parameter = true;
                return true;
            }

            if (value == "off")
            {
                parameter = false;
                return true;
            }

            parameter = Convert.ToBoolean(value);
            return true;
        }

        parameter = default;
        return false;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.Append(Scheme switch
        {
            Scheme.Http => "http://",
            Scheme.Https => "https://",
            _ => throw new ArgumentException($"Unknown scheme {Scheme}")
        });

        if (!string.IsNullOrEmpty(Username))
        {
            builder.Append(Uri.EscapeDataString(Username));
            builder.Append('@');
        }

        for (var hostIndex = 0; hostIndex < Hosts.Count; hostIndex++)
        {
            if (hostIndex > 0)
            {
                builder.Append(',');
            }

            builder.Append(Hosts[hostIndex].Host);
            if (Hosts[hostIndex].Port != null)
            {
                builder.AppendFormat(":{0}", Hosts[hostIndex].Port);
            }
        }

        if (Parameters.Count > 0)
        {
            var first = true;
            foreach (var parameter in Parameters)
            {
                if (first)
                {
                    first = false;
                    builder.Append('?');
                }
                else
                {
                    builder.Append('&');
                }

                builder.Append(Uri.EscapeDataString(parameter.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(parameter.Value));
            }
        }

        return builder.ToString();
    }
}

internal enum Scheme
{
    /// <summary>
    /// Non-TLS couchbase clusters.
    /// </summary>
    Http,
    
    /// <summary>
    /// For TLS/SSL clusters.
    /// </summary>
    Https
}

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
