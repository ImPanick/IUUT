using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.Core.Editing;
using IUUT.Core.Parsers;
using IUUT.Core.Prospects.World;
using IUUT.Core.Serializers;

namespace IUUT.App.ViewModels;

/// <summary>One quest step shown in the Prospect Quests panel.</summary>
public sealed record ProspectQuestStepViewModel(string QuestName, bool IsComplete);

/// <summary>
/// Prospect Quests (Tier 3): per-prospect mission state read straight from the world blob
/// (<see cref="ProspectQuestReader"/>), with the gated RESET MISSION write
/// (<see cref="ProspectQuestEditor"/> — in-place, size-preserving; items/mounts/bases are
/// untouched). The same engine as <c>iuut quest-reset</c>, surfaced in the app.
/// </summary>
public sealed class ProspectQuestsViewModel : ObservableObject
{
    private readonly CustomFileService _files;
    private readonly string _saveFolder;
    private readonly ProspectQuestReader _reader = new();

    private NamedFileViewModel? _selectedProspect;
    private bool _isBusy;
    private string _statusMessage = "Loading the selected save…";
    private string _missionSummary = "";

    /// <summary>Creates the panel for one save profile folder.</summary>
    public ProspectQuestsViewModel(CustomFileService files, string saveFolder, string profileLabel)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);

        _files = files;
        _saveFolder = saveFolder;
        ProfileLabel = string.IsNullOrEmpty(profileLabel) ? "this save" : profileLabel;

        Prospects = [];
        Steps = [];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    /// <summary>The profile being viewed (for the header).</summary>
    public string ProfileLabel { get; }

    /// <summary>The save's prospect world files.</summary>
    public ObservableCollection<NamedFileViewModel> Prospects { get; }

    /// <summary>The selected prospect's quest steps.</summary>
    public ObservableCollection<ProspectQuestStepViewModel> Steps { get; }

    /// <summary>Relists the prospect files.</summary>
    public IAsyncRelayCommand LoadCommand { get; }

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

    /// <summary>Mission line for the header (name + progress), or empty for open-world prospects.</summary>
    public string MissionSummary
    {
        get => _missionSummary;
        private set => SetProperty(ref _missionSummary, value);
    }

    /// <summary>Whether the selected prospect has any quest progress to reset.</summary>
    public bool CanReset => Steps.Count > 0 && !IsBusy;

    /// <summary>How many steps are marked complete (for the confirm dialog).</summary>
    public int CompleteSteps => Steps.Count(s => s.IsComplete);

    /// <summary>True while loading, previewing, or resetting.</summary>
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

    /// <summary>True once the prospect list was read and the panel is usable.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Lists (or relists) the prospect world files.</summary>
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            Prospects.Clear();
            Steps.Clear();
            MissionSummary = "";

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
            OnPropertyChanged(nameof(CanReset));
        }
    }

    /// <summary>Resets the selected prospect's mission (call after a user confirm).</summary>
    public async Task ResetMissionAsync()
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
            var result = ProspectQuestEditor.ResetMission(model);
            if (!result.Changed)
            {
                StatusMessage = "Nothing to reset — the mission is already at its initial state.";
                return;
            }

            var save = await _files.SaveJsonTextAsync(SelectedProspect.Path, ProspectFileSerializer.Serialize(model));
            StatusMessage = save.Ok
                ? $"Reset {result.StepsReset} step(s) ({result.VariablesCleared} variables) — a backup was taken. The mission can be replayed."
                : $"Reset failed; the original prospect is unchanged. {save.Error?.Message}";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Reset failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }

        var outcome = StatusMessage;
        await PreviewAsync();
        StatusMessage = outcome;
    }

    private async Task PreviewAsync()
    {
        Steps.Clear();
        MissionSummary = "";

        if (SelectedProspect is null)
        {
            OnPropertyChanged(nameof(CanReset));
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

            var state = _reader.ReadBlob(ProspectFileParser.Parse(json).ProspectBlob);
            foreach (var step in state.Steps)
            {
                Steps.Add(new ProspectQuestStepViewModel(step.QuestName, step.IsComplete));
            }

            MissionSummary = state.HasMission
                ? $"{state.MissionName} — {(state.MissionComplete ? "COMPLETE" : "in progress")} · {CompleteSteps}/{Steps.Count} steps done"
                : (Steps.Count > 0 ? $"open world · {CompleteSteps}/{Steps.Count} quest steps done" : "");
            StatusMessage = Steps.Count > 0
                ? $"{SelectedProspect.Name}: {MissionSummary}"
                : $"{SelectedProspect.Name}: no mission or quest state.";
        }
#pragma warning disable CA1031 // UI boundary: surface, never crash.
        catch (Exception ex)
        {
            StatusMessage = $"Could not read the prospect's quest state: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanReset));
            OnPropertyChanged(nameof(CompleteSteps));
        }
    }
}
