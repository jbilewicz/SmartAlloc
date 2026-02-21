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
        services.AddSingleton<CurrencyDisplayService>();
        services.AddSingleton<PrivacyService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<RecurringTransactionService>();
        services.AddSingleton<ReminderService>();
        services.AddSingleton<SnackbarService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<CurrentUserService>();
        services.AddSingleton<BackupService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<TransactionsViewModel>();
        services.AddSingleton<BudgetsViewModel>();
        services.AddSingleton<GoalsViewModel>();
        services.AddSingleton<StatisticsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<CalendarViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();

        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();

        var privacySvc   = Services.GetRequiredService<PrivacyService>();
        var currencyDisp = Services.GetRequiredService<CurrencyDisplayService>();
        try { await currencyDisp.InitializeAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Currency init failed: {ex.Message}");
        }

        var db = Services.GetRequiredService<DatabaseContext>();
        LocalizationService.Current.LoadSaved(db);

        var mainVM = Services.GetRequiredService<MainViewModel>();
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = mainVM;
        mainWindow.Initialize(mainVM);

        var snackbar = Services.GetRequiredService<SnackbarService>();
        mainWindow.SnackbarElement.MessageQueue = snackbar.MessageQueue;

        var loginVM = Services.GetRequiredService<LoginViewModel>();
        mainWindow.ShowLoginOverlay(loginVM);

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
        base.OnExit(e);
    }
}
