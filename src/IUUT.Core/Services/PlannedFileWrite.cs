namespace IUUT.Core.Services;

/// <summary>
/// One file a <see cref="LazyMaxPlan"/> intends to write: its name, absolute path, and the
/// already-serialized new content. Holding the content in the plan means apply just writes
/// (no re-serialization between preview and confirm).
/// </summary>
public sealed record PlannedFileWrite
{
    /// <summary>The file name (e.g. <c>Profile.json</c>).</summary>
    public required string FileName { get; init; }

    /// <summary>Absolute path to the file.</summary>
    public required string FilePath { get; init; }

    /// <summary>The serialized content to write.</summary>
    public required string NewContent { get; init; }
}
