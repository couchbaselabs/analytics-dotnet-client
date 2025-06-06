using Xunit;

namespace Couchbase.Analytics2.FunctionalTests.Fixtures;

[CollectionDefinition(Name)]
public class TestCollection : ICollectionFixture<Analytics2Fixture>
{
    public const string Name = "XUnitCollection";
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}