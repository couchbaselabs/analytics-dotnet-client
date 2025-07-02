namespace Couchbase.Analytics2.Internal.DnsUtil;

internal enum EndpointSelectionMode
{
    // Round-robin but starts at a random index instead of 0
    RoundRobinRandomStart,
    // Round-robin strategy that always starts at 0
    RoundRobin,
    // Always randomly selects an endpoint
    Random,
    // Randomly selects an endpoint, but tracks which were attempted and only picks from unused endpoints
    RandomFromUnusedEndpoints,
}