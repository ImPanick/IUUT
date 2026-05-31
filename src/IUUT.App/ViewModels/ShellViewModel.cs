using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IUUT.App.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace IUUT.App.ViewModels;

/// <summary>
/// The navigation shell (docs/UI-DESIGN-CONCEPT.md §8): hosts one page view-model at a time and
/// swaps it on navigation. Page view-models are resolved lazily from DI by key (so a page can
/// depend on <see cref="INavigationService"/> = this shell without a construction cycle).
/// </summary>
public sealed class ShellViewModel : ObservableObject, INavigationService
{
    /// <summary>The Home page key.</summary>
    public const string HomeKey = "Home";

    /// <summary>The Broken Save Recovery page key.</summary>
    public const string RecoveryKey = "Recovery";

    /// <summary>The Custom editor page key.</summary>
    public const string CustomKey = "Custom";

    private readonly IServiceProvider _services;
    private readonly Stack<string> _back = new();

    private object? _currentPage;
    private string _currentKey = "";

    /// <summary>Creates the shell over the DI provider it resolves pages from.</summary>
    public ShellViewModel(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
        BackCommand = new RelayCommand(GoBack, () => CanGoBack);
        HomeCommand = new RelayCommand(GoHome);
    }

    /// <summary>The currently-displayed page view-model (bound by the window's content region).</summary>
    public object? CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    /// <inheritdoc />
    public bool CanGoBack => _back.Count > 0;

    /// <summary>Navigates to the previous page.</summary>
    public IRelayCommand BackCommand { get; }

    /// <summary>Navigates Home.</summary>
    public IRelayCommand HomeCommand { get; }

    /// <inheritdoc />
    public void NavigateTo(string pageKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(pageKey);
        if (_currentKey.Length > 0 && !string.Equals(_currentKey, pageKey, StringComparison.Ordinal))
        {
            _back.Push(_currentKey);
        }

        SetPage(pageKey);
    }

    /// <inheritdoc />
    public void GoHome()
    {
        _back.Clear();
        SetPage(HomeKey);
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (_back.Count == 0)
        {
            return;
        }

        SetPage(_back.Pop());
    }

    private void SetPage(string key)
    {
        _currentKey = key;
        CurrentPage = Resolve(key);
        OnPropertyChanged(nameof(CanGoBack));
        BackCommand.NotifyCanExecuteChanged();
    }

    private object Resolve(string key) => key switch
    {
        HomeKey => _services.GetRequiredService<HomeViewModel>(),
        RecoveryKey => _services.GetRequiredService<RecoveryViewModel>(),
        CustomKey => _services.GetRequiredService<CustomViewModel>(),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown page key."),
    };
}
