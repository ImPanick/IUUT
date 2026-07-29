using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// The Field Guide: the tracked statistics, task-list checklists, and fishing records that the
/// game keeps but IUUT previously only round-tripped.
/// <list type="bullet">
/// <item><c>Accolades.json</c> → <c>PlayerTrackers</c>: a flat counter map (distance travelled,
/// time survived, creatures killed, …) keyed by a UE struct-string.</item>
/// <item><c>Accolades.json</c> → <c>PlayerTaskListTrackers</c>: checklists (e.g. visit every
/// biome) whose <c>CompletedTasks</c> array names what is done.</item>
/// <item><c>BestiaryData.json</c> → <c>FishTracking</c>: per-fish caught count and best
/// quality/weight/length.</item>
/// </list>
/// The two Accolades blocks are edited <b>in their preserved JSON form</b> rather than re-typed,
/// so every unknown member of every entry still round-trips verbatim (CONSTITUTION VI) and a
/// future schema change cannot break loading the file.
/// </summary>
public sealed partial class FieldGuideEditService
{
    /// <summary>The data table the tracker keys reference.</summary>
    public const string TrackerDataTable = "D_PlayerTrackers";

    private const string TrackersProperty = "PlayerTrackers";
    private const string TaskListsProperty = "PlayerTaskListTrackers";
    private const string CompletedTasksProperty = "CompletedTasks";

    /// <summary>One tracked statistic: its row name and current value.</summary>
    public sealed record TrackedStat(string RowName, long Value);

    /// <summary>One task-list checklist: its row name and the tasks already completed.</summary>
    public sealed record TaskList(string RowName, IReadOnlyList<string> CompletedTasks);

    /// <summary>Every tracked statistic in <paramref name="accolades"/>, ordered by row name.</summary>
    public IReadOnlyList<TrackedStat> ListStats(AccoladesModel accolades)
    {
        var stats = new List<TrackedStat>();
        foreach (var (key, value) in Entries(accolades, TrackersProperty))
        {
            if (RowNameOf(key) is { } rowName && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var v))
            {
                stats.Add(new TrackedStat(rowName, v));
            }
        }

        return stats.OrderBy(s => s.RowName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Every task-list checklist in <paramref name="accolades"/>, ordered by row name.</summary>
    public IReadOnlyList<TaskList> ListTaskLists(AccoladesModel accolades)
    {
        var lists = new List<TaskList>();
        foreach (var (key, value) in Entries(accolades, TaskListsProperty))
        {
            if (RowNameOf(key) is not { } rowName || value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var tasks = new List<string>();
            if (value.TryGetProperty(CompletedTasksProperty, out var completed) && completed.ValueKind == JsonValueKind.Array)
            {
                tasks.AddRange(completed.EnumerateArray()
                    .Where(t => t.ValueKind == JsonValueKind.String)
                    .Select(t => t.GetString()!));
            }

            lists.Add(new TaskList(rowName, tasks));
        }

        return lists.OrderBy(l => l.RowName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Sets a tracked statistic (clamped to ≥ 0), adding the entry when the save has never
    /// recorded it. Returns false when nothing changed.
    /// </summary>
    public bool SetStat(AccoladesModel accolades, string rowName, long value)
    {
        ArgumentNullException.ThrowIfNull(accolades);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var node = MutableBlock(accolades, TrackersProperty);
        var key = FindKey(node, rowName) ?? Key(rowName);
        if (node[key] is JsonValue existing && existing.TryGetValue<long>(out var current) && current == Math.Max(0, value))
        {
            return false;
        }

        node[key] = JsonValue.Create(Math.Max(0, value));
        Commit(accolades, TrackersProperty, node);
        return true;
    }

    /// <summary>
    /// Marks a task complete (or not) within a task list. Adding to a list the save has never
    /// recorded creates it. Returns false when nothing changed.
    /// </summary>
    public bool SetTaskCompleted(AccoladesModel accolades, string listRowName, string task, bool completed)
    {
        ArgumentNullException.ThrowIfNull(accolades);
        ArgumentException.ThrowIfNullOrEmpty(listRowName);
        ArgumentException.ThrowIfNullOrEmpty(task);

        var node = MutableBlock(accolades, TaskListsProperty);
        var key = FindKey(node, listRowName) ?? Key(listRowName);
        if (node[key] is not JsonObject entry)
        {
            entry = [];
            node[key] = entry;
        }

        if (entry[CompletedTasksProperty] is not JsonArray tasks)
        {
            tasks = [];
            entry[CompletedTasksProperty] = tasks;
        }

        var index = -1;
        for (var i = 0; i < tasks.Count; i++)
        {
            if (tasks[i] is JsonValue v && v.TryGetValue<string>(out var s) &&
                string.Equals(s, task, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (completed == (index >= 0))
        {
            return false;
        }

        if (completed)
        {
            tasks.Add(JsonValue.Create(task));
        }
        else
        {
            tasks.RemoveAt(index);
        }

        Commit(accolades, TaskListsProperty, node);
        return true;
    }

    /// <summary>
    /// Sets a fish's records, adding the entry when the fish has never been caught. Values are
    /// clamped to ≥ 0. Returns the entry that was created or updated.
    /// </summary>
    public FishEntry SetFish(BestiaryModel bestiary, string fishRowName, long caught, long quality, long weight, long length)
    {
        ArgumentNullException.ThrowIfNull(bestiary);
        ArgumentException.ThrowIfNullOrEmpty(fishRowName);

        var entry = bestiary.FishTracking.FirstOrDefault(f =>
            string.Equals(f.FishRow.RowName, fishRowName, StringComparison.Ordinal));
        if (entry is null)
        {
            entry = new FishEntry { FishRow = new DataTableRef { RowName = fishRowName, DataTableName = "D_FishData" } };
            bestiary.FishTracking.Add(entry);
        }

        entry.CaughtCount = Math.Max(0, caught);
        entry.MaxQuality = Math.Max(0, quality);
        entry.MaxWeight = Math.Max(0, weight);
        entry.MaxLength = Math.Max(0, length);
        return entry;
    }

    /// <summary>The UE struct-string key the game uses, e.g. <c>(RowName="X",DataTableName="D_PlayerTrackers")</c>.</summary>
    public static string Key(string rowName) => $"(RowName=\"{rowName}\",DataTableName=\"{TrackerDataTable}\")";

    /// <summary>The <c>RowName</c> inside a struct-string key, or null when it doesn't parse.</summary>
    public static string? RowNameOf(string key)
    {
        var match = RowNameRegex().Match(key);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static IEnumerable<KeyValuePair<string, JsonElement>> Entries(AccoladesModel accolades, string property)
    {
        ArgumentNullException.ThrowIfNull(accolades);
        if (accolades.AdditionalData is null ||
            !accolades.AdditionalData.TryGetValue(property, out var block) ||
            block.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var entry in block.EnumerateObject())
        {
            yield return new KeyValuePair<string, JsonElement>(entry.Name, entry.Value);
        }
    }

    // The block as a mutable JsonObject (empty when the save has no such block yet).
    private static JsonObject MutableBlock(AccoladesModel accolades, string property)
    {
        if (accolades.AdditionalData is not null &&
            accolades.AdditionalData.TryGetValue(property, out var block) &&
            block.ValueKind == JsonValueKind.Object &&
            JsonNode.Parse(block.GetRawText()) is JsonObject parsed)
        {
            return parsed;
        }

        return [];
    }

    private static void Commit(AccoladesModel accolades, string property, JsonObject node)
    {
        accolades.AdditionalData ??= [];
        accolades.AdditionalData[property] = JsonSerializer.Deserialize<JsonElement>(node.ToJsonString());
    }

    // Match the save's existing key verbatim when present, so we never write a second entry for
    // the same row just because the game formatted the struct-string differently.
    private static string? FindKey(JsonObject node, string rowName)
    {
        foreach (var entry in node)
        {
            if (string.Equals(RowNameOf(entry.Key), rowName, StringComparison.Ordinal))
            {
                return entry.Key;
            }
        }

        return null;
    }

    [GeneratedRegex("RowName=\"([^\"]+)\"")]
    private static partial Regex RowNameRegex();
}
