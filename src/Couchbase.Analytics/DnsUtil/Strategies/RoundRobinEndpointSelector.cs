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

using System.Net;

namespace Couchbase.AnalyticsClient.DnsUtil.Strategies;

internal class RoundRobinEndpointSelector : IEndpointSelectionStrategy
{
    private uint _index;

    /// <summary>
    /// Creates a new instance of <see cref="RoundRobinEndpointSelector"/>.
    /// </summary>
    /// <param name="startAtRandom">Determines whether the initial index should be 0 (false) or random (true)</param>
    public RoundRobinEndpointSelector(bool startAtRandom = false)
    {
        // If startAtRandom is true, we initialize _index to a random value.
        // Otherwise, we start at uint.MaxValue (which will wrap at the next increment).
        _index = startAtRandom ? (uint)Random.Shared.Next(int.MaxValue) : uint.MaxValue;
    }

    public int SelectEndpointIndex(IPAddress[] addresses)
    {
        if (addresses.Length == 1) return 0;

        var nextIndex = Interlocked.Increment(ref _index);
        Interlocked.Exchange(ref _index, (uint)(nextIndex % addresses.Length));
        return (int)_index;
    }
}