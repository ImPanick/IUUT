using System.Text.RegularExpressions;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>One mount found deployed inside a prospect world blob.</summary>
public sealed record ProspectMount(string Name, string MountType)
{
    /// <summary>A best label: the custom name, else the type, else a generic "Mount".</summary>
    public string Label =>
        !string.IsNullOrWhiteSpace(Name) ? Name
        : !string.IsNullOrWhiteSpace(MountType) ? MountType
        : "Mount";
}

/// <summary>The mounts deployed in one prospect (its <c>Prospects\&lt;name&gt;.json</c> world save).</summary>
public sealed record ProspectMountGroup(string ProspectName, IReadOnlyList<ProspectMount> Mounts);

/// <summary>
/// Read-only reader that finds the mounts deployed inside a prospect world blob. Each mount is an
/// actor in <c>StateRecorderBlobs</c> whose recorder is the
/// <c>IcarusMountCharacterRecorderComponent</c>; its custom <c>MountName</c> and (best-effort) type
/// (from the <c>BP_Mount_&lt;X&gt;_C</c> actor class) are pulled from the UE property tree.
/// <para>
/// This is why the flat Mounts editor only ever showed one cohort (issue #19): mounts deployed in an
/// active prospect live in that prospect's world save, NOT in the profile <c>Mounts.json</c> roster.
/// </para>
/// </summary>
public sealed partial class ProspectMountReader
{
    /// <summary>Decompresses a prospect blob and lists its mounts.</summary>
    public IReadOnlyList<ProspectMount> ReadBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Read(ProspectBlobVerifier.Decompress(blob.BinaryBlob));
    }

    /// <summary>Lists the mounts in an already-decompressed prospect world blob.</summary>
    public IReadOnlyList<ProspectMount> Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return Array.Empty<ProspectMount>();
        }

        var mounts = new List<ProspectMount>();
        foreach (var actor in recorders.Children)
        {
            string? componentClass = null;
            string? mountName = null;
            var mountType = "";
            CollectActor(actor, decompressed, ref componentClass, ref mountName, ref mountType);

            if (SlotOwner.Classify(componentClass) == SlotOwnerKind.Mount)
            {
                mounts.Add(new ProspectMount(mountName ?? "", mountType));
            }
        }

        return mounts;
    }

    // One pass over an actor's subtree: capture its ComponentClassName, its MountName, and a
    // BP_Mount_<X>_C type from any string-valued property (the actor class), whichever appears.
    private static void CollectActor(
        UeProperty node, byte[] data, ref string? componentClass, ref string? mountName, ref string mountType)
    {
        if (string.Equals(node.Type, "StrProperty", StringComparison.Ordinal) ||
            string.Equals(node.Type, "NameProperty", StringComparison.Ordinal))
        {
            var pos = node.ValueOffset;
            var value = UePropertyReader.ReadFString(data, ref pos);
            if (!string.IsNullOrEmpty(value))
            {
                if (componentClass is null && string.Equals(node.Name, "ComponentClassName", StringComparison.Ordinal))
                {
                    componentClass = value;
                }
                else if (mountName is null && string.Equals(node.Name, "MountName", StringComparison.Ordinal))
                {
                    mountName = value;
                }

                if (mountType.Length == 0)
                {
                    var match = MountClassRegex().Match(value);
                    if (match.Success)
                    {
                        mountType = match.Groups[1].Value;
                    }
                }
            }
        }

        foreach (var child in node.Children)
        {
            CollectActor(child, data, ref componentClass, ref mountName, ref mountType);
        }
    }

    [GeneratedRegex("BP_Mount_([A-Za-z]+)_C")]
    private static partial Regex MountClassRegex();
}
