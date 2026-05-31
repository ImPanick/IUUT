using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Engine Flags editor (master §8.11): adds/removes raw engine unlock flag IDs in the binary
/// <c>flags_&lt;SteamID&gt;.dat</c> via <see cref="FlagsEditService"/>, then writes through
/// <see cref="CustomFileService"/> (backup + atomic byte write). Advanced/low-level — flag IDs have
/// no friendly names. The confirm lives in the view.
/// </summary>
public sealed class FlagEditorViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly FlagsEditService _service;
    private readonly string _saveFolder;

    private FlagsFileModel? _model;
    private uint? _selectedFlag;
    private string _newFlagText = "";
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public FlagEditorViewModel(CustomFileService files, FlagsEditService service, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _service = service;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Flags = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddFlagCommand = new RelayCommand(AddFlag, () => !IsBusy && _model is not null);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => !IsBusy && _model is not null && SelectedFlag.HasValue);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The engine unlock flag IDs.</summary>
    public ObservableCollection<uint> Flags { get; }

    /// <summary>Reloads the flags file into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Adds the flag id typed in <see cref="NewFlagText"/>.</summary>
    public IRelayCommand AddFlagCommand { get; }

    /// <summary>Removes the selected flag id.</summary>
    public IRelayCommand RemoveSelectedCommand { get; }

    /// <summary>The flags file's SteamID (read-only display).</summary>
    public string SteamId => _model?.SteamId ?? "—";

    /// <summary>The selected flag id (the removal target).</summary>
    public uint? SelectedFlag
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

    /// <summary>The flag id to add (free text; parsed as an unsigned integer).</summary>
    public string NewFlagText
    {
        get => _newFlagText;
        set => SetProperty(ref _newFlagText, value);
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
        if (_model is null)
        {
            return;
        }

        if (!uint.TryParse(NewFlagText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var flagId))
        {
            StatusMessage = "Enter a whole number (0 – 4,294,967,295) to add.";
            return;
        }

        StatusMessage = _service.AddFlag(_model, flagId)
            ? $"Added flag {flagId} — review, then Apply."
            : $"Flag {flagId} is already present.";
        NewFlagText = "";
        RebuildFlags();
    }

    private void RemoveSelected()
    {
        if (_model is null || SelectedFlag is null)
        {
            return;
        }

        var flagId = SelectedFlag.Value;
        if (_service.RemoveFlag(_model, flagId))
        {
            StatusMessage = $"Removed flag {flagId} — review, then Apply.";
            RebuildFlags();
        }
    }

    private void RebuildFlags()
    {
        Flags.Clear();
        if (_model is not null)
        {
            foreach (var flag in _model.Flags)
            {
                Flags.Add(flag);
            }
        }

        SelectedFlag = null;
    }
}
