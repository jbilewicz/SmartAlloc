using System.Net.Http;
using System.Net.Http.Json;
using SmartAlloc.Models;

namespace SmartAlloc.Services;

public class CurrencyService
{
    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.nbp.pl/api/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private List<CurrencyRate>? _cache;
    private DateTime _cacheTime = DateTime.MinValue;
    private const int CacheMinutes = 15;

    public async Task<List<CurrencyRate>> GetRatesAsync()
    {
        if (_cache != null && (DateTime.Now - _cacheTime).TotalMinutes < CacheMinutes)
            return _cache;

        try
        {
            var response = await _http.GetFromJsonAsync<List<NbpApiResponse>>(
                "exchangerates/tables/A/?format=json")
                .ConfigureAwait(false);

            var table = response?.FirstOrDefault();
            if (table is null) return GetFallback();

            var wantedCodes = new HashSet<string> { "USD", "EUR", "CHF", "GBP" };
            _cache = table.Rates
                .Where(r => wantedCodes.Contains(r.Code))
                .Select(r => new CurrencyRate
                {
                    Code = r.Code,
                    Currency = r.Currency,
                    Mid = r.Mid,
                    EffectiveDate = DateTime.TryParse(table.EffectiveDate, out var d) ? d : DateTime.Today
                })
                .ToList();

            _cacheTime = DateTime.Now;
            return _cache;
        }
        catch
        {
            return GetFallback();
        }
    }

    public decimal ConvertFromPln(decimal amountPln, string targetCode, List<CurrencyRate> rates)
    {
        if (targetCode == "PLN") return amountPln;
        var rate = rates.FirstOrDefault(r => r.Code == targetCode);
        return rate != null ? Math.Round(amountPln / rate.Mid, 2) : amountPln;
    }

    private static List<CurrencyRate> GetFallback() =>
    [
        new() { Code = "USD", Currency = "US dollar",      Mid = 4.01m, EffectiveDate = DateTime.Today },
        new() { Code = "EUR", Currency = "euro",            Mid = 4.27m, EffectiveDate = DateTime.Today },
        new() { Code = "CHF", Currency = "Swiss franc",     Mid = 4.52m, EffectiveDate = DateTime.Today },
        new() { Code = "GBP", Currency = "British pound",   Mid = 5.05m, EffectiveDate = DateTime.Today },
    ];
}
