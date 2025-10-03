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

using Couchbase.Core.Json;

namespace Couchbase.AnalyticsClient.Results;

public sealed class AnalyticsRow
{
    private readonly IJsonToken _token;

    internal AnalyticsRow(IJsonToken token)
    {
        _token = token ?? throw new ArgumentNullException(nameof(token));
    }

    public T ContentAs<T>()
    {
        if (typeof(T) == typeof(byte[]))
        {
            var bytes = _token.ToUtf8Bytes();
            return (T)(object)bytes;
        }

        return _token.ToObject<T>();
    }
}