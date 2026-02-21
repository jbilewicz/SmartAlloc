using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;

namespace SmartAlloc.ViewModels;

public class BudgetItem : ObservableObject
{
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(Description)
        ? CategoryName : $"{CategoryName} – {Description}";
    public decimal MonthlyLimit { get; set; }
    public decimal Deposited { get; set; }
    public double UsagePercent => MonthlyLimit > 0
        ? Math.Min(100, (double)(Deposited / MonthlyLimit * 100)) : 0;
    public decimal Remaining => Math.Max(0, MonthlyLimit - Deposited);
    public string StatusColor => UsagePercent >= 90 ? "#E74C3C"
                               : UsagePercent >= 70 ? "#F39C12"
                               : "#27AE60";
    public int BudgetId { get; set; }
}

public partial class BudgetsViewModel : BaseViewModel
{
    private readonly BudgetService          _budgetService;
    private readonly TransactionService     _txService;
    private readonly CategoryService        _catService;
    private readonly CurrencyDisplayService _currencyDisplay;

    [ObservableProperty] private ObservableCollection<BudgetItem> _budgetItems = [];
    [ObservableProperty] private ObservableCollection<string> _categories = [];
    [ObservableProperty] private string _selectedCategory = string.Empty;
    [ObservableProperty] private decimal _newLimit;
    [ObservableProperty] private string _newDescription = string.Empty;

    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;

    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;

    public string MonthLabel =>
        new DateTime(SelectedYear, SelectedMonth, 1)
            .ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

    public BudgetsViewModel(BudgetService budgetService, TransactionService txService,
                             CategoryService catService, CurrencyDisplayService currencyDisplay)
    {
        _budgetService   = budgetService;
        _txService       = txService;
        _catService      = catService;
        _currencyDisplay = currencyDisplay;
        _currencyDisplay.DisplayCurrencyChanged += RefreshBudgets;
    }

    [RelayCommand]
    public void Load()
    {
        var cats = _catService.GetAll();
        Categories.Clear();
        foreach (var c in cats) Categories.Add(c.Name);
        SelectedCategory = Categories.FirstOrDefault() ?? "";
        RefreshBudgets();
    }

    private void RefreshBudgets()
    {
        var budgets = _budgetService.GetByMonth(SelectedYear, SelectedMonth);
        BudgetItems.Clear();
        foreach (var b in budgets)
        {
            BudgetItems.Add(new BudgetItem
            {
                BudgetId = b.Id,
                CategoryName = b.CategoryName,
                Description = b.Description,
                MonthlyLimit = b.MonthlyLimit,
                Deposited = b.Deposited
            });
        }
    }

    [RelayCommand]
    private void AddOrUpdateBudget()
    {
        if (string.IsNullOrWhiteSpace(SelectedCategory) || NewLimit <= 0) return;
        _budgetService.Upsert(new Budget
        {
            CategoryName = SelectedCategory,
            MonthlyLimit = _currencyDisplay.ConvertToPln(NewLimit),
            Description  = NewDescription,
            Month = SelectedMonth,
            Year = SelectedYear
        });
        NewLimit = 0;
        NewDescription = string.Empty;
        RefreshBudgets();
    }

    [RelayCommand]
    private void DeleteBudget(BudgetItem item)
    {
        _budgetService.Delete(item.BudgetId);
        RefreshBudgets();
    }

    [RelayCommand]
    private void DepositToBudget(BudgetItem item)
    {
        if (item == null) return;
        var remaining = _currencyDisplay.Convert(item.Remaining);
        if (remaining <= 0) return;
        SmartAlloc.Views.MainWindow.SetBlur(true);
        var dialog = new SmartAlloc.Views.BudgetDepositDialog(item.DisplayName, remaining, _currencyDisplay.Symbol)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.DepositAmount <= 0)
        {
            SmartAlloc.Views.MainWindow.SetBlur(false);
            return;
        }
        SmartAlloc.Views.MainWindow.SetBlur(false);
        var plnAmount = _currencyDisplay.ConvertToPln(dialog.DepositAmount);
        _budgetService.AddDeposit(item.BudgetId, plnAmount);
        RefreshBudgets();
    }

    private bool _suppressRefresh;
    partial void OnSelectedMonthChanged(int value) { if (!_suppressRefresh) RefreshBudgets(); }
    partial void OnSelectedYearChanged(int value) { if (!_suppressRefresh) RefreshBudgets(); }

    [RelayCommand]
    private void PreviousMonth()
    {
        if (SelectedMonth == 1) { _suppressRefresh = true; SelectedMonth = 12; _suppressRefresh = false; SelectedYear--; }
        else SelectedMonth--;
    }

    [RelayCommand]
    private void NextMonth()
    {
        if (SelectedMonth == 12) { _suppressRefresh = true; SelectedMonth = 1; _suppressRefresh = false; SelectedYear++; }
        else SelectedMonth++;
    }
}
