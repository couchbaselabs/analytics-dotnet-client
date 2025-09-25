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

namespace Couchbase.AnalyticsClient.Internal.Retry;

/// <summary>
/// Static utility methods for retry logic.
/// </summary>
internal static class RetryUtils
{
    private const uint BaseDelayMs = 100;
    private const double ExponentialFactor = 2.0;
    private const uint MaxDelayMs = 60_000;
    private const double JitterPercent = 0.25;

    /// <summary>
    /// Calculates the backoff delay for a retry attempt using exponential backoff with jitter.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (0-based).</param>
    /// <returns>The calculated delay as a TimeSpan.</returns>
    public static TimeSpan CalculateBackoffDelay(int attemptNumber)
    {
        // baseDelay * (factor ^ attemptNumber)
        var exponentialDelay = BaseDelayMs * Math.Pow(ExponentialFactor, attemptNumber);

        // Cap at maximum delay
        var cappedDelay = Math.Min(exponentialDelay, MaxDelayMs);

        var jitterRange = cappedDelay * JitterPercent;
        var jitterOffset = (Random.Shared.NextDouble() - 0.5) * 2 * jitterRange; // -jitterRange to +jitterRange
        var finalDelay = cappedDelay + jitterOffset;

        // Ensure we don't go below zero
        finalDelay = Math.Max(finalDelay, 0);

        return TimeSpan.FromMilliseconds(finalDelay);
    }

    public static async Task BackoffAsync(int attempt, CancellationToken token)
    {
        await Task.Delay(CalculateBackoffDelay(attempt), token).ConfigureAwait(false);
    }
}