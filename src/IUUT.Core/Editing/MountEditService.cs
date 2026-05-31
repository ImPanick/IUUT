using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode mount edits (master doc §8.10): the denormalized JSON fields (name, level). The
/// authoritative <c>RecorderBlob</c> binary is never touched here. Pure in-memory mutation.
/// </summary>
public sealed class MountEditService
{
    /// <summary>Renames a mount (name must be non-empty).</summary>
    public void SetName(Mount mount, string name)
    {
        ArgumentNullException.ThrowIfNull(mount);
        ArgumentException.ThrowIfNullOrEmpty(name);
        mount.MountName = name;
    }

    /// <summary>Sets a mount's denormalized level.</summary>
    public void SetLevel(Mount mount, int level)
    {
        ArgumentNullException.ThrowIfNull(mount);
        mount.MountLevel = level;
    }
}
