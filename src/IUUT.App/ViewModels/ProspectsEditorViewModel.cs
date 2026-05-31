using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Prospects editor (master §8.8): per-character <c>AssociatedProspects_Slot_N.json</c> files.
/// "Unstick" removes a phantom prospect association (a stuck Continue-menu entry) via
/// <see cref="ProspectEditService"/>, writing the slot file through <see cref="CustomFileService"/>
/// (backed up + atomic). The world blob is never touched. The confirm lives in the view.
/// </summary>
public sealed class ProspectsEditorViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly ProspectEditService _service;
    private readonly string _saveFolder;

    private AssociatedProspectsModel? _model;
    private NamedFileViewModel? _selectedFile;
    private string? _selectedAssociation;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public ProspectsEditorViewModel(CustomFileService files, ProspectEditService service, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _service = service;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Files = [];
        Associations = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        LoadSelectedFileCommand = new AsyncRelayCommand(LoadSelectedFileAsync);
    }

    /// <summary>The profile being edited (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The per-slot association files.</summary>
    public ObservableCollection<NamedFileViewModel> Files { get; }

    /// <summary>The selected file's prospect-association ids.</summary>
    public ObservableCollection<string> Associations { get; }

    /// <summary>Reloads the list of slot files.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Loads the selected slot file's associations.</summary>
    public IAsyncRelayCommand LoadSelectedFileCommand { get; }

    /// <summary>The selected slot file.</summary>
    public NamedFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                LoadSelectedFileCommand.Execute(null);
            }
        }
    }

    /// <summary>The selected association (the unstick target).</summary>
    public string? SelectedAssociation
    {
        get => _selectedAssociation;
        set
        {
            if (SetProperty(ref _selectedAssociation, value))
            {
                OnPropertyChanged(nameof(HasSelectedAssociation));
            }
        }
    }

    /// <summary>Whether an association is selected (enables Unstick).</summary>
    public bool HasSelectedAssociation => SelectedAssociation is not null;

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

    /// <summary>True once the slot files were listed and the editor is usable.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Lists (or relists) the slot files.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Files.Clear();
            Associations.Clear();
            _model = null;

            foreach (var path in _files.ResolveAssociatedProspectFiles(_saveFolder))
            {
                Files.Add(new NamedFileViewModel(path));
            }

            IsLoaded = true;
            if (Files.Count == 0)
            {
                StatusMessage = "No AssociatedProspects_Slot_*.json files in this save folder.";
                return;
            }

            // Select the first file directly (bypassing the command) and await its associations.
            _selectedFile = Files[0];
            OnPropertyChanged(nameof(SelectedFile));
            await LoadSelectedFileAsync();
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not list the slot files: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Removes the selected association (call after a user confirm).</summary>
    public async Task UnstickSelectedAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_model is null || SelectedFile is null || SelectedAssociation is null)
        {
            StatusMessage = "Select a prospect association to unstick.";
            return;
        }

        var prospectId = SelectedAssociation;
        var path = SelectedFile.Path;
        IsBusy = true;
        try
        {
            if (!_service.Unstick(_model, prospectId))
            {
                StatusMessage = "That association is no longer present.";
                return;
            }

            var result = await _files.SaveAssociatedProspectsAsync(path, _model);
            StatusMessage = result.Ok
                ? $"Unstuck “{prospectId}” — a backup was taken."
                : "Apply failed; the original slot file is unchanged.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Unstick failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        await LoadSelectedFileAsync();
    }

    private async Task LoadSelectedFileAsync()
    {
        Associations.Clear();
        SelectedAssociation = null;
        _model = null;

        if (SelectedFile is null)
        {
            return;
        }

        try
        {
            _model = await _files.LoadAssociatedProspectsAsync(SelectedFile.Path);
            if (_model is null)
            {
                StatusMessage = $"Could not read {SelectedFile.Name}.";
                return;
            }

            foreach (var prospect in _model.Prospects)
            {
                Associations.Add(prospect.ProspectId);
            }

            StatusMessage = $"{SelectedFile.Name}: {Associations.Count} association(s).";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not read the slot file: {ex.Message}";
        }
#pragma warning restore CA1031
    }
}
