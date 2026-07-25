using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>One account unlock flag in the Account Flags checklist (id + friendly label + checkbox).</summary>
public sealed class AccountFlagRowViewModel : ObservableObject
{
    private bool _isEnabled;

    /// <summary>Creates a row from the service's flag state.</summary>
    public AccountFlagRowViewModel(int id, string label, string? rowName, bool isEnabled)
    {
        Id = id;
        Label = label;
        RowName = rowName ?? $"(beyond catalog — id {id})";
        _isEnabled = isEnabled;
    }

    /// <summary>The flag id (the value stored in <c>Profile.json</c> <c>UnlockedFlags</c>).</summary>
    public int Id { get; }

    /// <summary>Friendly label from the catalog.</summary>
    public string Label { get; }

    /// <summary>The <c>D_AccountFlags</c> RowName (or a placeholder for ids beyond the catalog).</summary>
    public string RowName { get; }

    /// <summary>Whether the profile has this flag set (checkbox, staged until Apply).</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}
