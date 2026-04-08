using Xunit;

namespace Couchbase.AnalyticsClient.FunctionalTests.Fixtures;

[CollectionDefinition(Name)]
public class CertificateCollection : ICollectionFixture<CertificateFixture>
{
    public const string Name = "Certificate";
}
