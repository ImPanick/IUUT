using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode edits to the binary engine unlock flags (<c>flags_&lt;SteamID&gt;.dat</c>, master
/// doc §8.11): add or remove a flag ID. Pure in-memory mutation of a <see cref="FlagsFileModel"/>;
/// the binary write goes through <c>FlagsFileCodec</c>.
/// </summary>
public sealed class FlagsEditService
{
    /// <summary>Adds a flag id if absent; returns whether it was added.</summary>
    public bool AddFlag(FlagsFileModel flags, uint flagId)
    {
        ArgumentNullException.ThrowIfNull(flags);
        if (flags.Flags.Contains(flagId))
        {
            return false;
        }

        flags.Flags.Add(flagId);
        return true;
    }

    /// <summary>Removes a flag id; returns whether it was present.</summary>
    public bool RemoveFlag(FlagsFileModel flags, uint flagId)
    {
        ArgumentNullException.ThrowIfNull(flags);
        return flags.Flags.Remove(flagId);
    }
}
