namespace Couchbase.Analytics2.Internal.DI;

public static class ClusterOptionsExtensions
{
    /// <summary>
    /// Register a singleton service with the cluster's <see cref="ICluster.ClusterServices"/>.
    /// </summary>
    /// <typeparam name="T">The type of the service which will be requested and returned.</typeparam>
    /// <param name="clusterOptions">The <see cref="ClusterOptions"/>.</param>
    /// <param name="singleton">Singleton instance which is always returned.</param>
    /// <returns>The <see cref="ClusterOptions"/>.</returns>
    public static ClusterOptions AddClusterService<T>(
        this ClusterOptions clusterOptions,
        T singleton)
        where T : notnull =>
        clusterOptions.AddService<T, T>(_ => singleton, ClusterServiceLifetime.Cluster);
}