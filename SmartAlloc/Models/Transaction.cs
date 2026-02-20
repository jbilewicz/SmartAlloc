namespace SmartAlloc.Models;

public class Transaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
}

public enum TransactionType
{
    Income,
    Expense
}
