using CommunityToolkit.Mvvm.ComponentModel;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public partial class CurrencyDisplayService : ObservableObject
{
    public static CurrencyDisplayService Current { get; private set; } = null!;

    private readonly CurrencyService _currencyService;
    private List<CurrencyRate> _rates = [];

    [NotifyPropertyChangedFor(nameof(AmountLabel))]
    [NotifyPropertyChangedFor(nameof(LimitLabel))]
    [NotifyPropertyChangedFor(nameof(TargetAmountLabel))]
    [NotifyPropertyChangedFor(nameof(InitialAmountLabel))]
    [NotifyPropertyChangedFor(nameof(Symbol))]
    [ObservableProperty] private string _selectedCurrency = "PLN";

    public List<string> AvailableCurrencies { get; } = ["PLN", "USD", "EUR", "CHF", "GBP"];

    public event Action? DisplayCurrencyChanged;

    public CurrencyDisplayService(CurrencyService currencyService)
    {
        _currencyService = currencyService;
        Current = this;
        LocalizationService.Current.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(AmountLabel));
            OnPropertyChanged(nameof(LimitLabel));
            OnPropertyChanged(nameof(TargetAmountLabel));
            OnPropertyChanged(nameof(InitialAmountLabel));
        };
    }

    public async Task InitializeAsync()
    {
        _rates = await _currencyService.GetRatesAsync();
    }

    public void UpdateRates(List<CurrencyRate> rates)
    {
        _rates = rates;
        if (SelectedCurrency != "PLN")
            DisplayCurrencyChanged?.Invoke();
    }

    partial void OnSelectedCurrencyChanged(string value)
    {
        DisplayCurrencyChanged?.Invoke();
    }

    public decimal Convert(decimal plnAmount)
    {
        if (SelectedCurrency == "PLN" || _rates.Count == 0) return plnAmount;
        return _currencyService.ConvertFromPln(plnAmount, SelectedCurrency, _rates);
    }

    public decimal ConvertTo(decimal plnAmount, string targetCode)
    {
        if (targetCode == "PLN" || _rates.Count == 0) return plnAmount;
        return _currencyService.ConvertFromPln(plnAmount, targetCode, _rates);
    }

    public string Format(decimal plnAmount)
    {
        if (SelectedCurrency == "PLN") return $"{plnAmount:N2} PLN";
        var converted = Convert(plnAmount);
        return SelectedCurrency switch
        {
            "USD" => $"${converted:N2}",
            "EUR" => $"€{converted:N2}",
            "CHF" => $"CHF {converted:N2}",
            "GBP" => $"£{converted:N2}",
            _     => $"{converted:N2} {SelectedCurrency}"
        };
    }

    public string Symbol => SelectedCurrency switch
    {
        "USD" => "$",
        "EUR" => "€",
        "CHF" => "CHF ",
        "GBP" => "£",
        _     => "PLN"
    };

    public decimal ConvertToPln(decimal displayAmount)
    {
        if (SelectedCurrency == "PLN" || _rates.Count == 0) return displayAmount;
        var rate = _rates.FirstOrDefault(r => r.Code == SelectedCurrency);
        return rate != null ? Math.Round(displayAmount * rate.Mid, 2) : displayAmount;
    }

    public string AmountLabel => $"{LocalizationService.Current.Get("Label.Amount")} ({SelectedCurrency})";
    public string LimitLabel  => $"{LocalizationService.Current.Get("Label.Amount")} ({SelectedCurrency})";
    public string TargetAmountLabel => $"{LocalizationService.Current.Get("Label.TargetAmount")} ({SelectedCurrency})";
    public string InitialAmountLabel => $"{LocalizationService.Current.Get("Label.InitialAmount")} ({SelectedCurrency})";
}