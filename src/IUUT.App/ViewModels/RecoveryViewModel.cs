using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>
/// The Broken Save Recovery page view-model. Stub for the navigation-shell step; the next step
/// wires it to <c>HealthScanService</c> / <c>RecoveryPlanner</c> / <c>RecoveryService</c>.
/// </summary>
public sealed class RecoveryViewModel : ObservableObject
{
    /// <summary>Status / placeholder text.</summary>
    public string StatusMessage { get; } =
        "Recovery is fully built in Core (scan → plan → master-backup → restore/template → report). " +
        "This screen is being wired to it now.";
}
