namespace IUUT.Core.Prospects.World;

/// <summary>
/// Renames mounts deployed inside a prospect world blob (Tier 3, the first length-changing
/// string write beyond item retyping). The <c>MountName</c> <c>StrProperty</c> leaf gets a
/// replacement value and the reconstructing serializer fixes every ancestor length — the same
/// proven path <see cref="ProspectWorldEditor.RetypeSlot"/> uses. Only leaves owned by the
/// mount recorder component are candidates; everything else round-trips untouched.
/// </summary>
public sealed class ProspectMountEditor
{
    private const string MountRecorderClass = "/Script/Icarus.IcarusMountCharacterRecorderComponent";
    private const string MountNameProperty = "MountName";
    private const string ComponentClassProperty = "ComponentClassName";

    private readonly UeBlob _blob;

    /// <summary>Creates the editor over a parsed world blob.</summary>
    public ProspectMountEditor(UeBlob blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        _blob = blob;
    }

    /// <summary>One deployed mount's name leaf (document order matches <see cref="ProspectMountReader"/>).</summary>
    public sealed record MountRef(UeNode NameLeaf, string Name);

    /// <summary>The deployed mounts, in document order.</summary>
    public IReadOnlyList<MountRef> FindMounts()
    {
        var mounts = new List<MountRef>();
        foreach (var root in _blob.Roots)
        {
            Collect(root, mounts);
        }

        return mounts;
    }

    /// <summary>Renames a mount in place (staged on the node; <see cref="Serialize"/> commits).</summary>
    public void Rename(MountRef mount, string newName)
    {
        ArgumentNullException.ThrowIfNull(mount);
        ArgumentException.ThrowIfNullOrEmpty(newName);
        mount.NameLeaf.ReplacementValue = ProspectWorldEditor.EncodeFString(newName);
        mount.NameLeaf.MarkDirty();
    }

    /// <summary>Serializes the edited world (feed to <c>ProspectBlobCodec.SetUncompressed</c>).</summary>
    public byte[] Serialize() => _blob.Serialize(false);

    private void Collect(UeNode node, List<MountRef> mounts)
    {
        if (string.Equals(node.Name, MountNameProperty, StringComparison.Ordinal) &&
            string.Equals(node.Type, "StrProperty", StringComparison.Ordinal) &&
            IsMountOwned(node))
        {
            var pos = node.ValueStart;
            var name = UePropertyReader.ReadFString(_blob.Data, ref pos) ?? "";
            mounts.Add(new MountRef(node, name));
        }

        foreach (var child in node.Children)
        {
            Collect(child, mounts);
        }
    }

    // Walk up to the owning recorder record and require the mount component class — a
    // "MountName" string anywhere else is never a rename candidate.
    private bool IsMountOwned(UeNode leaf)
    {
        for (var n = leaf.Parent; n is not null; n = n.Parent)
        {
            var componentClass = n.Children.FirstOrDefault(c =>
                string.Equals(c.Name, ComponentClassProperty, StringComparison.Ordinal) &&
                string.Equals(c.Type, "StrProperty", StringComparison.Ordinal));
            if (componentClass is not null)
            {
                var pos = componentClass.ValueStart;
                return string.Equals(
                    UePropertyReader.ReadFString(_blob.Data, ref pos), MountRecorderClass, StringComparison.Ordinal);
            }
        }

        return false;
    }
}
