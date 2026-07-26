using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>
/// One mission in the Missions checklist (master §8.8): complete = the profile owns its
/// <c>Prospect_*</c> reward talent. Completion is additive and idempotent, so completed rows
/// stay locked; incomplete rows can be staged for completion (prerequisites close at apply).
/// </summary>
public sealed class MissionRowViewModel : ObservableObject
{
    private bool _isStaged;

    /// <summary>Creates the row.</summary>
    public MissionRowViewModel(string rowName, string label, string tree, int prerequisiteCount, bool isComplete)
    {
        RowName = rowName;
        Label = label;
        Tree = tree;
        PrerequisiteCount = prerequisiteCount;
        IsComplete = isComplete;
    }

    /// <summary>The <c>Prospect_*</c> row name.</summary>
    public string RowName { get; }

    /// <summary>Human mission name.</summary>
    public string Label { get; }

    /// <summary>Region tree label (e.g. "Olympus").</summary>
    public string Tree { get; }

    /// <summary>Transitive prerequisite count (all complete along with this mission).</summary>
    public int PrerequisiteCount { get; }

    /// <summary>Whether the loaded profile already owns this mission.</summary>
    public bool IsComplete { get; }

    /// <summary>Whether the checkbox is interactive (completion is additive — done stays done).</summary>
    public bool CanStage => !IsComplete;

    /// <summary>Staged for completion (meaningful only while not complete).</summary>
    public bool IsStaged
    {
        get => _isStaged;
        set
        {
            if (SetProperty(ref _isStaged, value))
            {
                OnPropertyChanged(nameof(IsChecked));
            }
        }
    }

    /// <summary>Checkbox state: lit when complete or staged; completed rows never unstage.</summary>
    public bool IsChecked
    {
        get => IsComplete || IsStaged;
        set
        {
            if (!IsComplete)
            {
                IsStaged = value;
            }
        }
    }

    /// <summary>Row meta line: tree + prerequisite note.</summary>
    public string Meta => PrerequisiteCount == 0
        ? Tree
        : $"{Tree} · completes {PrerequisiteCount} prerequisite(s) with it";
}
