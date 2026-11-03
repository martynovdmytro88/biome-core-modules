namespace Biome.LocalStore.Abstract
{
    /// <summary>
    /// Outcome of a read. The three failures are distinct events and are never collapsed into "first launch":
    /// <see cref="Missing"/> = no record; <see cref="Corrupt"/> = the record exists but is not a readable envelope
    /// (any parse failure, missing envelope field, or payload that does not fit the requested type — one strict
    /// policy); <see cref="VersionMismatch"/> = the record's version differs from the registered one.
    /// </summary>
    public enum ReadStatus
    {
        Ok,
        Missing,
        Corrupt,
        VersionMismatch,
    }
}
