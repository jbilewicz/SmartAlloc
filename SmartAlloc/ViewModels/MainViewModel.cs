using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Services;
using SmartAlloc.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartAlloc.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly DashboardViewModel _dashboardVM;
    private readonly TransactionsViewModel _transactionsVM;
    private readonly BudgetsViewModel _budgetsVM;
    private readonly GoalsViewModel _goalsVM;
    private readonly TransactionService _txService;
    private readonly ReportService _reportService;
    private readonly ThemeService _themeService;

    [ObservableProperty] private BaseViewModel _currentView;
    [ObservableProperty] private string _activeTab = "dashboard";
    [ObservableProperty] private string _themeIcon = "☀️";
    [ObservableProperty] private string _themeLabel = "Light mode";

    public DashboardViewModel DashboardVM => _dashboardVM;
    public TransactionsViewModel TransactionsVM => _transactionsVM;
    public BudgetsViewModel BudgetsVM => _budgetsVM;
    public GoalsViewModel GoalsVM => _goalsVM;

    public MainViewModel(
        DashboardViewModel dashboardVM,
        TransactionsViewModel transactionsVM,
        BudgetsViewModel budgetsVM,
        GoalsViewModel goalsVM,
        TransactionService txService,
        ReportService reportService,
        ThemeService themeService)
    {
        _dashboardVM = dashboardVM;
        _transactionsVM = transactionsVM;
        _budgetsVM = budgetsVM;
        _goalsVM = goalsVM;
        _txService = txService;
        _reportService = reportService;
        _themeService = themeService;
        _currentView = dashboardVM;
    }

    public async Task InitializeAsync()
    {
        _transactionsVM.Load();
        _budgetsVM.Load();
        _goalsVM.Load();
        await _dashboardVM.LoadAsync();
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
            MessageBox.Show($"PDF report generated:\n{path}",
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
