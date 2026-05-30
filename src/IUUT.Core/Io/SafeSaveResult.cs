namespace IUUT.Core.Io;

/// <summary>
/// Outcome of a <see cref="SafeSaveWriter"/> write. On failure the original file has
/// already been restored from <see cref="BackupPath"/> (CONSTITUTION III); inspect
/// <see cref="Error"/> for the cause.
/// </summary>
public sealed record SafeSaveResult
{
    /// <summary>Whether the write succeeded and the re-read content validated.</summary>
    public required bool Ok { get; init; }

    /// <summary>The file that was written.</summary>
    public required string FilePath { get; init; }

    /// <summary>The backup created before writing, or <c>null</c> if the file was new.</summary>
    public string? BackupPath { get; init; }

    /// <summary>The failure cause when <see cref="Ok"/> is <c>false</c>; otherwise <c>null</c>.</summary>
    public Exception? Error { get; init; }

    /// <summary>Creates a success result.</summary>
    public static SafeSaveResult Success(string filePath, string? backupPath) =>
        new() { Ok = true, FilePath = filePath, BackupPath = backupPath };

    /// <summary>Creates a failure result (the original has already been restored).</summary>
    public static SafeSaveResult Failure(string filePath, string? backupPath, Exception error) =>
        new() { Ok = false, FilePath = filePath, BackupPath = backupPath, Error = error };
}
