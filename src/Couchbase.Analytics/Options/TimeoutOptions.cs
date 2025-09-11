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
 
namespace Couchbase.Analytics2;

public record TimeoutOptions
{
    internal TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
    internal TimeSpan DispatchTimeout = TimeSpan.FromSeconds(30);
    internal TimeSpan QueryTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Socket connection timeout, or more broadly the timeout
    /// for establishing an individual authenticated connection.
    /// <remarks>The default is 10s.</remarks>
    /// </summary>
    public TimeoutOptions WithConnectTimeout(TimeSpan connectTimeout )
    {
        ConnectTimeout = connectTimeout;
        return this;
    }

    /// <summary>
    /// How long the user is willing to wait for the SDK to retry
    /// a request due to network connectivity issues or unexpected
    /// cluster topology changes. Should be longer than the connect
    /// timeout, since recovery may involve multiple connection attempts.
    /// <remarks>The default is 30s.</remarks>
    /// </summary>
    public TimeoutOptions WithDispatchTimeout(TimeSpan dispatchTimeout )
    {
        DispatchTimeout = dispatchTimeout;
        return this;
    }

    /// <summary>
    /// Columnar query timeout.
    /// <remarks>The default is 10m.</remarks>
    /// </summary>
    public TimeoutOptions WithQueryTimeout(TimeSpan queryTimeout )
    {
        QueryTimeout = queryTimeout;
        return this;
    }
}