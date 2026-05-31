using IUUT.Core.Io;

namespace IUUT.Core.Tests.TestDoubles;

/// <summary>
/// In-memory <see cref="ISafeSaveWriter"/> for apply-pipeline tests. Records the order of
/// writes, always runs the re-parse <c>validate</c> callback (so a serialization bug still
/// surfaces), and can be told to fail on the first file whose name contains a marker.
/// </summary>
internal sealed class FakeSafeSaveWriter : ISafeSaveWriter
{
    private readonly string? _failOnFileNameContaining;

    public FakeSafeSaveWriter(string? failOnFileNameContaining = null) =>
        _failOnFileNameContaining = failOnFileNameContaining;

    /// <summary>Paths written successfully, in order.</summary>
    public List<string> WrittenPaths { get; } = [];

    public Task<SafeSaveResult> WriteAsync(
        string filePath,
        string newContent,
        Action<string> validate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validate);

        // Exercise the post-write re-parse exactly as the real writer would.
        validate(newContent);

        var backupPath = filePath + ".iuut-backup-fake";
        if (_failOnFileNameContaining is not null &&
            filePath.Contains(_failOnFileNameContaining, StringComparison.Ordinal))
        {
            return Task.FromResult(SafeSaveResult.Failure(filePath, backupPath, new IOException("simulated write failure")));
        }

        WrittenPaths.Add(filePath);
        return Task.FromResult(SafeSaveResult.Success(filePath, backupPath));
    }
}
