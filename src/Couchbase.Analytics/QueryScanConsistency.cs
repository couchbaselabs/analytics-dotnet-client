namespace Couchbase.Analytics2;

/// <summary>
/// The scan consistency setting specifies the consistency guarantee for index scanning.
/// </summary>
public enum QueryScanConsistency
{
    /// <summary>
    /// The index scan does not use a timestamp vector. This is the fastest mode, because it avoids the costs of obtaining the vector and waiting for the index to catch up to the vector.
    /// </summary>
    NotBounded,

    /// <summary>
    /// This option implements bounded consistency. You can use this setting to implement read-your-own-writes (RYOW).
    /// </summary>
    RequestPlus
}
