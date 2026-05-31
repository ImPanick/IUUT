namespace IUUT.Core.Io;

/// <summary>
/// The save-file write contract (CONSTITUTION III; CODE_STYLE §10): no save-file mutator
/// writes to disk except through this. The implementation backs up, writes to a temp
/// sibling, re-reads + validates, and only then atomically renames over the original.
/// Abstracted so apply pipelines can be unit-tested against a fake writer.
/// </summary>
public interface ISafeSaveWriter
{
    /// <summary>
    /// Safely writes <paramref name="newContent"/> to <paramref name="filePath"/>: backs up
    /// an existing file, writes a temp sibling (UTF-8 without BOM), re-reads it and runs
    /// <paramref name="validate"/> over it, then atomically renames the temp over the original.
    /// On any failure the original is left intact and the result reports the error.
    /// </summary>
    /// <param name="filePath">Absolute path to the save file.</param>
    /// <param name="newContent">The new file content.</param>
    /// <param name="validate">Re-parse/validation callback over the temp content; throw to reject it.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SafeSaveResult> WriteAsync(
        string filePath,
        string newContent,
        Action<string> validate,
        CancellationToken cancellationToken = default);
}
