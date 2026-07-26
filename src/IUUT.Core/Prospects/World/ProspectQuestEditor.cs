using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>What a quest reset changed.</summary>
public sealed record QuestResetResult(int StepsReset, int VariablesCleared, bool ManagerCleared)
{
    /// <summary>Whether anything changed at all.</summary>
    public bool Changed => StepsReset > 0 || VariablesCleared > 0 || ManagerCleared;
}

/// <summary>
/// GATED WRITE (Tier 3): resets a prospect's mission progress by zeroing every quest recorder's
/// <c>VariableRecords</c> (the <c>bVariable</c> tag byte, and the <c>iVariable</c>/<c>fVariable</c>
/// value bytes) and clearing the manager's <c>bMissionComplete</c>. Every write is IN-PLACE and
/// SIZE-PRESERVING — the blob's length never changes, no tag is rewritten, and nothing outside
/// those exact bytes is touched — which is what makes this write safe to gate on the same parse
/// offsets the read-only <see cref="ProspectQuestReader"/> uses. Structure/ownership of every
/// other recorder round-trips untouched (CONSTITUTION VI).
/// </summary>
public static class ProspectQuestEditor
{
    private const string ManagerClass = "/Script/Icarus.IcarusQuestManagerRecorderComponent";
    private const string QuestClass = "/Script/Icarus.IcarusQuestRecorderComponent";

    /// <summary>
    /// Resets the mission inside <paramref name="prospect"/>'s blob (decompress → in-place zeroing →
    /// recompress via <see cref="ProspectBlobCodec.SetUncompressed"/>). The model is only touched
    /// when something actually changed.
    /// </summary>
    public static QuestResetResult ResetMission(ProspectFileModel prospect)
    {
        ArgumentNullException.ThrowIfNull(prospect);

        var data = ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob);
        var result = Reset(data);
        if (result.Changed)
        {
            ProspectBlobCodec.SetUncompressed(prospect.ProspectBlob, data);
        }

        return result;
    }

    /// <summary>Zeroes the quest state in an already-decompressed blob, mutating it in place.</summary>
    public static QuestResetResult Reset(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var stepsReset = 0;
        var variablesCleared = 0;
        var managerCleared = false;

        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return new QuestResetResult(0, 0, false);
        }

        foreach (var element in recorders.Children)
        {
            foreach (var blobStruct in ElementBlobs(element))
            {
                var (componentClass, binary) = Identify(blobStruct, decompressed);
                if (binary is null)
                {
                    continue;
                }

                if (string.Equals(componentClass, ManagerClass, StringComparison.Ordinal))
                {
                    managerCleared |= ClearBool(decompressed, binary, "bMissionComplete");
                }
                else if (string.Equals(componentClass, QuestClass, StringComparison.Ordinal))
                {
                    var cleared = ResetStep(decompressed, binary);
                    if (cleared > 0)
                    {
                        stepsReset++;
                        variablesCleared += cleared;
                    }
                }
            }
        }

        return new QuestResetResult(stepsReset, variablesCleared, managerCleared);
    }

    private static int ResetStep(byte[] data, UeProperty binary)
    {
        var cleared = 0;
        var records = binary.Children.FirstOrDefault(c =>
            string.Equals(c.Name, "VariableRecords", StringComparison.Ordinal));
        if (records is null)
        {
            return 0;
        }

        foreach (var record in records.Children)
        {
            var changed = false;
            foreach (var field in record.Children)
            {
                switch (field.Type)
                {
                    case "BoolProperty" when string.Equals(field.Name, "bVariable", StringComparison.Ordinal):
                        changed |= ZeroTagBool(data, field);
                        break;
                    case "IntProperty" when string.Equals(field.Name, "iVariable", StringComparison.Ordinal):
                    case "FloatProperty" when string.Equals(field.Name, "fVariable", StringComparison.Ordinal):
                        changed |= ZeroValue(data, field);
                        break;
                }
            }

            if (changed)
            {
                cleared++;
            }
        }

        return cleared;
    }

    private static bool ClearBool(byte[] data, UeProperty parent, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal) &&
                string.Equals(child.Type, "BoolProperty", StringComparison.Ordinal))
            {
                return ZeroTagBool(data, child);
            }
        }

        return false;
    }

    // The bool value byte lives in the TAG at ValueOffset - 2 (see UePropertyReader.ReadBool).
    private static bool ZeroTagBool(byte[] data, UeProperty property)
    {
        var offset = property.ValueOffset - 2;
        if (data[offset] == 0)
        {
            return false;
        }

        data[offset] = 0;
        return true;
    }

    private static bool ZeroValue(byte[] data, UeProperty property)
    {
        var changed = false;
        for (var i = 0; i < property.ValueSize; i++)
        {
            if (data[property.ValueOffset + i] != 0)
            {
                data[property.ValueOffset + i] = 0;
                changed = true;
            }
        }

        return changed;
    }

    private static IEnumerable<UeProperty> ElementBlobs(UeProperty element) =>
        string.Equals(element.StructName, "StateRecorderBlob", StringComparison.Ordinal)
            ? [element]
            : element.Children.Where(c => string.Equals(c.StructName, "StateRecorderBlob", StringComparison.Ordinal));

    private static (string? ComponentClass, UeProperty? Binary) Identify(UeProperty blobStruct, byte[] data)
    {
        string? componentClass = null;
        UeProperty? binary = null;
        foreach (var child in blobStruct.Children)
        {
            if (string.Equals(child.Name, "ComponentClassName", StringComparison.Ordinal) &&
                child.Type is "StrProperty" or "NameProperty")
            {
                var pos = child.ValueOffset;
                componentClass = UePropertyReader.ReadFString(data, ref pos);
            }
            else if (string.Equals(child.Name, "BinaryData", StringComparison.Ordinal))
            {
                binary = child;
            }
        }

        return (componentClass, binary);
    }
}
