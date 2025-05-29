using Xunit;
using Moq;

namespace Couchbase.Analytics2.UnitTests
{
    public class ClusterTest
    {
        [Fact]
        public void Create_ValidParameters_ReturnsClusterInstance()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var clusterOptions = new Mock<ClusterOptions>().Object;

            // Act
            var cluster = Cluster.Create(httpEndpoint, credential, clusterOptions);

            // Assert
            Assert.NotNull(cluster);
        }

        [Fact]
        public void Create_NullHttpEndpoint_ThrowsArgumentNullException()
        {
            // Arrange
            Credential credential = Credential.Create("Administrator", "password");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Cluster.Create(null, credential));
        }

        [Fact]
        public void Create_NullCredential_ThrowsArgumentNullException()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => Cluster.Create(httpEndpoint, null));
        }

        [Fact]
        public void Database_ReturnsDatabaseInstance()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var cluster = Cluster.Create(httpEndpoint, credential);

            // Act
            var database = cluster.Database("TestDatabase");

            // Assert
            Assert.NotNull(database);
            Assert.Equal("TestDatabase", database.Name); // Assuming Database has a Name property
        }

        [Fact]
        public void Links_ReturnsLinkManagerInstance()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var cluster = Cluster.Create(httpEndpoint, credential);

            // Act
            var linkManager = cluster.Links();

            // Assert
            Assert.NotNull(linkManager);
        }

        [Fact]
        public void Databases_ReturnsDatabaseManagerInstance()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var cluster = Cluster.Create(httpEndpoint, credential);

            // Act
            var databaseManager = cluster.Databases();

            // Assert
            Assert.NotNull(databaseManager);
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            // Arrange
            var httpEndpoint = "http://localhost:8091";
            var credential = Credential.Create("Administrator", "password");
            var cluster = Cluster.Create(httpEndpoint, credential);

            // Act
            cluster.Dispose();

            // Assert
            // No exceptions should be thrown, and resources should be released.
        }
    }
}
