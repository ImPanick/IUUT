namespace IUUT.Core.Services;

/// <summary>The result of scanning a profile folder (master doc §11.3, §10.2 Home summary).</summary>
public sealed record HealthReport
{
    /// <summary>Per-file results.</summary>
    public required IReadOnlyList<FileHealth> Files { get; init; }

    /// <summary>Files that parsed cleanly.</summary>
    public int OkCount => Files.Count(f => f.Status == FileHealthStatus.Ok);

    /// <summary>Files with a genuine problem (excludes <see cref="FileHealthStatus.Missing"/>).</summary>
    public int IssueCount => Files.Count(f =>
        f.Status is FileHealthStatus.Unparseable
                 or FileHealthStatus.BlobHashMismatch
                 or FileHealthStatus.Unreadable);

    /// <summary>True when no file has a genuine problem (missing optional files do not count).</summary>
    public bool IsHealthy => IssueCount == 0;

    /// <summary>The problem files, for the recovery report.</summary>
    public IReadOnlyList<FileHealth> Issues =>
        Files.Where(f =>
            f.Status is FileHealthStatus.Unparseable
                     or FileHealthStatus.BlobHashMismatch
                     or FileHealthStatus.Unreadable).ToList();
}
