using CommunityToolkit.Mvvm.ComponentModel;
using IUUT.Core.Editing;
using IUUT.Core.Models;

namespace IUUT.App.ViewModels;

/// <summary>
/// One tamed mount in the Mounts editor (master §8.10). Holds the editable denormalized JSON fields
/// (name, level) seeded from the model; the authoritative <c>RecorderBlob</c> is never touched.
/// <see cref="WriteTo"/> reconciles the edits through <see cref="MountEditService"/> at apply time.
/// </summary>
public sealed class MountSlotViewModel : ObservableObject
{
    private string _name;
    private int _level;

    /// <summary>Wraps a mount model for editing.</summary>
    public MountSlotViewModel(Mount model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        _name = model.MountName;
        _level = model.MountLevel;
        MountType = string.IsNullOrEmpty(model.MountType) ? "—" : model.MountType;
    }

    /// <summary>The underlying model this wrapper edits (reconciled on apply).</summary>
    public Mount Model { get; }

    /// <summary>The mount type (read-only display).</summary>
    public string MountType { get; }

    /// <summary>Editable display name.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    /// <summary>Editable denormalized level.</summary>
    public int Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    /// <summary>Label for the mount selector.</summary>
    public string DisplayLabel => $"{(string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name)}  ·  {MountType}";

    /// <summary>Reconciles the edits into the model through the edit service (called at apply time).</summary>
    public void WriteTo(MountEditService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (!string.IsNullOrWhiteSpace(Name))
        {
            service.SetName(Model, Name);
        }

        service.SetLevel(Model, Level);
    }
}
