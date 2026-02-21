using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartAlloc.ViewModels;

public partial class GoalItemVM : ObservableObject
{
    public Goal Goal { get; set; } = null!;

    public bool   HasPrediction         { get; set; }
    public string PredictedDateText     { get; set; } = "";
    public int    MonthsWithSavings     { get; set; }
    public string InsufficientDataTooltip =>
        $"{LocalizationService.Current.Get("Goal.InsufficientTip1")}\n" +
        $"{string.Format(LocalizationService.Current.Get("Goal.InsufficientTip2"), MonthsWithSavings)}\n\n" +
        $"{LocalizationService.Current.Get("Goal.InsufficientTip3")}";

    public string ProgressText
    {
        get
        {
            if (PrivacyService.Current.IsPrivate)
                return $"{LocalizationService.Current.Get("Goal.PrivacyProgress")} {CurrencyDisplayService.Current.SelectedCurrency}";
            var svc = CurrencyDisplayService.Current;
            return $"{svc.Convert(Goal.CurrentAmount):N0} / {svc.Convert(Goal.TargetAmount):N0} {svc.SelectedCurrency}";
        }
    }
    public double Progress => Goal.ProgressPercent;
    public string StatusColor => Progress >= 100 ? "#27AE60"
                               : Progress >= 50  ? "#F39C12"
                               : "#6C63FF";

    [ObservableProperty] private decimal _depositAmount;

    partial void OnDepositAmountChanged(decimal value)
    {
        var maxDisplay = CurrencyDisplayService.Current.Convert(Goal.RemainingAmount);
        if (value > maxDisplay && maxDisplay >= 0)
            DepositAmount = maxDisplay;
    }

    internal decimal AvgMonthlySavings { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WhatIfResult))]
    private double _whatIfExtra = 0;

    public double WhatIfMax => (double)CurrencyDisplayService.Current.Convert(3000m);

    public string WhatIfResult
    {
        get
        {
            var loc = LocalizationService.Current;
            decimal total = AvgMonthlySavings + CurrencyDisplayService.Current.ConvertToPln((decimal)WhatIfExtra);

            if (Goal.RemainingAmount <= 0)  return loc.Get("Goal.AlreadyAchieved");
            if (total <= 0)                 return loc.Get("Goal.StartSaving");

            int months = (int)Math.Ceiling((double)(Goal.RemainingAmount / total));
            if (months > 1200) return loc.Get("Goal.StartSaving");
            var date   = DateTime.Now.AddMonths(months);

            if (WhatIfExtra <= 0)
            {
                if (AvgMonthlySavings <= 0) return loc.Get("Goal.NoSavingsData");
                return string.Format(loc.Get("Goal.CurrentPace"), months, $"{date:MMM yyyy}");
            }

            decimal baseSavings = AvgMonthlySavings;
            int baseMonths = baseSavings > 0
                ? (int)Math.Ceiling((double)(Goal.RemainingAmount / baseSavings))
                : int.MaxValue;

            int saved = baseMonths != int.MaxValue ? baseMonths - months : 0;
            string faster = saved > 0 ? string.Format(loc.Get("Goal.MonthsSooner"), saved) : "";
            return string.Format(loc.Get("Goal.ExtraSavings"), $"{WhatIfExtra:N0}", CurrencyDisplayService.Current.Symbol, $"{date:MMM yyyy}", faster);
        }
    }
}

public class HabitAlert
{
    public string  Category       { get; set; } = "";
    public decimal ThisMonth      { get; set; }
    public decimal LastMonth      { get; set; }
    public double  ChangePercent  => LastMonth > 0
        ? (double)((ThisMonth - LastMonth) / LastMonth * 100) : 0;
    public string  Message
    {
        get
        {
            var svc = CurrencyDisplayService.Current;
            return $"📊 {Category}: +{ChangePercent:N0}% vs last month " +
                   $"({svc.Convert(LastMonth):N0} → {svc.Convert(ThisMonth):N0} {svc.Symbol})";
        }
    }
    public string ImpactHint { get; set; } = "";
}

public class MonthCellVM
{
    public int Month { get; set; }
    public string MonthName { get; set; } = "";
    public int GoalCount { get; set; }
    public bool IsCurrent { get; set; }
    public List<string> GoalNames { get; set; } = [];
    public string GoalSummary => GoalCount == 0
        ? LocalizationService.Current.Get("Label.NoGoals")
        : $"{GoalCount} {LocalizationService.Current.Get("Label.GoalCount")}";
}

public partial class GoalsViewModel : BaseViewModel
{
    private readonly GoalService         _goalService;
    private readonly TransactionService  _txService;
    private readonly SnackbarService     _snackbar;
    private readonly CurrencyDisplayService _currencyDisplay;

    [ObservableProperty] private ObservableCollection<GoalItemVM>  _goalItems    = [];
    [ObservableProperty] private ObservableCollection<GoalItemVM>  _filteredGoalItems = [];
    [ObservableProperty] private ObservableCollection<HabitAlert>  _habitAlerts  = [];
    [ObservableProperty] private bool _showHabitAlerts;
    [ObservableProperty] private Goal? _selectedGoal;

    [ObservableProperty] private string   _newName          = string.Empty;
    [ObservableProperty] private string   _newIcon          = "🎯";
    [ObservableProperty] private decimal  _newTargetAmount;
    [ObservableProperty] private decimal  _newCurrentAmount;
    [ObservableProperty] private DateTime? _newTargetDate;

    [ObservableProperty] private int _selectedYear = DateTime.Today.Year;
    [ObservableProperty] private int? _selectedMonth;
    [ObservableProperty] private bool _isMonthView;
    [ObservableProperty] private string _yearLabel = DateTime.Today.Year.ToString();
    [ObservableProperty] private string _monthLabel = "";

    public ObservableCollection<MonthCellVM> MonthCells { get; } = [];

    public List<string> AvailableIcons { get; } =
        ["🎯", "🏠", "🚗", "✈️", "💍", "🎓", "🏖️", "💻", "📱", "🏋️", "🌍", "💎"];

    public GoalsViewModel(GoalService goalService, TransactionService txService,
                          SnackbarService snackbar, CurrencyDisplayService currencyDisplay)
    {
        _goalService      = goalService;
        _txService        = txService;
        _snackbar         = snackbar;
        _currencyDisplay  = currencyDisplay;
        _currencyDisplay.DisplayCurrencyChanged += Load;
        PrivacyService.Current.PrivacyChanged += Load;
        LocalizationService.Current.LanguageChanged += Load;
    }

    [RelayCommand]
    public void Load()
    {
        var avgSavings        = _txService.GetAverageMonthlySavings(3);
        var monthsWithSavings = _txService.GetMonthsWithPositiveSavings(3);

        GoalItems.Clear();
        foreach (var g in _goalService.GetAll())
        {
            var predicted = _goalService.PredictAchievementDate(g, avgSavings);
            GoalItems.Add(new GoalItemVM
            {
                Goal                = g,
                HasPrediction       = predicted.HasValue,
                PredictedDateText   = predicted.HasValue
                    ? string.Format(LocalizationService.Current.Get("Goal.Achievable"), $"{predicted.Value:MMMM yyyy}")
                    : "",
                MonthsWithSavings   = monthsWithSavings,
                AvgMonthlySavings   = avgSavings
            });
        }

        LoadHabitAlerts();
        RefreshMonthGrid();
        RefreshFilteredGoals();
    }

    private void RefreshFilteredGoals()
    {
        FilteredGoalItems.Clear();
        if (IsMonthView && SelectedMonth.HasValue)
        {
            foreach (var item in GoalItems.Where(g =>
                g.Goal.CreatedDate.Year == SelectedYear &&
                g.Goal.CreatedDate.Month == SelectedMonth.Value))
                FilteredGoalItems.Add(item);
        }
    }

    private void RefreshMonthGrid()
    {
        YearLabel = SelectedYear.ToString();
        MonthCells.Clear();
        var allGoals = _goalService.GetAll();
        var monthNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames;

        for (int m = 1; m <= 12; m++)
        {
            var active = allGoals.Where(g =>
                g.CreatedDate.Year == SelectedYear && g.CreatedDate.Month == m).ToList();

            bool isCurrent = SelectedYear == DateTime.Today.Year && m == DateTime.Today.Month;

            MonthCells.Add(new MonthCellVM
            {
                Month = m,
                MonthName = monthNames[m - 1],
                GoalCount = active.Count,
                IsCurrent = isCurrent,
                GoalNames = active.Take(3).Select(g => $"{g.Icon} {g.Name}").ToList()
            });
        }
    }

    [RelayCommand]
    private void PreviousYear()
    {
        SelectedYear--;
        RefreshMonthGrid();
    }

    [RelayCommand]
    private void NextYear()
    {
        SelectedYear++;
        RefreshMonthGrid();
    }

    [RelayCommand]
    private void SelectMonth(MonthCellVM cell)
    {
        if (cell == null) return;
        SelectedMonth = cell.Month;
        MonthLabel = $"{new DateTime(SelectedYear, cell.Month, 1):MMMM yyyy}";
        IsMonthView = true;
        RefreshFilteredGoals();
    }

    [RelayCommand]
    private void BackToYearView()
    {
        IsMonthView = false;
        SelectedMonth = null;
        FilteredGoalItems.Clear();
    }


    private void LoadHabitAlerts()
    {
        var today   = DateTime.Today;
        var currExp = _txService.GetExpensesByCategory(today.Year, today.Month);

        var prev    = today.AddMonths(-1);
        var prevExp = _txService.GetExpensesByCategory(prev.Year, prev.Month);

        HabitAlerts.Clear();
        const double threshold = 0.10;

        foreach (var kv in currExp)
        {
            prevExp.TryGetValue(kv.Key, out var lastMonthAmt);
            if (lastMonthAmt <= 0) continue;

            double pct = (double)((kv.Value - lastMonthAmt) / lastMonthAmt);
            if (pct < threshold) continue;

            var firstGoal = GoalItems.FirstOrDefault(g =>
                g.AvgMonthlySavings > 0 && g.Goal.RemainingAmount > 0);

            string impact = "";
            if (firstGoal != null)
            {
                decimal extraExpense    = kv.Value - lastMonthAmt;
                decimal savingsWithout  = firstGoal.AvgMonthlySavings;
                decimal savingsWith     = Math.Max(0, savingsWithout - extraExpense);
                int baseWeeks  = savingsWithout > 0
                    ? (int)Math.Ceiling((double)(firstGoal.Goal.RemainingAmount / savingsWithout) * 4.33)
                    : 0;
                int newWeeks   = savingsWith > 0
                    ? (int)Math.Ceiling((double)(firstGoal.Goal.RemainingAmount / savingsWith) * 4.33)
                    : baseWeeks;
                int delayWeeks = newWeeks - baseWeeks;
                if (delayWeeks > 0)
                    impact = $"   ⚠️ Goal '{firstGoal.Goal.Name}' may shift by ~{delayWeeks} week(s).";
            }

            HabitAlerts.Add(new HabitAlert
            {
                Category    = kv.Key,
                ThisMonth   = kv.Value,
                LastMonth   = lastMonthAmt,
                ImpactHint  = impact
            });
        }

        ShowHabitAlerts = HabitAlerts.Count > 0;
    }

    [RelayCommand]
    private void AddGoal()
    {
        if (string.IsNullOrWhiteSpace(NewName) || NewTargetAmount <= 0)
        {
            _snackbar.ShowWarning("Please enter a name and target amount.");
            return;
        }
        if (NewCurrentAmount < 0)
        {
            _snackbar.ShowWarning("Current amount cannot be negative.");
            return;
        }
        if (NewCurrentAmount > NewTargetAmount)
        {
            _snackbar.ShowWarning("Current amount cannot exceed the target amount.");
            return;
        }
        var createdDate = IsMonthView && SelectedMonth.HasValue
            ? new DateTime(SelectedYear, SelectedMonth.Value, 1)
            : DateTime.Today;

        _goalService.Add(new Goal
        {
            Name          = NewName,
            Icon          = NewIcon,
            TargetAmount  = _currencyDisplay.ConvertToPln(NewTargetAmount),
            CurrentAmount = _currencyDisplay.ConvertToPln(NewCurrentAmount),
            CreatedDate   = createdDate,
            TargetDate    = NewTargetDate
        });
        NewName          = string.Empty;
        NewTargetAmount  = 0;
        NewCurrentAmount = 0;
        NewTargetDate    = null;
        NewIcon          = "🎯";
        Load();
    }

    [RelayCommand]
    private void Deposit(GoalItemVM item)
    {
        if (item == null || item.DepositAmount <= 0) return;
        if (item.Goal.ProgressPercent >= 100)
        {
            _snackbar.ShowWarning("This goal is already completed.");
            return;
        }
        _goalService.AddFunds(item.Goal.Id, _currencyDisplay.ConvertToPln(item.DepositAmount));
        item.DepositAmount = 0;
        Load();
    }

    [RelayCommand]
    private void DeleteGoal(GoalItemVM item)
    {
        if (item == null) return;
        _goalService.Delete(item.Goal.Id);
        _snackbar.Show($"Goal '{item.Goal.Name}' deleted.");
        Load();
    }
}
