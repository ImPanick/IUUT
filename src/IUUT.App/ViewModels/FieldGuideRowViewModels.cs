using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>One tracked statistic in the Field Guide (editable value).</summary>
public sealed class TrackedStatRowViewModel : ObservableObject
{
    private long _value;

    /// <summary>Creates the row.</summary>
    public TrackedStatRowViewModel(string rowName, string label, string category, long value)
    {
        RowName = rowName;
        Label = label;
        Category = category;
        _value = value;
    }

    /// <summary>The <c>D_PlayerTrackers</c> row name.</summary>
    public string RowName { get; }

    /// <summary>The in-game display name (falls back to a humanized row name).</summary>
    public string Label { get; }

    /// <summary>The tracker category, or "" when the game gives none.</summary>
    public string Category { get; }

    /// <summary>Row meta line: category + raw row name.</summary>
    public string Meta => Category.Length == 0 ? RowName : $"{Category} · {RowName}";

    /// <summary>The counter value (staged until Apply).</summary>
    public long Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

/// <summary>One fish in the Field Guide — catalog-listed even when never caught.</summary>
public sealed class FishRowViewModel : ObservableObject
{
    private long _caught;
    private long _maxQuality;
    private long _maxWeight;
    private long _maxLength;

    /// <summary>Creates the row.</summary>
    public FishRowViewModel(string rowName, string label, string? lore, long caught, long maxQuality, long maxWeight, long maxLength)
    {
        RowName = rowName;
        Label = label;
        Lore = lore;
        _caught = caught;
        _maxQuality = maxQuality;
        _maxWeight = maxWeight;
        _maxLength = maxLength;
    }

    /// <summary>The <c>D_FishData</c> row name.</summary>
    public string RowName { get; }

    /// <summary>Humanized row name (the game ships no display name for fish).</summary>
    public string Label { get; }

    /// <summary>The field-guide lore text, when the game has one.</summary>
    public string? Lore { get; }

    /// <summary>How many have been caught (staged until Apply).</summary>
    public long Caught
    {
        get => _caught;
        set
        {
            if (SetProperty(ref _caught, value))
            {
                OnPropertyChanged(nameof(IsCaught));
            }
        }
    }

    /// <summary>Best quality caught.</summary>
    public long MaxQuality
    {
        get => _maxQuality;
        set => SetProperty(ref _maxQuality, value);
    }

    /// <summary>Best weight caught.</summary>
    public long MaxWeight
    {
        get => _maxWeight;
        set => SetProperty(ref _maxWeight, value);
    }

    /// <summary>Best length caught.</summary>
    public long MaxLength
    {
        get => _maxLength;
        set => SetProperty(ref _maxLength, value);
    }

    /// <summary>Whether this fish has ever been caught (drives the field-guide tick).</summary>
    public bool IsCaught => Caught > 0;
}

/// <summary>One completed task inside a checklist (unticking stages its removal).</summary>
public sealed class TaskRowViewModel : ObservableObject
{
    private bool _isCompleted = true;

    /// <summary>Creates the row.</summary>
    public TaskRowViewModel(string listRowName, string task)
    {
        ListRowName = listRowName;
        Task = task;
    }

    /// <summary>The owning checklist's row name.</summary>
    public string ListRowName { get; }

    /// <summary>The task name as the game records it.</summary>
    public string Task { get; }

    /// <summary>Whether the task counts as completed (staged until Apply).</summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }
}

/// <summary>One task-list checklist and its recorded tasks.</summary>
public sealed class TaskListRowViewModel
{
    /// <summary>Creates the checklist.</summary>
    public TaskListRowViewModel(string rowName, string label, IReadOnlyList<TaskRowViewModel> tasks)
    {
        RowName = rowName;
        Label = label;
        Tasks = tasks;
    }

    /// <summary>The checklist's row name.</summary>
    public string RowName { get; }

    /// <summary>Humanized checklist name.</summary>
    public string Label { get; }

    /// <summary>The tasks the save records as completed.</summary>
    public IReadOnlyList<TaskRowViewModel> Tasks { get; }

    /// <summary>Header line.</summary>
    public string Header => $"{Label}   ({Tasks.Count(t => t.IsCompleted)}/{Tasks.Count})";
}
