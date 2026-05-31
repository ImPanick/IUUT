using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Mounts editor (master §8.10): loads <c>Mounts.json</c>, lets the user rename mounts and set
/// their denormalized level, then writes through <see cref="CustomFileService"/> (backed up +
/// atomic). The authoritative RecorderBlob round-trips verbatim. Confirm lives in the view.
/// </summary>
public sealed class MountEditorViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly MountEditService _service;
    private readonly string _saveFolder;

    private MountsModel? _model;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public MountEditorViewModel(CustomFileService files, MountEditService service, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _service = service;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Mounts = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The save's tamed mounts.</summary>
    public ObservableCollection<MountSlotViewModel> Mounts { get; }

    /// <summary>Reloads the save into the editor.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Whether the save has at least one mount (drives the empty state).</summary>
    public bool HasMounts => Mounts.Count > 0;

    /// <summary>True while loading or applying.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Status-bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>True once <c>Mounts.json</c> parsed and the editor is usable.</summary>
    public bool IsLoaded => _model is not null;

    /// <summary>Loads (or reloads) the save's mounts into the editor.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _model = await _files.LoadMountsAsync(_saveFolder);
            Mounts.Clear();

            if (_model is null)
            {
                StatusMessage = "Could not load this save's Mounts.json (missing or unreadable).";
                OnPropertyChanged(nameof(HasMounts));
                return;
            }

            foreach (var mount in _model.SavedMounts)
            {
                Mounts.Add(new MountSlotViewModel(mount));
            }

            OnPropertyChanged(nameof(HasMounts));
            StatusMessage = Mounts.Count > 0
                ? $"Loaded {Mounts.Count} mount(s) for “{ProfileLabel}”."
                : "This save has no tamed mounts.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not load the save: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Applies the edited mounts (call after a user confirm).</summary>
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
            foreach (var mount in Mounts)
            {
                mount.WriteTo(_service);
            }

            var result = await _files.SaveMountsAsync(_saveFolder, _model);
            StatusMessage = result.Ok
                ? "Applied mount changes to Mounts.json — a backup was taken."
                : "Apply failed; the original Mounts.json is unchanged.";
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
}
