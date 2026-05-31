using System.Reflection;

namespace IUUT.Catalog;

/// <summary>
/// Accessor for the embedded D_* catalog JSON shipped in this assembly
/// (master doc §15). Resource names are matched by file-name suffix so the build's
/// resource-name mangling never matters to callers.
/// </summary>
public static class CatalogResources
{
    private static readonly Assembly _assembly = typeof(CatalogResources).Assembly;

    /// <summary>Opens the embedded catalog file (e.g. <c>talents.json</c>) as a stream.</summary>
    /// <exception cref="InvalidOperationException">No embedded resource matches <paramref name="fileName"/>.</exception>
    public static Stream Open(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var resourceName = Array.Find(
            _assembly.GetManifestResourceNames(),
            n => n.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase)
              || n.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded catalog '{fileName}' was not found.");

        return _assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded catalog '{resourceName}' could not be opened.");
    }

    /// <summary>The manifest names of every embedded resource (diagnostic).</summary>
    public static IReadOnlyList<string> ResourceNames() => _assembly.GetManifestResourceNames();
}
