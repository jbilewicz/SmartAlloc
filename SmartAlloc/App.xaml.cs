using Microsoft.Extensions.DependencyInjection;
using SmartAlloc.Data;
using SmartAlloc.Services;
using SmartAlloc.ViewModels;
using SmartAlloc.Views;
using System.Windows;
using System.Windows.Threading;

namespace SmartAlloc;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, args) =>
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"),
                $"{DateTime.Now}\n{args.Exception}\n{args.Exception.StackTrace}");
            MessageBox.Show(args.Exception.ToString(), "SmartAlloc Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        var services = new ServiceCollection();

        services.AddSingleton<DatabaseContext>();
        services.AddSingleton<TransactionService>();
        services.AddSingleton<BudgetService>();
        services.AddSingleton<CategoryService>();
        services.AddSingleton<GoalService>();
        services.AddSingleton<CurrencyService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<ThemeService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<TransactionsViewModel>();
        services.AddSingleton<BudgetsViewModel>();
        services.AddSingleton<GoalsViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();

        var mainVM = Services.GetRequiredService<MainViewModel>();
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = mainVM;
        mainWindow.Show();

        await mainVM.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
        base.OnExit(e);
    }
}
