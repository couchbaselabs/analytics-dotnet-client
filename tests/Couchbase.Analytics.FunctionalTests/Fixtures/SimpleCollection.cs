using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures
{
    [CollectionDefinition(Name)]
    public class SimpleCollection : ICollectionFixture<SimpleFixture>
    {
        public const string Name = "SimpleCollection";
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
