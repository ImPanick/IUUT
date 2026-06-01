using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;

namespace IUUT.Core.Editing;

/// <summary>
/// File-level orchestration for "return trapped items → orbital stash": loads a prospect file and the
/// orbital stash, runs <see cref="ProspectReturnService"/>, and atomically saves both (each with a
/// backup, via <see cref="CustomFileService"/>). The stash is saved <em>before</em> the prospect so a
/// mid-operation failure duplicates items (recoverable) rather than losing them.
/// </summary>
public sealed class ProspectReturnFileService
{
    private readonly CustomFileService _files;
    private readonly ProspectReturnService _return;

    /// <summary>Creates the service from the file service and the model-level return service.</summary>
    public ProspectReturnFileService(CustomFileService files, ProspectReturnService returnService)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(returnService);
        _files = files;
        _return = returnService;
    }

    /// <summary>The outcome of a file-level return: success flag, what moved, the backups, and any error.</summary>
    public sealed record ProspectReturnFileResult(
        bool Ok,
        ProspectReturnService.ProspectReturnResult? Moved,
        string? StashBackupPath,
        string? ProspectBackupPath,
        string? Error);

    /// <summary>
    /// Previews (read-only) the items trapped in the prospect file at <paramref name="prospectFilePath"/>.
    /// Returns an empty list if the file is missing/unparseable.
    /// </summary>
    public async Task<IReadOnlyList<ProspectReturnService.TrappedItem>> PreviewAsync(
        string prospectFilePath,
        CancellationToken cancellationToken = default)
    {
        var prospect = await LoadProspectAsync(prospectFilePath, cancellationToken).ConfigureAwait(false);
        return prospect is null ? Array.Empty<ProspectReturnService.TrappedItem>() : _return.Preview(prospect);
    }

    /// <summary>
    /// Returns trapped items from the prospect file to the orbital stash in <paramref name="saveFolder"/>
    /// (<c>MetaInventory.json</c>), writing both files atomically. When <paramref name="rowNames"/> is null,
    /// every item is returned; otherwise only matching RowNames.
    /// </summary>
    public async Task<ProspectReturnFileResult> ReturnAsync(
        string prospectFilePath,
        string saveFolder,
        IReadOnlySet<string>? rowNames = null,
        CancellationToken cancellationToken = default)
    {
        var prospect = await LoadProspectAsync(prospectFilePath, cancellationToken).ConfigureAwait(false);
        if (prospect is null)
        {
            return new ProspectReturnFileResult(false, null, null, null, $"Prospect file not found or unparseable: {prospectFilePath}");
        }

        var stash = await _files.LoadStashAsync(saveFolder, cancellationToken).ConfigureAwait(false)
            ?? new MetaInventoryModel();

        ProspectReturnService.ProspectReturnResult moved;
        try
        {
            moved = _return.Return(prospect, stash, rowNames);
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException or InvalidOperationException)
        {
            return new ProspectReturnFileResult(false, null, null, null, $"Return failed: {ex.Message}");
        }

        if (moved.SlotsRemoved == 0)
        {
            return new ProspectReturnFileResult(true, moved, null, null, null); // nothing matched — no writes
        }

        // Stash first: if the prospect save then fails, items are duplicated (recoverable from backup),
        // never lost.
        var stashSave = await _files.SaveStashAsync(saveFolder, stash, cancellationToken).ConfigureAwait(false);
        if (!stashSave.Ok)
        {
            return new ProspectReturnFileResult(false, moved, null, null, $"Stash save failed: {stashSave.Error?.Message}");
        }

        var prospectSave = await _files
            .SaveJsonTextAsync(prospectFilePath, ProspectFileSerializer.Serialize(prospect), cancellationToken)
            .ConfigureAwait(false);
        if (!prospectSave.Ok)
        {
            return new ProspectReturnFileResult(
                false,
                moved,
                stashSave.BackupPath,
                null,
                $"Stash updated but prospect save failed — items may be duplicated; restore the prospect from its backup. {prospectSave.Error?.Message}");
        }

        return new ProspectReturnFileResult(true, moved, stashSave.BackupPath, prospectSave.BackupPath, null);
    }

    private async Task<ProspectFileModel?> LoadProspectAsync(string prospectFilePath, CancellationToken cancellationToken)
    {
        var json = await _files.ReadTextAsync(prospectFilePath, cancellationToken).ConfigureAwait(false);
        if (json is null)
        {
            return null;
        }

        try
        {
            return ProspectFileParser.Parse(json);
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
