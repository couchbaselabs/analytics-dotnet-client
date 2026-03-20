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

namespace Couchbase.AnalyticsClient.Query;

/// <summary>
/// Represents a query context that combines a database and scope into a formatted string
/// suitable for Analytics queries.
/// </summary>
public sealed class QueryContext
{
    private readonly string _formatted;

    /// <summary>
    /// Initializes a new instance of the QueryContext class.
    /// </summary>
    /// <param name="databaseName">The database name. Must not be null or contain backticks.</param>
    /// <param name="scopeName">The scope name. Must not be null or contain backticks.</param>
    /// <exception cref="ArgumentNullException">Thrown when database or scope is null.</exception>
    /// <exception cref="ArgumentException">Thrown when database or scope contains backtick characters.</exception>
    public QueryContext(string databaseName, string scopeName)
    {
        if (string.IsNullOrEmpty(databaseName)) throw new ArgumentException("databaseName cannot be null or empty", nameof(databaseName));
        if (string.IsNullOrEmpty(scopeName)) throw new ArgumentException("scopeName cannot be null or empty.", nameof(scopeName));

        Database = databaseName;
        Scope = scopeName;

        if (databaseName.Contains('`'))
        {
            throw new ArgumentException($"Database name must not contain backtick (`), but got: {databaseName}", nameof(databaseName));
        }

        if (scopeName.Contains('`'))
        {
            throw new ArgumentException($"Scope name must not contain backtick (`), but got: {scopeName}", nameof(scopeName));
        }

        _formatted = $"default:`{databaseName}`.`{scopeName}`";
    }

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string Database { get; }

    /// <summary>
    /// Gets the scope name.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Returns the formatted query context string.
    /// </summary>
    /// <returns>The formatted query context.</returns>
    public override string ToString() => _formatted;
}
