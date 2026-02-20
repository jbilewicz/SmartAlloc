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

    public TransactionsViewModel(TransactionService txService, CategoryService catService)
    {
        _txService = txService;
        _catService = catService;
    }

    [RelayCommand]
    public void Load()
    {
        var cats = _catService.GetAll();
        Categories.Clear();
        Categories.Add("All");
        foreach (var c in cats) Categories.Add(c.Name);
        if (!string.IsNullOrEmpty(Categories.FirstOrDefault()))
            NewCategory = cats.FirstOrDefault()?.Name ?? "";
        RefreshTransactions();
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
            MessageBox.Show("Please enter an amount greater than 0.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(NewCategory))
        {
            MessageBox.Show("Please select a category.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _txService.Add(new Transaction
        {
            Amount = NewAmount,
            Date = NewDate,
            CategoryName = NewCategory,
            Note = NewNote,
            Type = IsIncome ? TransactionType.Income : TransactionType.Expense
        });

        NewAmount = 0;
        NewNote = string.Empty;
        NewDate = DateTime.Today;
        RefreshTransactions();
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
}
