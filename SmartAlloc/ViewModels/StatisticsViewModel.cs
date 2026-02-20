using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SmartAlloc.ViewModels;

public partial class StatisticsViewModel : BaseViewModel
{
    private readonly TransactionService _txService;
    private readonly ThemeService _themeService;

    [ObservableProperty] private decimal _avgDailyExpense;
    [ObservableProperty] private decimal _avgDailyIncome;
    [ObservableProperty] private int _totalTransactions;
    [ObservableProperty] private decimal _biggestExpense;
    [ObservableProperty] private string _biggestExpenseCategory = "";
    [ObservableProperty] private string _savingsRate = "";

    [ObservableProperty] private ObservableCollection<CategoryStat> _topExpenseCategories = [];
    [ObservableProperty] private ObservableCollection<CategoryStat> _topIncomeCategories = [];

    [ObservableProperty] private ISeries[] _comparisonSeries = [];
    [ObservableProperty] private Axis[] _comparisonXAxes = [];
    [ObservableProperty] private Axis[] _comparisonYAxes = [
        new Axis
        {
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = v => $"{v:N0}",
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2A4A")) { StrokeThickness = 1 }
        }
    ];

    [ObservableProperty] private ISeries[] _trendSeries = [];
    [ObservableProperty] private Axis[] _trendXAxes = [];
    [ObservableProperty] private Axis[] _trendYAxes = [
        new Axis
        {
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = v => $"{v:N0}",
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2A4A")) { StrokeThickness = 1 }
        }
    ];

    public StatisticsViewModel(TransactionService txService, ThemeService themeService)
    {
        _txService = txService;
        _themeService = themeService;
    }

    [RelayCommand]
    public void Load()
    {
        LoadKPIs();
        LoadTopCategories();
        LoadComparisonChart();
        LoadTrendChart();
    }

    private void LoadKPIs()
    {
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var daysPassed = today.Day;

        var monthlyExpense = _txService.GetMonthlyExpense(today.Year, today.Month);
        var monthlyIncome = _txService.GetMonthlyIncome(today.Year, today.Month);

        AvgDailyExpense = daysPassed > 0 ? monthlyExpense / daysPassed : 0;
        AvgDailyIncome = daysPassed > 0 ? monthlyIncome / daysPassed : 0;
        TotalTransactions = _txService.GetByMonth(today.Year, today.Month).Count;

        var allTx = _txService.GetByMonth(today.Year, today.Month);
        var expenseTx = allTx.Where(t => t.Type == TransactionType.Expense).ToList();
        if (expenseTx.Count != 0)
        {
            var biggest = expenseTx.MaxBy(t => t.Amount)!;
            BiggestExpense = biggest.Amount;
            BiggestExpenseCategory = biggest.CategoryName;
        }
        else
        {
            BiggestExpense = 0;
            BiggestExpenseCategory = "—";
        }

        SavingsRate = monthlyIncome > 0
            ? $"{(monthlyIncome - monthlyExpense) / monthlyIncome * 100:N1}%"
            : "—";
    }

    private void LoadTopCategories()
    {
        var today = DateTime.Today;
        var expenses = _txService.GetExpensesByCategory(today.Year, today.Month);
        TopExpenseCategories.Clear();
        foreach (var kvp in expenses.OrderByDescending(x => x.Value).Take(5))
        {
            var total = expenses.Values.Sum();
            TopExpenseCategories.Add(new CategoryStat
            {
                Name = kvp.Key,
                Amount = kvp.Value,
                Percentage = total > 0 ? (double)(kvp.Value / total * 100) : 0
            });
        }
    }

    private void LoadComparisonChart()
    {
        var incomeValues = new List<double>();
        var expenseValues = new List<double>();
        var labels = new List<string>();

        for (int i = 5; i >= 0; i--)
        {
            var d = DateTime.Now.AddMonths(-i);
            incomeValues.Add((double)_txService.GetMonthlyIncome(d.Year, d.Month));
            expenseValues.Add((double)_txService.GetMonthlyExpense(d.Year, d.Month));
            labels.Add(d.ToString("MMM yy", CultureInfo.InvariantCulture));
        }

        ComparisonSeries =
        [
            new ColumnSeries<double>
            {
                Values = incomeValues,
                Name = "Income",
                Fill = new SolidColorPaint(SKColor.Parse("#27AE60")),
                MaxBarWidth = 20,
                Rx = 4, Ry = 4
            },
            new ColumnSeries<double>
            {
                Values = expenseValues,
                Name = "Expenses",
                Fill = new SolidColorPaint(SKColor.Parse("#E74C3C")),
                MaxBarWidth = 20,
                Rx = 4, Ry = 4
            }
        ];

        ComparisonXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2A4A")) { StrokeThickness = 1 }
            }
        ];
    }

    private void LoadTrendChart()
    {
        var values = new List<double>();
        var labels = new List<string>();

        for (int i = 11; i >= 0; i--)
        {
            var d = DateTime.Now.AddMonths(-i);
            values.Add((double)_txService.GetMonthlyExpense(d.Year, d.Month));
            labels.Add(d.ToString("MMM", CultureInfo.InvariantCulture));
        }

        TrendSeries =
        [
            new LineSeries<double>
            {
                Values = values,
                Name = "Monthly expenses",
                Stroke = new SolidColorPaint(SKColor.Parse("#E74C3C")) { StrokeThickness = 3 },
                Fill = new LinearGradientPaint(
                    SKColor.Parse("#44E74C3C"),
                    SKColor.Parse("#00E74C3C"),
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 6,
                GeometryFill = new SolidColorPaint(SKColor.Parse("#E74C3C")),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            }
        ];

        TrendXAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#2A2A4A")) { StrokeThickness = 1 }
            }
        ];
    }
}

public class CategoryStat
{
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}
