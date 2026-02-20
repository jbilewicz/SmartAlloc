using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Services;
using SmartAlloc.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartAlloc.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly DashboardViewModel _dashboardVM;
    private readonly TransactionsViewModel _transactionsVM;
    private readonly BudgetsViewModel _budgetsVM;
    private readonly GoalsViewModel _goalsVM;
    private readonly StatisticsViewModel _statisticsVM;
    private readonly SettingsViewModel _settingsVM;
    private readonly TransactionService _txService;
    private readonly ReportService _reportService;
    private readonly ThemeService _themeService;
    private readonly SnackbarService _snackbar;
    private readonly CurrentUserService _currentUser;

    [ObservableProperty] private BaseViewModel _currentView;
    [ObservableProperty] private string _activeTab = "dashboard";
    [ObservableProperty] private string _themeIcon = "☀️";
    [ObservableProperty] private string _themeLabel = "Light mode";
    [ObservableProperty] private string _currentUserName = string.Empty;
    [ObservableProperty] private ImageSource? _currentUserAvatar;

    public DashboardViewModel DashboardVM => _dashboardVM;
    public TransactionsViewModel TransactionsVM => _transactionsVM;
    public BudgetsViewModel BudgetsVM => _budgetsVM;
    public GoalsViewModel GoalsVM => _goalsVM;
    public StatisticsViewModel StatisticsVM => _statisticsVM;
    public SettingsViewModel SettingsVM => _settingsVM;

    public event Action? LogoutRequested;

    public MainViewModel(
        DashboardViewModel dashboardVM,
        TransactionsViewModel transactionsVM,
        BudgetsViewModel budgetsVM,
        GoalsViewModel goalsVM,
        StatisticsViewModel statisticsVM,
        SettingsViewModel settingsVM,
        TransactionService txService,
        ReportService reportService,
        ThemeService themeService,
        SnackbarService snackbar,
        CurrentUserService currentUser)
    {
        _dashboardVM = dashboardVM;
        _transactionsVM = transactionsVM;
        _budgetsVM = budgetsVM;
        _goalsVM = goalsVM;
        _statisticsVM = statisticsVM;
        _settingsVM = settingsVM;
        _txService = txService;
        _reportService = reportService;
        _themeService = themeService;
        _snackbar = snackbar;
        _currentUser = currentUser;
        _currentView = dashboardVM;

        _settingsVM.LogoutRequested += () => LogoutRequested?.Invoke();
    }

    public async Task InitializeAsync()
    {
        CurrentUserName = _currentUser.DisplayName;
        CurrentUserAvatar = LoadAvatar(_currentUser.AvatarPath);

        _transactionsVM.Load();
        _budgetsVM.Load();
        _goalsVM.Load();
        _settingsVM.Load();
        await _dashboardVM.LoadAsync();
    }

    private static ImageSource? LoadAvatar(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path, UriKind.Absolute);
            bi.DecodePixelWidth = 120;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    [RelayCommand]
    private async Task NavigateToDashboard()
    {
        ActiveTab = "dashboard";
        CurrentView = _dashboardVM;
        await _dashboardVM.LoadAsync();
    }

    [RelayCommand]
    private void NavigateToTransactions()
    {
        ActiveTab = "transactions";
        CurrentView = _transactionsVM;
        _transactionsVM.Load();
    }

    [RelayCommand]
    private void NavigateToBudgets()
    {
        ActiveTab = "budgets";
        CurrentView = _budgetsVM;
        _budgetsVM.Load();
    }

    [RelayCommand]
    private void NavigateToGoals()
    {
        ActiveTab = "goals";
        CurrentView = _goalsVM;
        _goalsVM.Load();
    }

    [RelayCommand]
    private void NavigateToStatistics()
    {
        ActiveTab = "statistics";
        CurrentView = _statisticsVM;
        _statisticsVM.Load();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ActiveTab = "settings";
        CurrentView = _settingsVM;
        _settingsVM.Load();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        ThemeIcon = _themeService.IsDark ? "☀️" : "🌙";
        ThemeLabel = _themeService.IsDark ? "Light mode" : "Dark mode";
    }

    [RelayCommand]
    private void GenerateReport()
    {
        var today = DateTime.Today;
        var transactions = _txService.GetByMonth(today.Year, today.Month);
        var expenses = _txService.GetExpensesByCategory(today.Year, today.Month);
        var income = _txService.GetTotalIncome();
        var expense = _txService.GetTotalExpense();
        var balance = _txService.GetBalance();

        try
        {
            var path = _reportService.GenerateMonthlyReport(
                today.Year, today.Month, transactions, expenses, income, expense, balance);
            _snackbar.ShowSuccess($"PDF report generated successfully.");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _snackbar.ShowError($"Error generating report: {ex.Message}");
        }
    }
}
