using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Engine Flags editor (master §8.11): the binary <c>flags_&lt;SteamID&gt;.dat</c> = the game's
/// <c>CharacterFlag</c> set. Decodes each flag id to its friendly name via the flag catalog, lets the
/// user add a flag by name or remove one, and offers "Complete missions" (set every mission/story
/// flag — e.g. the Olympus map-unlock gate + the Mission_* rewards). Writes through
/// <see cref="CustomFileService"/> (backup + atomic). The confirm lives in the view.
/// </summary>
public sealed class FlagEditorViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly FlagsEditService _service;
    private readonly FlagCatalog _catalog;
    private readonly string _saveFolder;

    private FlagsFileModel? _model;
    private FlagRowViewModel? _selectedFlag;
    private FlagRowViewModel? _selectedAvailableFlag;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public FlagEditorViewModel(
        CustomFileService files,
        FlagsEditService service,
        FlagCatalog catalog,
        string saveFolder,
        string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _service = service;
        _catalog = catalog;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Flags = [];
        AvailableFlags = catalog.Ids
            .Select(id => new FlagRowViewModel((uint)id, catalog.Label(id), catalog.IsMissionFlag(id)))
            .ToList();

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddFlagCommand = new RelayCommand(AddFlag, () => !IsBusy && _model is not null && SelectedAvailableFlag is not null);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => !IsBusy && _model is not null && SelectedFlag is not null);
        CompleteMissionsCommand = new RelayCommand(CompleteMissions, () => !IsBusy && _model is not null);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The character flags currently set (decoded to names).</summary>
    public ObservableCollection<FlagRowViewModel> Flags { get; }

    /// <summary>Every known character flag, for the add-by-name picker.</summary>
    public IReadOnlyList<FlagRowViewModel> AvailableFlags { get; }

    /// <summary>Reloads the flags file into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Adds the picked flag.</summary>
    public IRelayCommand AddFlagCommand { get; }

    /// <summary>Removes the selected flag.</summary>
    public IRelayCommand RemoveSelectedCommand { get; }

    /// <summary>Sets every mission/story flag (Olympus unlock + Mission_* rewards).</summary>
    public IRelayCommand CompleteMissionsCommand { get; }

    /// <summary>The flags file's SteamID (read-only display).</summary>
    public string SteamId => _model?.SteamId ?? "—";

    /// <summary>The selected current flag (the removal target).</summary>
    public FlagRowViewModel? SelectedFlag
    {
        get => _selectedFlag;
        set
        {
            if (SetProperty(ref _selectedFlag, value))
            {
                RemoveSelectedCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>The flag picked in the add picker.</summary>
    public FlagRowViewModel? SelectedAvailableFlag
    {
        get => _selectedAvailableFlag;
        set
        {
            if (SetProperty(ref _selectedAvailableFlag, value))
            {
                AddFlagCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                AddFlagCommand.NotifyCanExecuteChanged();
                RemoveSelectedCommand.NotifyCanExecuteChanged();
                CompleteMissionsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True once the flags file decoded and the editor is usable.</summary>
    public bool IsLoaded => _model is not null;

    /// <summary>Loads (or reloads) the flags file into the editor.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _model = await _files.LoadFlagsAsync(_saveFolder);
            RebuildFlags();
            OnPropertyChanged(nameof(SteamId));

            StatusMessage = _model is null
                ? "No flags_*.dat in this save folder (or it could not be decoded)."
                : $"Loaded {Flags.Count} engine flag(s) for “{ProfileLabel}”.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not load the flags file: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
            AddFlagCommand.NotifyCanExecuteChanged();
            CompleteMissionsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Writes the flags file (call after a user confirm).</summary>
    public async Task ApplyAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_model is null)
        {
            StatusMessage = "Nothing is loaded to apply.";
            return;
        }

        IsBusy = true;
        try
        {
            var ok = await _files.SaveFlagsAsync(_saveFolder, _model);
            StatusMessage = ok
                ? "Applied to the flags file — a backup was taken."
                : "Apply failed; the original flags file is unchanged.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        await LoadAsync();
    }

    private void AddFlag()
    {
        if (_model is null || SelectedAvailableFlag is null)
        {
            return;
        }

        var flag = SelectedAvailableFlag;
        StatusMessage = _service.AddFlag(_model, flag.Id)
            ? $"Added {flag.Label} (staged) — review, then Apply."
            : $"{flag.Label} is already present.";
        RebuildFlags();
    }

    private void RemoveSelected()
    {
        if (_model is null || SelectedFlag is null)
        {
            return;
        }

        var flag = SelectedFlag;
        if (_service.RemoveFlag(_model, flag.Id))
        {
            StatusMessage = $"Removed {flag.Label} (staged) — review, then Apply.";
            RebuildFlags();
        }
    }

    private void CompleteMissions()
    {
        if (_model is null)
        {
            return;
        }

        var added = 0;
        foreach (var id in _catalog.MissionFlagIds())
        {
            if (_service.AddFlag(_model, (uint)id))
            {
                added++;
            }
        }

        RebuildFlags();
        StatusMessage = added > 0
            ? $"Set {added} mission/story flag(s) incl. map unlocks (staged) — review, then Apply."
            : "All mission/story flags are already set.";
    }

    private void RebuildFlags()
    {
        Flags.Clear();
        if (_model is not null)
        {
            foreach (var flag in _model.Flags)
            {
                Flags.Add(new FlagRowViewModel(flag, _catalog.Label((int)flag), _catalog.IsMissionFlag((int)flag)));
            }
        }

        SelectedFlag = null;
    }
}
