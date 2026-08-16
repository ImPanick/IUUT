using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;
using IUUT.Core.Parsers;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;
using IUUT.Core.Serializers;

namespace IUUT.App.ViewModels;

/// <summary>One character in the rescue panel, with what they are carrying.</summary>
public sealed record RescueCharacterViewModel(ProspectCharacter Character, string Carrying)
{
    /// <summary>Masked player id and character slot — never the raw SteamID (CONSTITUTION VII).</summary>
    public string Title => $"Player {Character.MaskedPlayerId} · character slot {Character.CharacterSlot}";

    /// <summary>Vitals and position, in plain language.</summary>
    public string Detail => Character.Location is null
        ? $"{State} · position unknown"
        : string.Create(CultureInfo.CurrentCulture,
            $"{State} · {Character.Health} hp · at ({Character.Location.Metres.X:N0}, {Character.Location.Metres.Y:N0}) m");

    /// <summary>Whether this character is dead and would need reviving.</summary>
    public bool IsDead => !Character.IsAlive;

    private string State => Character.IsAlive ? "alive" : "DEAD";
}

/// <summary>One body or grave marker in the rescue panel.</summary>
public sealed record RescueGraveViewModel(ProspectGrave Grave)
{
    /// <summary>What kind of grave this is.</summary>
    public string Title => Grave.Kind == GraveKind.MissingInAction
        ? "Missing-in-action marker"
        : "Downed body (revivable in-game)";

    /// <summary>How much it holds and where it is.</summary>
    public string Detail => Grave.Placement is null
        ? $"{Grave.ItemSlots} item slot(s) · position unknown"
        : string.Create(CultureInfo.CurrentCulture,
            $"{Grave.ItemSlots} item slot(s) · at ({Grave.Placement.Metres.X:N0}, {Grave.Placement.Metres.Y:N0}) m");
}

/// <summary>
/// Prospect Rescue (RESCUE group): the panel for when the game has stranded you — a zone reset
/// behind you, a boss glitched and pinned a body somewhere unreachable, or a world will not resume.
/// <para>
/// Deliberately coordinate-free. You pick WHO is stuck and WHERE they should go by clicking the
/// things on screen; the panel works out the positions. Every action previews into the status line
/// and only writes after an explicit confirm, with a backup taken first.
/// </para>
/// </summary>
public sealed class ProspectRescueViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly string _saveFolder;
    private readonly ProspectCharacterReader _characters = new();
    private readonly ProspectGraveReader _graves = new();
    private readonly ProspectInventoryReader _inventories = new();

    private NamedFileViewModel? _selectedProspect;
    private RescueCharacterViewModel? _selectedCharacter;
    private RescueGraveViewModel? _selectedGrave;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the panel for one save profile folder.</summary>
    public ProspectRescueViewModel(CustomFileService files, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Prospects = [];
        Characters = [];
        Graves = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being viewed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The save's prospect world files.</summary>
    public ObservableCollection<NamedFileViewModel> Prospects { get; }

    /// <summary>Characters recorded in the selected prospect.</summary>
    public ObservableCollection<RescueCharacterViewModel> Characters { get; }

    /// <summary>Bodies and grave markers in the selected prospect.</summary>
    public ObservableCollection<RescueGraveViewModel> Graves { get; }

    /// <summary>Relists the prospect files.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>True once the prospect list was read and the panel is usable.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>The prospect being viewed.</summary>
    public NamedFileViewModel? SelectedProspect
    {
        get => _selectedProspect;
        set
        {
            if (SetProperty(ref _selectedProspect, value))
            {
                _ = PreviewAsync();
            }
        }
    }

    /// <summary>The character the actions apply to.</summary>
    public RescueCharacterViewModel? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (SetProperty(ref _selectedCharacter, value))
            {
                RaiseActionStates();
            }
        }
    }

    /// <summary>The grave the actions apply to.</summary>
    public RescueGraveViewModel? SelectedGrave
    {
        get => _selectedGrave;
        set
        {
            if (SetProperty(ref _selectedGrave, value))
            {
                RaiseActionStates();
            }
        }
    }

    /// <summary>True while loading or writing.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseActionStates();
            }
        }
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Whether anything at all was found to rescue.</summary>
    public bool HasAnything => Characters.Count > 0 || Graves.Count > 0;

    /// <summary>Whether any body or grave marker is stranded here.</summary>
    public bool HasGraves => Graves.Count > 0;

    /// <summary>Whether a grave can be brought to the selected character.</summary>
    public bool CanBringGrave =>
        !IsBusy && SelectedGrave?.Grave.Placement is not null && SelectedCharacter?.Character.Location is not null;

    /// <summary>Whether the selected character can be sent to the selected grave.</summary>
    public bool CanGoToGrave => CanBringGrave;

    /// <summary>Whether the selected character is dead and can be revived.</summary>
    public bool CanRevive => !IsBusy && SelectedCharacter?.IsDead == true;

    /// <summary>Confirmation text for bringing a grave to a character.</summary>
    public string BringGraveSummary => SelectedGrave is null || SelectedCharacter is null
        ? ""
        : $"Move the {SelectedGrave.Grave.Label} holding {SelectedGrave.Grave.ItemSlots} item slot(s) "
        + $"next to player {SelectedCharacter.Character.MaskedPlayerId}. The grave moves — its contents are "
        + "never converted or re-homed, so you loot it in-game exactly as normal.";

    /// <summary>Confirmation text for sending a character to their grave.</summary>
    public string GoToGraveSummary => SelectedGrave is null || SelectedCharacter is null
        ? ""
        : $"Move player {SelectedCharacter.Character.MaskedPlayerId} to the {SelectedGrave.Grave.Label}. "
        + "Everything they are carrying travels with them.";

    /// <summary>Confirmation text for reviving.</summary>
    public string ReviveSummary => SelectedCharacter is null
        ? ""
        : $"Revive player {SelectedCharacter.Character.MaskedPlayerId} where they are, with enough health to stand up.";

    /// <summary>Lists (or relists) the prospect world files.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Prospects.Clear();
            Characters.Clear();
            Graves.Clear();

            foreach (var path in _files.ResolveProspectFiles(_saveFolder))
            {
                Prospects.Add(new NamedFileViewModel(path));
            }

            IsLoaded = true;
            if (Prospects.Count == 0)
            {
                StatusMessage = "No prospect world saves in this save folder.";
                return;
            }

            _selectedProspect = Prospects[0];
            OnPropertyChanged(nameof(SelectedProspect));
            await PreviewAsync();
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not list the prospects: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Moves the selected grave next to the selected character (call after a confirm).</summary>
    public Task BringGraveToCharacterAsync() => WriteAsync(model =>
    {
        var to = SelectedCharacter!.Character.Location!;
        var from = SelectedGrave!.Grave.Placement!;
        var result = ProspectHomesteadEditor.MoveActors(
            model,
            [SelectedGrave.Grave.ActorGuid],
            to.Metres.X + 2 - from.Metres.X,
            to.Metres.Y - from.Metres.Y,
            to.Metres.Z - from.Metres.Z);

        return result.Changed
            ? $"Brought the {SelectedGrave.Grave.Label} ({SelectedGrave.Grave.ItemSlots} item slots) to player "
              + $"{SelectedCharacter.Character.MaskedPlayerId}"
            : null;
    });

    /// <summary>Moves the selected character to the selected grave (call after a confirm).</summary>
    public Task SendCharacterToGraveAsync() => WriteAsync(model =>
    {
        var grave = SelectedGrave!.Grave.Placement!;
        var result = ProspectCharacterEditor.Rescue(
            model, SelectedCharacter!.Character, grave.Metres.X + 2, grave.Metres.Y, grave.Metres.Z);

        return result.Changed
            ? $"Moved player {SelectedCharacter.Character.MaskedPlayerId} to their {SelectedGrave.Grave.Label}"
            : null;
    });

    /// <summary>Revives the selected character where they stand (call after a confirm).</summary>
    public Task ReviveCharacterAsync() => WriteAsync(model =>
    {
        var at = SelectedCharacter!.Character.Location;
        if (at is null)
        {
            return null;
        }

        var result = ProspectCharacterEditor.Rescue(
            model, SelectedCharacter.Character, at.Metres.X, at.Metres.Y, at.Metres.Z, revive: true);

        return result.Revived ? $"Revived player {SelectedCharacter.Character.MaskedPlayerId}" : null;
    });

    // One write path for every action: read, mutate, save with a backup, then re-read so the
    // panel always reflects the file rather than what we hoped we wrote.
    private async Task WriteAsync(Func<IUUT.Core.Models.ProspectFileModel, string?> mutate)
    {
        if (IsBusy || SelectedProspect is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var json = await _files.ReadTextAsync(SelectedProspect.Path);
            if (json is null)
            {
                StatusMessage = "Could not read the prospect file.";
                return;
            }

            var model = ProspectFileParser.Parse(json);
            var outcome = mutate(model);
            if (outcome is null)
            {
                StatusMessage = "Nothing changed.";
                return;
            }

            var save = await _files.SaveJsonTextAsync(SelectedProspect.Path, ProspectFileSerializer.Serialize(model));
            if (!save.Ok)
            {
                StatusMessage = $"Write failed; the prospect is unchanged. {save.Error?.Message}";
                return;
            }

            // Read the file back off disk and check it the way the game will. A blob the game
            // rejects is not reported as an error — it silently discards the world and generates a
            // new one — so the only safe assumption is that an unverified write is a broken write.
            var verify = await VerifyOnDiskAsync(SelectedProspect.Path);
            StatusMessage = verify is null
                ? $"{outcome} — verified on disk, and a backup was taken. Everyone must be OUT of this prospect, or the running session will overwrite it."
                : $"WROTE A FILE THAT DID NOT VERIFY: {verify}. Restore the backup beside the prospect "
                  + $"({save.BackupPath}) before loading the world.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Rescue failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        var message = StatusMessage;
        await PreviewAsync();
        StatusMessage = message;
    }

    // Re-reads a written prospect and checks everything the game checks. Returns null when the
    // file is sound, or a plain-English reason when it is not.
    private async Task<string?> VerifyOnDiskAsync(string path)
    {
        try
        {
            var json = await _files.ReadTextAsync(path);
            if (json is null)
            {
                return "the file could not be read back";
            }

            var reread = ProspectFileParser.Parse(json);
            var blob = ProspectBlobCodec.Decompress(reread.ProspectBlob.BinaryBlob);

            if (!ProspectBlobVerifier.VerifyHash(reread.ProspectBlob))
            {
                return "the world blob's hash does not match its contents";
            }

            var hash = reread.ProspectBlob.Hash;
            if (!string.Equals(hash, hash.ToLowerInvariant(), StringComparison.Ordinal))
            {
                return "the world blob's hash is not in the game's lowercase form";
            }

            if (_characters.Read(blob).Count == 0)
            {
                return "no characters could be read back from the written world";
            }

            return null;
        }
#pragma warning disable CA1031 // Any failure to verify must be reported as a failure, whatever its type.
        catch (Exception ex)
        {
            return $"reading it back failed ({ex.GetType().Name}: {ex.Message})";
        }
#pragma warning restore CA1031
    }

    private async Task PreviewAsync()
    {
        Characters.Clear();
        Graves.Clear();
        _selectedCharacter = null;
        _selectedGrave = null;
        OnPropertyChanged(nameof(SelectedCharacter));
        OnPropertyChanged(nameof(SelectedGrave));

        if (SelectedProspect is null)
        {
            RaiseActionStates();
            return;
        }

        IsBusy = true;
        try
        {
            var json = await _files.ReadTextAsync(SelectedProspect.Path);
            if (json is null)
            {
                StatusMessage = $"Could not read {SelectedProspect.Name}.";
                return;
            }

            var model = ProspectFileParser.Parse(json);
            var blob = ProspectBlobCodec.Decompress(model.ProspectBlob.BinaryBlob);

            foreach (var character in _characters.Read(blob))
            {
                Characters.Add(new RescueCharacterViewModel(character, DescribeCarrying(blob, character)));
            }

            foreach (var grave in _graves.Read(blob))
            {
                Graves.Add(new RescueGraveViewModel(grave));
            }

            StatusMessage = Graves.Count > 0
                ? $"{Characters.Count} character(s) and {Graves.Count} grave(s). Pick one of each, then choose an action."
                : $"{Characters.Count} character(s), no graves — nobody's body is stranded here.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not read this prospect: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasAnything));
            OnPropertyChanged(nameof(HasGraves));
        }
    }

    // The inventory panels the game itself shows, so you can see what is at stake before moving anyone.
    private string DescribeCarrying(byte[] blob, ProspectCharacter character)
    {
        try
        {
            var parts = _inventories.Read(blob, character)
                .Where(i => i.OccupiedCount > 0)
                .Select(i => $"{i.Label} {i.OccupiedCount}")
                .ToList();
            return parts.Count == 0 ? "carrying nothing" : string.Join(" · ", parts);
        }
#pragma warning disable CA1031 // A readable character with an odd inventory must still be rescuable.
        catch (Exception)
        {
            return $"{character.CarriedSlots} item slot(s)";
        }
#pragma warning restore CA1031
    }

    private void RaiseActionStates()
    {
        OnPropertyChanged(nameof(CanBringGrave));
        OnPropertyChanged(nameof(CanGoToGrave));
        OnPropertyChanged(nameof(CanRevive));
        OnPropertyChanged(nameof(BringGraveSummary));
        OnPropertyChanged(nameof(GoToGraveSummary));
        OnPropertyChanged(nameof(ReviveSummary));
    }
}
