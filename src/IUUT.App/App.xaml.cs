using System.Windows;
using IUUT.App.ViewModels;
using IUUT.Core.Abstractions;
using IUUT.Core.Catalog;
using IUUT.Core.Io;
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

        // --- UI ---------------------------------------------------------------
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
