using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;

namespace SmartAlloc.ViewModels;

public partial class CalendarDayVM : ObservableObject
{
    public int Day { get; set; }
    public bool IsPadding { get; set; }
    public bool IsToday   { get; set; }
    public bool IsWeekend { get; set; }
    public List<RecurringTransaction> Items { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];
    public bool HasItems => Items.Count > 0 || Transactions.Count > 0;

    public string ChipText => Items.Count == 1
        ? $"{Items[0].CategoryName.Split(' ').Last()}"
        : Items.Count > 1 ? $"{Items.Count} payments" : "";
}

public partial class CalendarViewModel : BaseViewModel
{
    private readonly RecurringTransactionService _recurringService;
    private readonly SnackbarService _snackbar;
    private readonly TransactionService _txService;
    private readonly CategoryService _catService;
    private readonly CurrencyDisplayService _currencyDisplay;

    [ObservableProperty] private ObservableCollection<CalendarDayVM> _days = [];
    [ObservableProperty] private CalendarDayVM? _selectedDay;
    [ObservableProperty] private int _displayYear  = DateTime.Today.Year;
    [ObservableProperty] private int _displayMonth = DateTime.Today.Month;
    [ObservableProperty] private string _monthLabel = "";
    [ObservableProperty] private string _selectedDateLabel = "";

    public IReadOnlyList<string> DayHeaders { get; } = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    [ObservableProperty] private ObservableCollection<RecurringTransaction> _allItems = [];
    [ObservableProperty] private ObservableCollection<string> _categories = [];

    public List<int> ReminderOptions { get; } = [0, 1, 2, 3, 7];
    [ObservableProperty] private RecurringTransaction? _selectedItem;
    [ObservableProperty] private int _selectedReminderDays = 0;

    [ObservableProperty] private decimal _quickAmount;
    [ObservableProperty] private string  _quickCategory = "";
    [ObservableProperty] private string  _quickNote     = "";
    [ObservableProperty] private bool    _quickIsExpense = true;
    [ObservableProperty] private string  _quickColor    = "#6C63FF";

    [ObservableProperty] private decimal _newRecurringAmount;
    [ObservableProperty] private string  _newRecurringCategory = "";
    [ObservableProperty] private string  _newRecurringNote     = "";
    [ObservableProperty] private bool    _newRecurringIsExpense = true;
    [ObservableProperty] private int     _newRecurringDay      = 1;
    [ObservableProperty] private string  _newRecurringColor    = "#FF6B6B";

    public List<string> PresetColors { get; } =
        ["#6C63FF", "#FF6B6B", "#4ECDC4", "#F39C12", "#27AE60", "#E74C3C", "#45B7D1", "#DDA0DD"];

    public CalendarViewModel(RecurringTransactionService recurringService, SnackbarService snackbar,
                             TransactionService txService, CategoryService catService,
                             CurrencyDisplayService currencyDisplay)
    {
        _recurringService = recurringService;
        _snackbar         = snackbar;
        _txService        = txService;
        _catService       = catService;
        _currencyDisplay  = currencyDisplay;
    }

    private void LoadCategories()
    {
        Categories.Clear();
        foreach (var c in _catService.GetAll())
            Categories.Add(c.Name);
        QuickCategory = Categories.FirstOrDefault() ?? "";
    }

    [RelayCommand]
    public void Load()
    {
        LoadCategories();
        Refresh();
    }

    private void Refresh()
    {
        var first    = new DateTime(DisplayYear, DisplayMonth, 1);
        MonthLabel   = first.ToString("MMMM yyyy");

        var allItems = _recurringService.GetAll();
        AllItems.Clear();
        foreach (var r in allItems) AllItems.Add(r);

        var lookup = allItems
            .GroupBy(r => r.DayOfMonth)
            .ToDictionary(g => g.Key, g => g.ToList());

        var txLookup = _txService.GetByMonth(DisplayYear, DisplayMonth)
            .GroupBy(t => t.Date.Day)
            .ToDictionary(g => g.Key, g => g.ToList());

        Days.Clear();

        int startDow = (int)first.DayOfWeek;
        int padBefore = startDow == 0 ? 6 : startDow - 1;

        for (int i = 0; i < padBefore; i++)
            Days.Add(new CalendarDayVM { IsPadding = true });

        int daysInMonth = DateTime.DaysInMonth(DisplayYear, DisplayMonth);
        var today        = DateTime.Today;

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(DisplayYear, DisplayMonth, d);
            Days.Add(new CalendarDayVM
            {
                Day          = d,
                IsToday      = date == today,
                IsWeekend    = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                Items        = lookup.TryGetValue(d, out var rList) ? rList : [],
                Transactions = txLookup.TryGetValue(d, out var tList) ? tList : []
            });
        }

        int total    = Days.Count;
        int padAfter = (7 - total % 7) % 7;
        for (int i = 0; i < padAfter; i++)
            Days.Add(new CalendarDayVM { IsPadding = true });
    }


    [RelayCommand]
    private void QuickAddTransaction()
    {
        if (QuickAmount <= 0 || string.IsNullOrWhiteSpace(QuickCategory)
            || SelectedDay == null || SelectedDay.IsPadding) return;

        var date = new DateTime(DisplayYear, DisplayMonth, SelectedDay.Day);
        _txService.Add(new Transaction
        {
            Amount       = _currencyDisplay.ConvertToPln(QuickAmount),
            Date         = date,
            CategoryName = QuickCategory,
            Note         = QuickNote,
            Type         = QuickIsExpense ? TransactionType.Expense : TransactionType.Income
        });
        QuickAmount = 0;
        QuickNote   = "";
        _snackbar.ShowSuccess($"Transaction added for {date:d MMM}.");
        Refresh();
    }

    [RelayCommand]
    private void AddRecurringEntry()
    {
        if (NewRecurringAmount <= 0 || string.IsNullOrWhiteSpace(NewRecurringCategory)) return;
        if (NewRecurringDay < 1 || NewRecurringDay > 28)
        {
            _snackbar.ShowWarning("Day must be between 1 and 28.");
            return;
        }
        _recurringService.Add(new RecurringTransaction
        {
            Amount       = _currencyDisplay.ConvertToPln(NewRecurringAmount),
            CategoryName = NewRecurringCategory,
            Note         = NewRecurringNote,
            Type         = NewRecurringIsExpense ? TransactionType.Expense : TransactionType.Income,
            DayOfMonth   = NewRecurringDay,
            Color        = NewRecurringColor
        });
        NewRecurringAmount = 0;
        NewRecurringNote   = "";
        NewRecurringDay    = 1;
        _snackbar.ShowSuccess("Recurring payment added.");
        Refresh();
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        var d = new DateTime(DisplayYear, DisplayMonth, 1).AddMonths(-1);
        DisplayYear  = d.Year;
        DisplayMonth = d.Month;
        Refresh();
    }

    [RelayCommand]
    private void NextMonth()
    {
        var d = new DateTime(DisplayYear, DisplayMonth, 1).AddMonths(1);
        DisplayYear  = d.Year;
        DisplayMonth = d.Month;
        Refresh();
    }


    [RelayCommand]
    private void SaveReminder()
    {
        if (SelectedItem is null) return;
        _recurringService.SetReminder(SelectedItem.Id, SelectedReminderDays);
        SelectedItem.ReminderDaysBefore = SelectedReminderDays;
        _snackbar.ShowSuccess(SelectedReminderDays == 0
            ? "Reminder disabled."
            : $"Reminder set: {SelectedReminderDays} day(s) before payment.");
        Refresh();
    }


    partial void OnSelectedDayChanged(CalendarDayVM? value)
    {
        if (value != null && !value.IsPadding)
            SelectedDateLabel = new DateTime(DisplayYear, DisplayMonth, value.Day)
                                    .ToString("dddd, d MMMM yyyy");
        else
            SelectedDateLabel = "";

        if (value?.Items.Count > 0)
        {
            SelectedItem          = value.Items.First();
            SelectedReminderDays  = SelectedItem.ReminderDaysBefore;
        }
        else
        {
            SelectedItem         = null;
            SelectedReminderDays = 0;
        }
    }
}
