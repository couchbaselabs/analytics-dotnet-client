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
namespace Couchbase.Analytics2;

/// <summary>
/// The scan consistency setting specifies the consistency guarantee for index scanning.
/// </summary>
public enum QueryScanConsistency
{
    /// <summary>
    /// The index scan does not use a timestamp vector. This is the fastest mode, because it avoids the costs of obtaining the vector and waiting for the index to catch up to the vector.
    /// </summary>
    NotBounded,

    /// <summary>
    /// This option implements bounded consistency. You can use this setting to implement read-your-own-writes (RYOW).
    /// </summary>
    RequestPlus
}