namespace SmartAlloc.Models;

public class CurrencyRate
{
    public string Code { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal Mid { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class NbpApiResponse
{
    public string Table { get; set; } = string.Empty;
    public string No { get; set; } = string.Empty;
    public string EffectiveDate { get; set; } = string.Empty;
    public List<NbpRate> Rates { get; set; } = new();
}

public class NbpRate
{
    public string Currency { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal Mid { get; set; }
}
