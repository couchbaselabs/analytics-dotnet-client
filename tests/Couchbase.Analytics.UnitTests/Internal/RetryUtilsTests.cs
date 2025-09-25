using Couchbase.AnalyticsClient.Internal.Retry;
using Xunit;
using Xunit.Abstractions;

namespace Couchbase.AnalyticsClient.UnitTests.Internal;

public class RetryUtilsTests
{
    private readonly ITestOutputHelper _outputHelper;

    public RetryUtilsTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Theory]
    [InlineData(0, 75, 125)]     // Attempt 0: 100ms base ±25% = 75-125ms
    [InlineData(1, 150, 250)]    // Attempt 1: 200ms ±25% = 150-250ms
    [InlineData(2, 300, 500)]    // Attempt 2: 400ms ±25% = 300-500ms
    [InlineData(3, 600, 1000)]   // Attempt 3: 800ms ±25% = 600-1000ms
    [InlineData(4, 1200, 2000)]  // Attempt 4: 1600ms ±25% = 1200-2000ms
    [InlineData(5, 2400, 4000)]  // Attempt 5: 3200ms ±25% = 2400-4000ms
    [InlineData(6, 4800, 8000)]  // Attempt 6: 6400ms ±25% = 4800-8000ms
    [InlineData(7, 9600, 16000)] // Attempt 7: 12800ms ±25% = 9600-16000ms
    [InlineData(8, 19200, 32000)] // Attempt 8: 25600ms ±25% = 19200-32000ms
    [InlineData(9, 38400, 75000)] // Attempt 9: 51200ms capped at 60000ms ±25% = 38400-75000ms
    [InlineData(10, 45000, 75000)] // Attempt 10: 102400ms capped at 60000ms ±25% = 45000-75000ms
    public void CalculateBackoffDelay_ReturnsExpectedRange(int attemptNumber, double expectedMinMs, double expectedMaxMs)
    {
        var delays = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            var delay = RetryUtils.CalculateBackoffDelay(attemptNumber);
            delays.Add(delay.TotalMilliseconds);
        }

        var minDelay = delays.Min();
        var maxDelay = delays.Max();
        var avgDelay = delays.Average();

        // Log the results
        _outputHelper.WriteLine($"Attempt {attemptNumber}:");
        _outputHelper.WriteLine($"  Expected Range: {expectedMinMs:F0}ms - {expectedMaxMs:F0}ms");
        _outputHelper.WriteLine($"  Actual Range:   {minDelay:F0}ms - {maxDelay:F0}ms");
        _outputHelper.WriteLine($"  Average:        {avgDelay:F0}ms");
        _outputHelper.WriteLine($"  Sample delays:  [{string.Join(", ", delays.Take(10).Select(d => $"{d:F0}ms"))}]");
        _outputHelper.WriteLine("");

        // Assert - Check that the actual range is within expected bounds
        // Allow for some tolerance due to jitter randomness
        Assert.True(minDelay >= expectedMinMs * 0.9,
            $"Minimum delay {minDelay:F0}ms should be >= {expectedMinMs * 0.9:F0}ms for attempt {attemptNumber}");
        Assert.True(maxDelay <= expectedMaxMs * 1.1,
            $"Maximum delay {maxDelay:F0}ms should be <= {expectedMaxMs * 1.1:F0}ms for attempt {attemptNumber}");

        // Verify that we actually get some variation
        var delayRange = maxDelay - minDelay;
        Assert.True(delayRange > 0, $"Should have some variation in delays due to jitter for attempt {attemptNumber}");
    }

    [Fact]
    public void CalculateBackoffDelay_ExponentialGrowth_FollowsExpectedPattern()
    {
        _outputHelper.WriteLine("Exponential Backoff Progression:");
        _outputHelper.WriteLine("Attempt | Base Delay | Expected Range (±25% jitter) | Actual Sample");
        _outputHelper.WriteLine("--------|------------|-------------------------------|---------------");

        for (int attempt = 0; attempt <= 10; attempt++)
        {
            // Calculate the theoretical base delay (before jitter)
            const uint baseDelayMs = 100;
            const double exponentialFactor = 2.0;
            const uint maxDelayMs = 60_000;

            var theoreticalDelay = Math.Min(baseDelayMs * Math.Pow(exponentialFactor, attempt), maxDelayMs);
            var minExpected = theoreticalDelay * 0.75; // -25% jitter
            var maxExpected = theoreticalDelay * 1.25; // +25% jitter

            // Get actual delay
            var actualDelay = RetryUtils.CalculateBackoffDelay(attempt);

            _outputHelper.WriteLine($"{attempt,7} | {theoreticalDelay,10:F0}ms | {minExpected,8:F0}ms - {maxExpected,8:F0}ms | {actualDelay.TotalMilliseconds,10:F0}ms");

            // Verify the actual delay is within the expected jitter range
            Assert.True(actualDelay.TotalMilliseconds >= minExpected * 0.95,
                $"Delay for attempt {attempt} should be >= {minExpected * 0.95:F0}ms");
            Assert.True(actualDelay.TotalMilliseconds <= maxExpected * 1.05,
                $"Delay for attempt {attempt} should be <= {maxExpected * 1.05:F0}ms");
        }
    }

    [Fact]
    public void CalculateBackoffDelay_MaxDelayIsCapped()
    {
        // Test that very high attempt numbers are capped at 60 seconds
        var delay = RetryUtils.CalculateBackoffDelay(20);

        _outputHelper.WriteLine($"Delay for attempt 20: {delay.TotalMilliseconds:F0}ms");

        // Should be capped at 60 seconds (60,000ms) plus jitter, so max would be 75,000ms
        Assert.True(delay.TotalMilliseconds <= 75_000,
            $"Delay should be capped at max 75,000ms (60s + 25% jitter), got {delay.TotalMilliseconds:F0}ms");
    }

    [Fact]
    public void CalculateBackoffDelay_MinDelayIsNonNegative()
    {
        // Test that delays are never negative (even with jitter)
        for (int attempt = 0; attempt <= 5; attempt++)
        {
            var delay = RetryUtils.CalculateBackoffDelay(attempt);

            _outputHelper.WriteLine($"Attempt {attempt}: {delay.TotalMilliseconds:F0}ms");

            Assert.True(delay.TotalMilliseconds >= 0,
                $"Delay for attempt {attempt} should be non-negative, got {delay.TotalMilliseconds:F0}ms");
        }
    }
}