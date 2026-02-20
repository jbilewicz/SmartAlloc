namespace SmartAlloc.Models;

public class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    /// <summary>Day of month on which the transaction is added (1–28).</summary>
    public int DayOfMonth { get; set; } = 1;
    /// <summary>"yyyy-MM" of the last month it was processed. Null = never.</summary>
    public string? LastRunYearMonth { get; set; }

    public string TypeLabel => Type == TransactionType.Income ? "Income" : "Expense";
}
