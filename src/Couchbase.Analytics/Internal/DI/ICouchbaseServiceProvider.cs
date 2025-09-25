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

namespace Couchbase.AnalyticsClient.Internal.DI;

/// <summary>
/// Extends <see cref="IServiceProvider"/> with a method to test for service registration.
/// </summary>
internal interface ICouchbaseServiceProvider : IServiceProvider
{
    /// <summary>
    /// Determines if the specified service type is available from the <see cref="ICouchbaseServiceProvider"/>.
    /// </summary>
    /// <param name="serviceType">An object that specifies the type of service object to test.</param>
    /// <returns>true if the specified service is available, false if it is not.</returns>
    bool IsService(Type serviceType);
}