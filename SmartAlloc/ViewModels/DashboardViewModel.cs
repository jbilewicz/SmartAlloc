using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Defaults;
using SkiaSharp;
using SmartAlloc.Models;
using SmartAlloc.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace SmartAlloc.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly TransactionService     _txService;
    private readonly CurrencyService        _currencyService;
    private readonly CurrencyDisplayService _currencyDisplay;

    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private decimal _totalIncome;
    [ObservableProperty] private decimal _totalExpense;
    [ObservableProperty] private string  _lastUpdated = "";
    [ObservableProperty] private bool    _ratesLoaded;
    [ObservableProperty] private string  _rateInfo = "Loading rates...";

    private List<CurrencyRate> _rates = [];

    [ObservableProperty]
    private ISeries[] _pieSeries = [];

    [ObservableProperty]
    private SolidColorPaint _legendTextPaint = new SolidColorPaint(SKColors.White);

    [ObservableProperty]
    private ISeries[] _lineSeries = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private Axis[] _yAxes =
    [
        new Axis
        {
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = v => $"{v:N0} z\u0142",
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#AAAACC")) { StrokeThickness = 1 }
        }
    ];

    public DashboardViewModel(TransactionService txService, CurrencyService currencyService,
                              CurrencyDisplayService currencyDisplay, ThemeService themeService)
    {
        _txService        = txService;
        _currencyService  = currencyService;
        _currencyDisplay  = currencyDisplay;
        LegendTextPaint = new SolidColorPaint(themeService.IsDark ? SKColors.White : SKColor.Parse("#1A1A2E"));
        themeService.ThemeChanged += () =>
        {
            LegendTextPaint = new SolidColorPaint(themeService.IsDark ? SKColors.White : SKColor.Parse("#1A1A2E"));
            LoadPieChart();
            LoadLineChart();
        };
        _currencyDisplay.DisplayCurrencyChanged += () =>
        {
            LoadPieChart();
            LoadLineChart();
        };
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        Balance = _txService.GetBalance();
        TotalIncome = _txService.GetTotalIncome();
        TotalExpense = _txService.GetTotalExpense();

        LoadPieChart();
        LoadLineChart();
        await LoadCurrencyAsync();
        IsBusy = false;
    }

    private void LoadPieChart()
    {
        var today = DateTime.Today;
        var expenses = _txService.GetExpensesByCategory(today.Year, today.Month);
        var colors = new[]
        {
            "#FF6B6B","#4ECDC4","#45B7D1","#96CEB4","#DDA0DD",
            "#F7DC6F","#82E0AA","#F1948A","#85C1E9","#D7BDE2"
        };
        int i = 0;
        PieSeries = expenses
            .OrderByDescending(x => x.Value)
            .Select(kvp => (ISeries)new PieSeries<decimal>
            {
                Values = [_currencyDisplay.Convert(kvp.Value)],
                Name = StripEmoji(LocalizationService.Current.TranslateCategory(kvp.Key)),
                Fill = new SolidColorPaint(SKColor.Parse(colors[i++ % colors.Length])),
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 11,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                HoverPushout = 0
            })
            .ToArray();
    }

    private static string StripEmoji(string text) =>
        Regex.Replace(text, @"[\p{So}\p{Cs}\uFE0F]+\s*", "").Trim();

    private void LoadLineChart()
    {
        var history = _txService.GetMonthlyBalanceHistory(6);
        var sym     = _currencyDisplay.Symbol;
        var values  = history.Select(h => new ObservableValue((double)_currencyDisplay.Convert(h.Balance))).ToArray();
        var labels  = history.Select(h => h.Month.ToString("MMM yy")).ToArray();

        LineSeries =
        [
            new LineSeries<ObservableValue>
            {
                Values = values,
                Name = "Monthly balance",
                Stroke = new SolidColorPaint(SKColor.Parse("#6C63FF")) { StrokeThickness = 3 },
                Fill = new LinearGradientPaint(
                    SKColor.Parse("#446C63FF"),
                    SKColor.Parse("#006C63FF"),
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(SKColor.Parse("#6C63FF")),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#AAAACC")) { StrokeThickness = 1 }
            }
        ];

        YAxes =
        [
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                Labeler = v => $"{v:N0} {sym}",
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#AAAACC")) { StrokeThickness = 1 }
            }
        ];
    }

    private async Task LoadCurrencyAsync()
    {
        _rates = await _currencyService.GetRatesAsync();
        RatesLoaded = _rates.Count > 0;
        _currencyDisplay.UpdateRates(_rates);

        RateInfo = _rates.Count > 0
            ? $"NBP: {string.Join("  •  ", _rates.Select(r => $"{r.Code} {r.Mid:N4}"))}"
            : "No NBP connection – using default values";
        LastUpdated = $"Updated: {DateTime.Now:HH:mm}";
    }

    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        RateInfo = "Updating rates...";
        await LoadCurrencyAsync();
    }
}
