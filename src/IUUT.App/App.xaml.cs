using System.Windows;
using IUUT.App.Navigation;
using IUUT.App.ViewModels;
using IUUT.Core.Abstractions;
using IUUT.Core.Catalog;
using IUUT.Core.Io;
using IUUT.Core.Recovery;
using IUUT.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IUUT.App;

/// <summary>
/// IUUT desktop application entry point. Builds the dependency-injection container
/// (CODE_STYLE §3) and shows the composed <see cref="MainWindow"/>. All business logic
/// lives in <c>IUUT.Core</c>; this layer is the thin WPF shell (master doc §9.1).
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services = ConfigureServices();
        _services.GetRequiredService<MainWindow>().Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // --- Foundation -------------------------------------------------------
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IGuidProvider, SystemGuidProvider>();
        services.AddSingleton(_ => AppPaths.Resolve(
            AppContext.BaseDirectory,
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));

        // --- Catalogs + headline feature + apply pipeline (master §13.3) ------
        services.AddSingleton(_ => GameCatalogs.LoadEmbedded());
        services.AddSingleton<LazyMaxService>();
        services.AddSingleton<BackupManager>();
        services.AddSingleton<ISafeSaveWriter, SafeSaveWriter>();
        services.AddSingleton<LazyMaxApplyService>();

        // --- Home screen dependencies (master doc §10.2) ----------------------
        services.AddSingleton<IRunningProcesses, SystemRunningProcesses>();
        services.AddSingleton<GameProcessDetector>();
        services.AddSingleton<SaveDiscoveryService>();
        services.AddSingleton<ILocalSteamNames, VdfLocalSteamNames>();
        services.AddSingleton<SteamProfileCache>();
        services.AddSingleton(sp => new SteamProfileResolverService(
            sp.GetRequiredService<SteamProfileCache>(),
            sp.GetRequiredService<ILocalSteamNames>(),
            sp.GetRequiredService<IClock>()));
        services.AddSingleton<HomeService>();

        // --- Broken Save Recovery (master §11.3, §12.1) -----------------------
        services.AddSingleton<HealthScanService>();
        services.AddSingleton<BackupChainWalker>();
        services.AddSingleton<TemplateRepairService>();
        services.AddSingleton<RecoveryPlanner>();
        services.AddSingleton<RecoveryAdvisor>();
        services.AddSingleton<RecoveryService>();

        // --- UI shell + pages -------------------------------------------------
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<ShellViewModel>());
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<RecoveryViewModel>();
        services.AddSingleton<MainWindow>();

        // ValidateOnBuild constructs every registration at startup, so a broken DI graph
        // (e.g. a page view-model's service) fails fast here rather than on first navigation.
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
