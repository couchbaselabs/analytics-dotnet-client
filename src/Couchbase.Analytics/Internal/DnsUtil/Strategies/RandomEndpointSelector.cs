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
using System.Linq;
using Couchbase.Analytics2.Exceptions;

namespace Couchbase.Analytics2.Internal.DnsUtil.Strategies;

internal class RandomEndpointSelector : IEndpointSelectionStrategy
{
    private IPAddress[]? _previousAddresses;
    private int[]? _unusedIndexes;
    private bool _pickFromUnused;

    /// <summary>
    /// Creates  a new instance of <see cref="RandomEndpointSelector"/>.
    /// </summary>
    /// <param name="pickFromUnused">Determines whether this selector should keep track of IP addresses which were already used
    /// so not to select the same IP multiple times (true) or pick a random one each time.
    /// </param>
    public RandomEndpointSelector(bool pickFromUnused = false)
    {
        _pickFromUnused = pickFromUnused;
    }
    public int SelectEndpointIndex(IPAddress[] addresses)
    {
        if (!_pickFromUnused) return addresses.Length == 1 ? 0 : Random.Shared.Next(addresses.Length);

        // If this is the first request, save the addresses and initialize the unused indexes.
        if (_pickFromUnused && (_previousAddresses is null && _unusedIndexes is null))
        {
            _previousAddresses = addresses;
            ResetUnusedIndexes(ref _unusedIndexes, addresses.Length);
        }

        // If the DNS record was refreshed and the list of addresses has changed,
        // reset the previous addresses and unused indexes.
        else if (_previousAddresses is not null && addresses != _previousAddresses)
        {
            _previousAddresses = addresses;
            ResetUnusedIndexes(ref _unusedIndexes, addresses.Length);
        }

        if (_unusedIndexes!.Length == 0)
        {
            // Since we reuse the same handler for all clients, if there are no unused indexes left, reset it.
            ResetUnusedIndexes(ref _unusedIndexes, addresses.Length);
        }
        // Pick a random index from the remaining unused indexes,
        // and remove it from the list of unused indexes.
        var nextIndex = _unusedIndexes![Random.Shared.Next(_unusedIndexes.Length)];
        _unusedIndexes = _unusedIndexes.Where(i => i != nextIndex).ToArray();
        return nextIndex;
    }

    private static void ResetUnusedIndexes(ref int[]? unusedIndexes, int range)
    {
        unusedIndexes = Enumerable.Range(0, range).ToArray();
    }

}