using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartAlloc.ViewModels;

public partial class TransactionsViewModel : BaseViewModel
{
    private readonly TransactionService _txService;
    private readonly CategoryService _catService;
    private readonly BudgetService _budgetService;
    private readonly RecurringTransactionService _recurringService;
    private readonly SnackbarService _snackbar;
    private readonly CurrencyDisplayService _currencyDisplay;

    [ObservableProperty] private ObservableCollection<Transaction> _transactions = [];
    [ObservableProperty] private ObservableCollection<string> _categories = [];
    [ObservableProperty] private Transaction? _selectedTransaction;

    [ObservableProperty] private decimal _newAmount;
    [ObservableProperty] private DateTime _newDate = DateTime.Today;
    [ObservableProperty] private string _newCategory = string.Empty;
    [ObservableProperty] private string _newNote = string.Empty;
    [ObservableProperty] private TransactionType _newType = TransactionType.Expense;
    [ObservableProperty] private bool _isExpense = true;
    [ObservableProperty] private bool _isIncome;

    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private string _filterCategory = "All";

    [ObservableProperty] private ObservableCollection<RecurringTransaction> _recurringItems = [];
    [ObservableProperty] private decimal _newRecurringAmount;
    [ObservableProperty] private string _newRecurringCategory = string.Empty;
    [ObservableProperty] private string _newRecurringNote = string.Empty;
    [ObservableProperty] private bool _newRecurringIsExpense = true;
    [ObservableProperty] private bool _newRecurringIsIncome;
    [ObservableProperty] private int _newRecurringDay = 1;

    public TransactionsViewModel(TransactionService txService, CategoryService catService,
                                  BudgetService budgetService, RecurringTransactionService recurringService,
                                  SnackbarService snackbar, CurrencyDisplayService currencyDisplay)
    {
        _txService = txService;
        _catService = catService;
        _budgetService = budgetService;
        _recurringService = recurringService;
        _snackbar = snackbar;
        _currencyDisplay = currencyDisplay;
    }

    [RelayCommand]
    public void Load()
    {
        var cats = _catService.GetAll();
        Categories.Clear();
        Categories.Add("All");
        foreach (var c in cats) Categories.Add(c.Name);
        var firstName = cats.FirstOrDefault()?.Name ?? "";
        if (!string.IsNullOrEmpty(firstName))
            NewCategory = firstName;
        NewRecurringCategory = firstName;
        RefreshTransactions();
        RefreshRecurring();
    }

    private void RefreshTransactions()
    {
        var all = _txService.GetAll();
        Transactions.Clear();
        foreach (var t in all
                     .Where(t =>
                         (FilterCategory == "All" || t.CategoryName == FilterCategory) &&
                         (string.IsNullOrWhiteSpace(FilterText) ||
                          t.Note.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                          t.CategoryName.Contains(FilterText, StringComparison.OrdinalIgnoreCase))))
            Transactions.Add(t);
    }

    [RelayCommand]
    private void AddTransaction()
    {
        if (NewAmount <= 0)
        {
            _snackbar.ShowWarning("Please enter an amount greater than 0.");
            return;
        }
        if (string.IsNullOrWhiteSpace(NewCategory))
        {
            _snackbar.ShowWarning("Please select a category.");
            return;
        }

        _txService.Add(new Transaction
        {
            Amount = _currencyDisplay.ConvertToPln(NewAmount),
            Date = NewDate,
            CategoryName = NewCategory,
            Note = NewNote,
            Type = IsIncome ? TransactionType.Income : TransactionType.Expense
        });

        var addedCategory = NewCategory;
        var addedIsExpense = IsExpense;
        NewAmount = 0;
        NewNote = string.Empty;
        NewDate = DateTime.Today;
        RefreshTransactions();
        if (addedIsExpense)
            CheckBudgetAlert(addedCategory);
    }

    private void CheckBudgetAlert(string categoryName)
    {
        var today = DateTime.Today;
        var budget = _budgetService.GetForCategory(today.Year, today.Month, categoryName);
        if (budget == null) return;
        var expenses = _txService.GetExpensesByCategory(today.Year, today.Month);
        expenses.TryGetValue(categoryName, out var spent);
        var pct = budget.MonthlyLimit > 0 ? (double)(spent / budget.MonthlyLimit * 100) : 0;
        var sym = _currencyDisplay.Symbol;
        var dispSpent = _currencyDisplay.Convert(spent);
        var dispLimit = _currencyDisplay.Convert(budget.MonthlyLimit);
        if (pct >= 100)
            _snackbar.ShowWarning(
                $"Budget exceeded for \"{categoryName}\"! Spent: {dispSpent:N2} / {dispLimit:N2} {sym}");
        else if (pct >= 90)
            _snackbar.ShowWarning(
                $"{pct:N0}% of budget used for \"{categoryName}\". Spent: {dispSpent:N2} / {dispLimit:N2} {sym}");
    }

    [RelayCommand]
    private void DeleteTransaction()
    {
        if (SelectedTransaction == null) return;
        var result = MessageBox.Show(
            $"Delete transaction: {SelectedTransaction.Amount:N2} PLN?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _txService.Delete(SelectedTransaction.Id);
            RefreshTransactions();
        }
    }

    partial void OnFilterTextChanged(string value) => RefreshTransactions();
    partial void OnFilterCategoryChanged(string value) => RefreshTransactions();

    [RelayCommand]
    private void AddRecurring()
    {
        if (NewRecurringAmount <= 0 || string.IsNullOrWhiteSpace(NewRecurringCategory)) return;
        if (NewRecurringDay < 1 || NewRecurringDay > 28)
        {
            _snackbar.ShowWarning("Day must be between 1 and 28.");
            return;
        }
        _recurringService.Add(new RecurringTransaction
        {
            Amount = _currencyDisplay.ConvertToPln(NewRecurringAmount),
            CategoryName = NewRecurringCategory,
            Note = NewRecurringNote,
            Type = NewRecurringIsIncome ? TransactionType.Income : TransactionType.Expense,
            DayOfMonth = NewRecurringDay
        });
        NewRecurringAmount = 0;
        NewRecurringNote = string.Empty;
        NewRecurringDay = 1;
        RefreshRecurring();
    }

    [RelayCommand]
    private void DeleteRecurring(RecurringTransaction item)
    {
        _recurringService.Delete(item.Id);
        RefreshRecurring();
    }

    private void RefreshRecurring()
    {
        RecurringItems.Clear();
        foreach (var r in _recurringService.GetAll())
            RecurringItems.Add(r);
    }
}
