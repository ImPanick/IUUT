using System.Text.Json;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode mount edits (master doc §8.10): the denormalized JSON fields (name, level) plus
/// roster restore/clone (mount-rescue slice 1). The authoritative <c>RecorderBlob</c> binary is
/// never decoded or modified here — a clone carries it verbatim. Pure in-memory mutation.
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

    /// <summary>
    /// Restores/clones a roster mount (mount-rescue slice 1, "Mount Reviver"): a deep copy of
    /// <paramref name="source"/> — RecorderBlob and all unknown members carried byte-for-byte —
    /// renamed to <paramref name="newName"/> and appended to <paramref name="model"/>'s roster.
    /// Returns the new mount.
    /// </summary>
    public Mount Clone(MountsModel model, Mount source, string newName)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(newName);

        // Serialize round-trip = a faithful deep copy including AdditionalData (CONSTITUTION VI).
        var clone = JsonSerializer.Deserialize<Mount>(JsonSerializer.SerializeToUtf8Bytes(source))
            ?? throw new InvalidOperationException("Mount clone round-trip produced null.");
        clone.MountName = newName;
        model.SavedMounts.Add(clone);
        return clone;
    }
}
