using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;

namespace IUUT.Core.Editing;

/// <summary>
/// Moves items trapped inside a prospect world back to the orbital stash (the "return trapped items"
/// feature). Decompresses the prospect's <c>ProspectBlob</c>, finds every inventory slot, adds the items
/// (with their real stack counts) to a <see cref="MetaInventoryModel"/> via <see cref="StashEditService"/>
/// — coalesced into stacks of <see cref="StashEditService.MaxStack"/> — then removes the slots from the
/// prospect world and re-stamps the blob. The caller saves both files (atomic, via the file service).
/// </summary>
/// <remarks>
/// Quantities use the decoded <c>DynamicData</c> stack index (see <see cref="ProspectWorldEditor.StackIndex"/>).
/// This is a model-level operation (no file I/O) so it is fully unit-testable; both the prospect blob and
/// the stash model are mutated in place.
/// </remarks>
public sealed class ProspectReturnService
{
    private readonly StashEditService _stash;

    /// <summary>Creates the service with the stash editor used to add returned items.</summary>
    public ProspectReturnService(StashEditService stash)
    {
        ArgumentNullException.ThrowIfNull(stash);
        _stash = stash;
    }

    /// <summary>One kind of trapped item: its RowName, how many slots hold it, and the total quantity.</summary>
    public sealed record TrappedItem(string RowName, int SlotCount, int TotalQuantity);

    /// <summary>The outcome of a return: what was moved.</summary>
    public sealed record ProspectReturnResult(
        IReadOnlyList<TrappedItem> Items,
        int SlotsRemoved,
        int TotalQuantity,
        int StashStacksAdded);

    /// <summary>Lists the items trapped in a prospect (read-only), grouped by RowName with summed quantities.</summary>
    public IReadOnlyList<TrappedItem> Preview(ProspectFileModel prospect)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        var editor = OpenEditor(prospect);
        return Summarize(editor.FindItemSlots());
    }

    /// <summary>
    /// Returns trapped items to <paramref name="stash"/>. <paramref name="rowNames"/> null = every item,
    /// else only matching RowNames; <paramref name="scope"/> further restricts to slots whose owning actor
    /// matches (e.g. <see cref="SlotOwner.IsPlayerOwned"/>). Mutates both <paramref name="prospect"/>'s blob
    /// and <paramref name="stash"/> in place and reports what moved.
    /// </summary>
    public ProspectReturnResult Return(
        ProspectFileModel prospect,
        MetaInventoryModel stash,
        IReadOnlySet<string>? rowNames = null,
        Func<ProspectWorldEditor.SlotRef, bool>? scope = null)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        ArgumentNullException.ThrowIfNull(stash);

        var editor = OpenEditor(prospect);
        var selected = editor.FindItemSlots()
            .Where(s => (rowNames is null || rowNames.Contains(s.RowName)) && (scope is null || scope(s)))
            .ToList();

        var summary = Summarize(selected);
        var stacksAdded = 0;
        foreach (var item in summary)
        {
            var remaining = item.TotalQuantity;
            while (remaining > 0)
            {
                var take = Math.Min(remaining, StashEditService.MaxStack);
                var added = _stash.AddItem(stash, item.RowName);
                _stash.SetStack(added, take);
                remaining -= take;
                stacksAdded++;
            }
        }

        foreach (var slot in selected)
        {
            editor.RemoveSlot(slot);
        }

        ProspectBlobCodec.SetUncompressed(prospect.ProspectBlob, editor.Serialize());

        return new ProspectReturnResult(summary, selected.Count, summary.Sum(i => i.TotalQuantity), stacksAdded);
    }

    /// <summary>
    /// Returns only the player's recoverable items — carried inventory, deployed containers, and mount
    /// bags (<see cref="SlotOwner.IsPlayerOwned"/>) — leaving world machines (drills, geysers) untouched.
    /// </summary>
    public ProspectReturnResult ReturnPlayerOwned(ProspectFileModel prospect, MetaInventoryModel stash) =>
        Return(prospect, stash, scope: s => SlotOwner.IsPlayerOwned(s.OwnerComponentClass));

    private static ProspectWorldEditor OpenEditor(ProspectFileModel prospect)
    {
        var decompressed = ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob);
        return new ProspectWorldEditor(UeBlob.Parse(decompressed));
    }

    private static List<TrappedItem> Summarize(IReadOnlyList<ProspectWorldEditor.SlotRef> slots) =>
        slots
            .GroupBy(s => s.RowName, StringComparer.Ordinal)
            .Select(g => new TrappedItem(g.Key, g.Count(), g.Sum(s => Math.Max(1, s.Stack))))
            .OrderByDescending(i => i.TotalQuantity)
            .ToList();
}
