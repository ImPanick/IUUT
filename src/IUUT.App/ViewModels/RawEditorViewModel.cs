using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Advanced / Raw editor (master §10.4): pick any save JSON file, view it, edit the text, and
/// save it back through <see cref="CustomFileService"/> (backup + JSON re-parse + atomic — malformed
/// JSON is rejected, leaving the original intact). For power users; the confirm lives in the view.
/// </summary>
public sealed class RawEditorViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly string _saveFolder;

    private NamedFileViewModel? _selectedFile;
    private string _content = "";
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";

    /// <summary>Creates the editor for one save profile folder.</summary>
    public RawEditorViewModel(CustomFileService files, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Files = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        LoadFileCommand = new AsyncRelayCommand(LoadFileAsync);
    }

    /// <summary>The profile being viewed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The save's JSON files.</summary>
    public ObservableCollection<NamedFileViewModel> Files { get; }

    /// <summary>Relists the JSON files.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

    /// <summary>Loads the selected file's text.</summary>
    public IAsyncRelayCommand LoadFileCommand { get; }

    /// <summary>The selected file.</summary>
    public NamedFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                OnPropertyChanged(nameof(HasFile));
                LoadFileCommand.Execute(null);
            }
        }
    }

    /// <summary>The editable raw text of the selected file.</summary>
    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    /// <summary>Whether a file is selected (enables Save).</summary>
    public bool HasFile => SelectedFile is not null;

    /// <summary>True while loading or saving.</summary>
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

    /// <summary>True once the JSON files were listed and the editor is usable.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Lists (or relists) the save's JSON files.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Files.Clear();
            foreach (var path in _files.ListJsonFiles(_saveFolder))
            {
                Files.Add(new NamedFileViewModel(path));
            }

            IsLoaded = true;
            if (Files.Count == 0)
            {
                Content = "";
                StatusMessage = "No JSON files found in this save folder.";
                return;
            }

            // Select the first file directly (bypassing the command) and await its text.
            _selectedFile = Files[0];
            OnPropertyChanged(nameof(SelectedFile));
            OnPropertyChanged(nameof(HasFile));
            await LoadFileAsync();
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not list files: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Saves the edited text (call after a user confirm).</summary>
    public async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedFile is null)
        {
            StatusMessage = "Select a file to save.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _files.SaveJsonTextAsync(SelectedFile.Path, Content);
            StatusMessage = result.Ok
                ? $"Saved {SelectedFile.Name} — a backup was taken."
                : $"Not saved — the text is not valid JSON (the original {SelectedFile.Name} is unchanged).";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadFileAsync()
    {
        if (SelectedFile is null)
        {
            Content = "";
            return;
        }

        var text = await _files.ReadTextAsync(SelectedFile.Path);
        Content = text ?? "";
        StatusMessage = text is null
            ? $"Could not read {SelectedFile.Name}."
            : $"{SelectedFile.Name} — {Content.Length:N0} chars.";
    }
}
