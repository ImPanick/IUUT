namespace IUUT.App.ViewModels;

/// <summary>A save file reference for a picker: its full <see cref="Path"/> and display <see cref="Name"/>.</summary>
public sealed class NamedFileViewModel
{
    /// <summary>Creates a file reference.</summary>
    public NamedFileViewModel(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        Path = path;
        Name = System.IO.Path.GetFileName(path);
    }

    /// <summary>The full path on disk.</summary>
    public string Path { get; }

    /// <summary>The file name (display).</summary>
    public string Name { get; }
}
