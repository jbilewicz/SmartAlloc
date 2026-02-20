namespace SmartAlloc.Models;

public class RecurringTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public int DayOfMonth { get; set; } = 1;
    public string? LastRunYearMonth { get; set; }

    public int UserId { get; set; }

    public string TypeLabel => Type == TransactionType.Income ? "Income" : "Expense";
}
