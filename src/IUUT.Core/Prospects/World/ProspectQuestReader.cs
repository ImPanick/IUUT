using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>One quest variable inside a quest recorder (e.g. "QuestComplete", "Count").</summary>
public sealed record ProspectQuestVariable(string Name, bool BoolValue, int IntValue);

/// <summary>One quest/step actor's state inside a prospect world blob.</summary>
public sealed record ProspectQuestStep(string QuestName, bool IsComplete, IReadOnlyList<ProspectQuestVariable> Variables);

/// <summary>The quest state of one prospect: the faction mission + its recorded quest steps.</summary>
public sealed record ProspectQuestState(string MissionName, bool MissionComplete, IReadOnlyList<ProspectQuestStep> Steps)
{
    /// <summary>Whether the prospect runs a faction mission at all (open-world prospects do not).</summary>
    public bool HasMission => MissionName.Length > 0 && !string.Equals(MissionName, "None", StringComparison.Ordinal);
}

/// <summary>
/// READ-ONLY decoder for the quest state inside a prospect world blob (Tier 3 research track,
/// roadmap rule: read-only report first, writes only after round-trip gates). Two recorder
/// classes carry it: <c>IcarusQuestManagerRecorderComponent</c> (one per prospect —
/// <c>FactionMissionName</c> + <c>bMissionComplete</c>) and <c>IcarusQuestRecorderComponent</c>
/// (one per quest/step actor — <c>QuestName</c> + <c>VariableRecords</c>, whose
/// <c>QuestComplete</c> variable marks a finished step). Nothing here mutates the blob.
/// </summary>
public sealed class ProspectQuestReader
{
    private const string ManagerClass = "/Script/Icarus.IcarusQuestManagerRecorderComponent";
    private const string QuestClass = "/Script/Icarus.IcarusQuestRecorderComponent";

    /// <summary>Decompresses a prospect blob and reads its quest state.</summary>
    public ProspectQuestState ReadBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Read(ProspectBlobVerifier.Decompress(blob.BinaryBlob));
    }

    /// <summary>Reads the quest state from an already-decompressed prospect world blob.</summary>
    public ProspectQuestState Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var mission = "";
        var missionComplete = false;
        var steps = new List<ProspectQuestStep>();

        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return new ProspectQuestState(mission, missionComplete, steps);
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
                    mission = FindString(binary, decompressed, "FactionMissionName") ?? mission;
                    missionComplete = FindBool(binary, decompressed, "bMissionComplete") ?? missionComplete;
                }
                else if (string.Equals(componentClass, QuestClass, StringComparison.Ordinal))
                {
                    steps.Add(ReadStep(binary, decompressed));
                }
            }
        }

        return new ProspectQuestState(mission, missionComplete, steps
            .OrderBy(s => s.QuestName, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    // The actor element itself may BE the StateRecorderBlob struct, or contain them as children.
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

    private static ProspectQuestStep ReadStep(UeProperty binary, byte[] data)
    {
        var questName = FindString(binary, data, "QuestName") ?? "";
        var variables = new List<ProspectQuestVariable>();

        var records = binary.Children.FirstOrDefault(c =>
            string.Equals(c.Name, "VariableRecords", StringComparison.Ordinal));
        if (records is not null)
        {
            foreach (var record in records.Children)
            {
                string? name = null;
                var boolValue = false;
                var intValue = 0;
                foreach (var field in record.Children)
                {
                    if (string.Equals(field.Name, "VariableName", StringComparison.Ordinal) &&
                        field.Type is "StrProperty" or "NameProperty")
                    {
                        var pos = field.ValueOffset;
                        name = UePropertyReader.ReadFString(data, ref pos);
                    }
                    else if (string.Equals(field.Name, "bVariable", StringComparison.Ordinal) &&
                             string.Equals(field.Type, "BoolProperty", StringComparison.Ordinal))
                    {
                        boolValue = UePropertyReader.ReadBool(data, field);
                    }
                    else if (string.Equals(field.Name, "iVariable", StringComparison.Ordinal) &&
                             string.Equals(field.Type, "IntProperty", StringComparison.Ordinal))
                    {
                        intValue = BitConverter.ToInt32(data, field.ValueOffset);
                    }
                }

                if (!string.IsNullOrEmpty(name))
                {
                    variables.Add(new ProspectQuestVariable(name, boolValue, intValue));
                }
            }
        }

        var complete = variables.FirstOrDefault(v =>
            string.Equals(v.Name, "QuestComplete", StringComparison.Ordinal))?.BoolValue ?? false;
        return new ProspectQuestStep(questName, complete, variables);
    }

    private static string? FindString(UeProperty parent, byte[] data, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal) &&
                child.Type is "StrProperty" or "NameProperty")
            {
                var pos = child.ValueOffset;
                return UePropertyReader.ReadFString(data, ref pos);
            }
        }

        return null;
    }

    private static bool? FindBool(UeProperty parent, byte[] data, string name)
    {
        foreach (var child in parent.Children)
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal) &&
                string.Equals(child.Type, "BoolProperty", StringComparison.Ordinal))
            {
                return UePropertyReader.ReadBool(data, child);
            }
        }

        return null;
    }
}
