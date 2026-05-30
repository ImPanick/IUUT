namespace IUUT.Core.Tests.TestDoubles;

/// <summary>
/// A unique temporary directory that deletes itself on <see cref="Dispose"/>.
/// Tests that need real on-disk files use this instead of touching
/// <c>%LOCALAPPDATA%</c> (TESTING_CONTRACT §5).
/// </summary>
public sealed class TempDir : IDisposable
{
    /// <summary>Creates and roots a fresh unique temp directory.</summary>
    public TempDir()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "iuut-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>The directory's absolute path.</summary>
    public string Path { get; }

    /// <summary>Resolves <paramref name="name"/> to a path inside this directory.</summary>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; a locked temp file must not fail the test run.
        }
    }
}
