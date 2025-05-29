namespace Couchbase.Analytics2.Internal;

internal class AnalyticsServiceCollection
{
    private readonly List<IAnalyticsService> _services = new();
    private int _currentIndex;

    public IAnalyticsService Next()
    {
        throw new NotImplementedException();
    }
}
