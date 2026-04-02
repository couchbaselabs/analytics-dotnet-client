using Xunit;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures;

[CollectionDefinition(Name)]
public class JwtCollection : ICollectionFixture<JwtFixture>
{
    public const string Name = "JwtCollection";
}
