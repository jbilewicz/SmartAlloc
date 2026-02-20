using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;

namespace SmartAlloc.ViewModels;

public class BudgetItem : ObservableObject
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public decimal Spent { get; set; }
    public double UsagePercent => MonthlyLimit > 0
        ? Math.Min(100, (double)(Spent / MonthlyLimit * 100)) : 0;
    public decimal Remaining => Math.Max(0, MonthlyLimit - Spent);
    public string StatusColor => UsagePercent >= 90 ? "#E74C3C"
                               : UsagePercent >= 70 ? "#F39C12"
                               : "#27AE60";
    public int BudgetId { get; set; }
}

public partial class BudgetsViewModel : BaseViewModel
{
    private readonly BudgetService _budgetService;
    private readonly TransactionService _txService;
    private readonly CategoryService _catService;

    [ObservableProperty] private ObservableCollection<BudgetItem> _budgetItems = [];
    [ObservableProperty] private ObservableCollection<string> _categories = [];
    [ObservableProperty] private string _selectedCategory = string.Empty;
    [ObservableProperty] private decimal _newLimit;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;

    public BudgetsViewModel(BudgetService budgetService, TransactionService txService, CategoryService catService)
    {
        _budgetService = budgetService;
        _txService = txService;
        _catService = catService;
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
        var expenses = _txService.GetExpensesByCategory(SelectedYear, SelectedMonth);
        BudgetItems.Clear();
        foreach (var b in budgets)
        {
            expenses.TryGetValue(b.CategoryName, out var spent);
            BudgetItems.Add(new BudgetItem
            {
                BudgetId = b.Id,
                CategoryName = b.CategoryName,
                MonthlyLimit = b.MonthlyLimit,
                Spent = spent
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
            MonthlyLimit = NewLimit,
            Month = SelectedMonth,
            Year = SelectedYear
        });
        NewLimit = 0;
        RefreshBudgets();
    }

    [RelayCommand]
    private void DeleteBudget(BudgetItem item)
    {
        _budgetService.Delete(item.BudgetId);
        RefreshBudgets();
    }

    partial void OnSelectedMonthChanged(int value) => RefreshBudgets();
    partial void OnSelectedYearChanged(int value) => RefreshBudgets();
}
