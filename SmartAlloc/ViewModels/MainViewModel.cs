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
    private readonly DashboardViewModel    _dashboardVM;
    private readonly TransactionsViewModel _transactionsVM;
    private readonly BudgetsViewModel      _budgetsVM;
    private readonly GoalsViewModel        _goalsVM;
    private readonly StatisticsViewModel   _statisticsVM;
    private readonly SettingsViewModel     _settingsVM;
    private readonly CalendarViewModel     _calendarVM;
    private readonly TransactionService    _txService;
    private readonly ReportService         _reportService;
    private readonly ThemeService          _themeService;
    private readonly SnackbarService       _snackbar;
    private readonly CurrentUserService    _currentUser;
    private readonly PrivacyService        _privacyService;
    private readonly CurrencyDisplayService _currencyDisplay;

    [ObservableProperty] private BaseViewModel _currentView;
    [ObservableProperty] private string _activeTab = "dashboard";
    [ObservableProperty] private string _themeIcon = "☀️";
    [ObservableProperty] private string _themeLabel = "Light mode";
    [ObservableProperty] private string _currentUserName = string.Empty;
    [ObservableProperty] private ImageSource? _currentUserAvatar;

    [ObservableProperty] private string _navDashboard = "Dashboard";
    [ObservableProperty] private string _navTransactions = "Transactions";
    [ObservableProperty] private string _navBudgets = "Budgets";
    [ObservableProperty] private string _navGoals = "Goals";
    [ObservableProperty] private string _navStatistics = "Statistics";
    [ObservableProperty] private string _navCalendar = "Calendar";
    [ObservableProperty] private string _navPdfReport = "PDF Report";
    [ObservableProperty] private string _navSettings = "Settings";
    [ObservableProperty] private string _navLogOut = "Log Out";
    [ObservableProperty] private string _navMenu = "MENU";

    public PrivacyService Privacy => _privacyService;

    public CurrencyDisplayService CurrencyDisplay => _currencyDisplay;

    public DashboardViewModel    DashboardVM    => _dashboardVM;
    public TransactionsViewModel TransactionsVM => _transactionsVM;
    public BudgetsViewModel      BudgetsVM      => _budgetsVM;
    public GoalsViewModel        GoalsVM        => _goalsVM;
    public StatisticsViewModel   StatisticsVM   => _statisticsVM;
    public SettingsViewModel     SettingsVM     => _settingsVM;
    public CalendarViewModel     CalendarVM     => _calendarVM;

    public event Action? LogoutRequested;

    public MainViewModel(
        DashboardViewModel    dashboardVM,
        TransactionsViewModel transactionsVM,
        BudgetsViewModel      budgetsVM,
        GoalsViewModel        goalsVM,
        StatisticsViewModel   statisticsVM,
        SettingsViewModel     settingsVM,
        CalendarViewModel     calendarVM,
        TransactionService    txService,
        ReportService         reportService,
        ThemeService          themeService,
        SnackbarService       snackbar,
        CurrentUserService    currentUser,
        PrivacyService        privacyService,
        CurrencyDisplayService currencyDisplay)
    {
        _dashboardVM    = dashboardVM;
        _transactionsVM = transactionsVM;
        _budgetsVM      = budgetsVM;
        _goalsVM        = goalsVM;
        _statisticsVM   = statisticsVM;
        _settingsVM     = settingsVM;
        _calendarVM     = calendarVM;
        _txService      = txService;
        _reportService  = reportService;
        _themeService   = themeService;
        _snackbar       = snackbar;
        _currentUser    = currentUser;
        _privacyService = privacyService;
        _currencyDisplay = currencyDisplay;
        _currentView    = dashboardVM;

        _settingsVM.LogoutRequested += () => LogoutRequested?.Invoke();
        LocalizationService.Current.LanguageChanged += RefreshLocalizedStrings;
        RefreshLocalizedStrings();
    }

    private void RefreshLocalizedStrings()
    {
        var l = LocalizationService.Current;
        NavDashboard    = l.Get("Nav.Dashboard");
        NavTransactions = l.Get("Nav.Transactions");
        NavBudgets      = l.Get("Nav.Budgets");
        NavGoals        = l.Get("Nav.Goals");
        NavStatistics   = l.Get("Nav.Statistics");
        NavCalendar     = l.Get("Nav.Calendar");
        NavPdfReport    = l.Get("Nav.PdfReport");
        NavSettings     = l.Get("Nav.Settings");
        NavLogOut       = l.Get("Nav.LogOut");
        NavMenu         = l.Get("Nav.Menu");
        ThemeLabel      = _themeService.IsDark ? l.Get("Theme.Light") : l.Get("Theme.Dark");
    }

    public async Task InitializeAsync()
    {
        CurrentUserName   = _currentUser.DisplayName;
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
        ActiveTab   = "dashboard";
        CurrentView = _dashboardVM;
        await _dashboardVM.LoadAsync();
    }

    [RelayCommand]
    private void NavigateToTransactions()
    {
        ActiveTab   = "transactions";
        CurrentView = _transactionsVM;
        _transactionsVM.Load();
    }

    [RelayCommand]
    private void NavigateToBudgets()
    {
        ActiveTab   = "budgets";
        CurrentView = _budgetsVM;
        _budgetsVM.Load();
    }

    [RelayCommand]
    private void NavigateToGoals()
    {
        ActiveTab   = "goals";
        CurrentView = _goalsVM;
        _goalsVM.Load();
    }

    [RelayCommand]
    private void NavigateToStatistics()
    {
        ActiveTab   = "statistics";
        CurrentView = _statisticsVM;
        _statisticsVM.Load();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ActiveTab   = "settings";
        CurrentView = _settingsVM;
        _settingsVM.Load();
    }

    [RelayCommand]
    private void NavigateToCalendar()
    {
        ActiveTab   = "calendar";
        CurrentView = _calendarVM;
        _calendarVM.Load();
    }

    [RelayCommand]
    private void TogglePrivacy() => _privacyService.Toggle();

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
        var l = LocalizationService.Current;
        ThemeIcon  = _themeService.IsDark ? "☀️" : "🌙";
        ThemeLabel = _themeService.IsDark ? l.Get("Theme.Light") : l.Get("Theme.Dark");
    }

    [RelayCommand]
    private void GenerateReport()
    {
        SmartAlloc.Views.MainWindow.SetBlur(true);
        var dialog = new SmartAlloc.Views.CurrencyPickerDialog(_currencyDisplay.SelectedCurrency)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true)
        {
            SmartAlloc.Views.MainWindow.SetBlur(false);
            return;
        }
        SmartAlloc.Views.MainWindow.SetBlur(false);

        var currency     = dialog.SelectedCurrency;
        var today        = DateTime.Today;
        var transactions = _txService.GetByMonth(today.Year, today.Month);
        var expenses     = _txService.GetExpensesByCategory(today.Year, today.Month);
        var income       = _txService.GetTotalIncome();
        var expense      = _txService.GetTotalExpense();
        var balance      = _txService.GetBalance();

        try
        {
            var path = _reportService.GenerateMonthlyReport(
                today.Year, today.Month, transactions, expenses, income, expense, balance,
                currency, v => _currencyDisplay.ConvertTo(v, currency));
            _snackbar.ShowSuccess($"PDF report ({currency}) generated successfully.");
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _snackbar.ShowError($"Error generating report: {ex.Message}");
        }
    }
}
